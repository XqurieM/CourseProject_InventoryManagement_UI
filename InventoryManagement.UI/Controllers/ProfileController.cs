using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class ProfileController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;
    private readonly IInventoryFacade _inventoryFacade;

    public ProfileController(IAuthenticationFacade authenticationFacade, IInventoryFacade inventoryFacade)
    {
        _authenticationFacade = authenticationFacade;
        _inventoryFacade = inventoryFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var userResult = await _authenticationFacade.GetCurrentUserAsync(cancellationToken);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return RedirectForFailure(userResult);
        }

        var ownTask = _inventoryFacade.GetOwnInventoriesAsync(userResult.Value.Id, cancellationToken);
        var editableTask = _inventoryFacade.GetEditableInventoriesAsync(userResult.Value.Id, cancellationToken);
        await Task.WhenAll(ownTask, editableTask);

        var model = new ProfilePageViewModel
        {
            User = userResult.Value,
            PreferredLanguage = userResult.Value.PreferredLanguage,
            PreferredTheme = userResult.Value.PreferredTheme,
            OwnedInventories = ownTask.Result.IsSuccess && ownTask.Result.Value is not null ? ownTask.Result.Value : [],
            WritableInventories = editableTask.Result.IsSuccess && editableTask.Result.Value is not null ? editableTask.Result.Value : [],
            InventoryNote = (!ownTask.Result.IsSuccess || !editableTask.Result.IsSuccess)
                ? await TAsync("profile.inventory.note", "Some profile inventory lists could not be loaded from the backend.", "profile.index", cancellationToken)
                : null
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(ProfilePageViewModel model, CancellationToken cancellationToken)
    {
        var languageResult = await _authenticationFacade.UpdateLanguageAsync(model.PreferredLanguage, cancellationToken);
        if (!languageResult.IsSuccess)
        {
            return RedirectForFailure(languageResult, fallbackAction: nameof(Index), fallbackController: "Profile");
        }

        var themeResult = await _authenticationFacade.UpdateThemeAsync(model.PreferredTheme, cancellationToken);
        if (!themeResult.IsSuccess)
        {
            return RedirectForFailure(themeResult, fallbackAction: nameof(Index), fallbackController: "Profile");
        }

        SetSuccessMessage(await TAsync("messages.preferences.saved", "Preferences saved.", "shared", cancellationToken));
        return RedirectToAction(nameof(Index));
    }
}
