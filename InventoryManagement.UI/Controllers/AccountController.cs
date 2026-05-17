using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.AuthCommands;
using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class AccountController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;

    public AccountController(IAuthenticationFacade authenticationFacade)
    {
        _authenticationFacade = authenticationFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        if (_authenticationFacade.IsSignedIn)
        {
            var currentUserResult = await _authenticationFacade.GetCurrentUserAsync(cancellationToken);
            if (currentUserResult.IsSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            await _authenticationFacade.LogoutAsync(cancellationToken);
        }

        return View(new LoginPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginPageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authenticationFacade.LoginAsync(model.Form, cancellationToken);
        if (!result.IsSuccess)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        SetSuccessMessage(await TAsync("messages.login.success", "Welcome back.", "shared", cancellationToken));
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        if (_authenticationFacade.IsSignedIn)
        {
            var currentUserResult = await _authenticationFacade.GetCurrentUserAsync(cancellationToken);
            if (currentUserResult.IsSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            await _authenticationFacade.LogoutAsync(cancellationToken);
        }

        return View(new RegisterPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterPageViewModel model, CancellationToken cancellationToken)
    {
        if (model.Form.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), await TAsync("messages.password.mismatch", "Passwords do not match.", "shared", cancellationToken));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authenticationFacade.RegisterAsync(model.Form, cancellationToken);
        if (!result.IsSuccess)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        SetSuccessMessage(await TAsync("messages.register.success", "Your account is ready.", "shared", cancellationToken));
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authenticationFacade.LogoutAsync(cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> ExternalLoginGoogle([FromBody] ExternalGoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { success = false, message = "Google token is required." });
        }

        var result = await _authenticationFacade.ExternalGoogleLoginAsync(request.Token, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode > 0 ? result.StatusCode : StatusCodes.Status400BadRequest,
                new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Google sign-in could not be completed."
                });
        }

        return Ok(new
        {
            success = true,
            redirectUrl = Url.Action("Index", "Home")
        });
    }

    [HttpPost]
    public async Task<IActionResult> ExternalLoginMicrosoft([FromBody] ExternalMicrosoftLoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { success = false, message = "Microsoft token is required." });
        }

        var result = await _authenticationFacade.ExternalMicrosoftLoginAsync(request.Token, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(
                result.StatusCode > 0 ? result.StatusCode : StatusCodes.Status400BadRequest,
                new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Microsoft sign-in could not be completed."
                });
        }

        return Ok(new
        {
            success = true,
            redirectUrl = Url.Action("Index", "Home")
        });
    }

    [HttpGet]
    public IActionResult MicrosoftCallback()
    {
        return View();
    }
}
