using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.UserCommands;

namespace InventoryManagement.UI.Controllers;

public class AdminController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;
    private readonly IAdminFacade _adminFacade;

    public AdminController(IAuthenticationFacade authenticationFacade, IAdminFacade adminFacade)
    {
        _authenticationFacade = authenticationFacade;
        _adminFacade = adminFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? query, string role = "all", string status = "all", string sort = "name", CancellationToken cancellationToken = default)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _adminFacade.GetUsersAsync(query, role, status, sort, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result);
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _adminFacade.GetUserByIdAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        return View("User", result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Categories(Guid? editId, CancellationToken cancellationToken = default)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _adminFacade.GetCategoriesPageAsync(editId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Tags(string? query, Guid? editId, CancellationToken cancellationToken = default)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _adminFacade.GetTagsPageAsync(query, editId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(AdminCategoriesPageViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.CreateCategoryAsync(model.CreateForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Categories), fallbackController: "Admin");
        }

        SetSuccessMessage("Category created.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(AdminCategoriesPageViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.UpdateCategoryAsync(model.UpdateForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Categories), fallbackController: "Admin");
        }

        SetSuccessMessage("Category updated.");
        return RedirectToAction(nameof(Categories), new { editId = model.UpdateForm.CategoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.DeleteCategoryAsync(categoryId, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Categories), fallbackController: "Admin");
        }

        SetSuccessMessage("Category deleted.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTag(AdminTagsPageViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.CreateTagAsync(model.CreateForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Tags), fallbackController: "Admin");
        }

        SetSuccessMessage("Tag created.");
        return RedirectToAction(nameof(Tags));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTag(AdminTagsPageViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.UpdateTagAsync(model.UpdateForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Tags), fallbackController: "Admin");
        }

        SetSuccessMessage("Tag updated.");
        return RedirectToAction(nameof(Tags), new { editId = model.UpdateForm.TagId, query = model.Query });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.DeleteTagAsync(tagId, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Tags), fallbackController: "Admin");
        }

        SetSuccessMessage("Tag deleted.");
        return RedirectToAction(nameof(Tags));
    }

    [HttpGet]
    public async Task<IActionResult> Localization(string? languageCode, string? pageName, string? resourceKey, bool? isActive, CancellationToken cancellationToken = default)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _adminFacade.GetLocalizationResourcesAsync(languageCode, pageName, resourceKey, isActive, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLocalization(AdminLocalizationPageViewModel model, CancellationToken cancellationToken = default)
    {
        var resources = model.Resources
            .Where(x => !string.IsNullOrWhiteSpace(x.ResourceKey) && !string.IsNullOrWhiteSpace(x.LanguageCode))
            .ToList();

        var result = await _adminFacade.SaveLocalizationResourcesAsync(resources, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Localization), fallbackController: "Admin");
        }

        SetSuccessMessage("Localization resources saved.");
        return RedirectToAction(nameof(Localization), new { model.LanguageCode, model.PageName, model.ResourceKey, model.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocalization(AdminLocalizationPageViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.CreateLocalizationResourceAsync(model.NewResource, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Localization), fallbackController: "Admin");
        }

        SetSuccessMessage("Localization resource created.");
        return RedirectToAction(nameof(Localization), new { languageCode = model.NewResource.LanguageCode, pageName = model.NewResource.PageName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLocalization(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _adminFacade.DeleteLocalizationResourceAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Localization), fallbackController: "Admin");
        }

        SetSuccessMessage("Localization resource deleted.");
        return RedirectToAction(nameof(Localization));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Block(Guid userId, CancellationToken cancellationToken) =>
        RunAdminActionAsync(() => _adminFacade.BlockUserAsync(userId, cancellationToken), "User blocked.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken) =>
        RunAdminActionAsync(() => _adminFacade.UnblockUserAsync(userId, cancellationToken), "User unblocked.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken) =>
        RunAdminActionAsync(() => _adminFacade.DeleteUserAsync(userId, cancellationToken), "User deleted.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> GrantAdmin(Guid userId, CancellationToken cancellationToken) =>
        RunAdminActionAsync(() => _adminFacade.GrantAdminAsync(userId, cancellationToken), "Admin role granted.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RevokeAdmin(Guid userId, CancellationToken cancellationToken) =>
        RunAdminActionAsync(() => _adminFacade.RevokeAdminAsync(userId, cancellationToken), "Admin role revoked.");

    private async Task<IActionResult> RunAdminActionAsync(Func<Task<InventoryManagement.UI.Models.ApiCallResult>> action, string successMessage)
    {
        var result = await action();
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        SetSuccessMessage(successMessage);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IntegrateSalesforce(IntegrateSalesforceCommand command, CancellationToken cancellationToken)
    {
        var result = await _authenticationFacade.IntegrateSalesforceAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Users), fallbackController: "Admin");
        }

        var sessionService = HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
        bool isEnglish = sessionService.GetUser()?.PreferredLanguage == CourseProject_InventoryManagement.Domain.Enums.LanguageType.English;

        string msg = isEnglish 
            ? $"Salesforce Integration Completed Successfully for user! Account ID: {result.Value?.AccountId}, Contact ID: {result.Value?.ContactId}"
            : $"Kullanıcı için Salesforce Entegrasyonu Başarıyla Tamamlandı! Hesap ID: {result.Value?.AccountId}, İletişim ID: {result.Value?.ContactId}";

        SetSuccessMessage(msg);
        return RedirectToAction(nameof(UserDetails), new { id = command.UserId });
    }
}
