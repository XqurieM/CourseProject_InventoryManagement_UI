using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public abstract class AppController : Controller
{
    protected IActionResult RedirectForFailure(ApiCallResult result, string fallbackAction = "Index", string fallbackController = "Home")
    {
        if (result.StatusCode == StatusCodes.Status401Unauthorized)
        {
            HttpContext.RequestServices.GetRequiredService<IUserSessionService>().Clear();
            TempData["ErrorMessage"] = "Your session is no longer valid. Please sign in again.";
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == StatusCodes.Status403Forbidden)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction("Forbidden", "Error");
        }

        TempData["ErrorMessage"] = result.ErrorMessage ?? "The request could not be completed.";
        return RedirectToAction(fallbackAction, fallbackController);
    }

    protected void SetSuccessMessage(string message) => TempData["SuccessMessage"] = message;

    protected async Task<string> TAsync(string key, string fallback, string? pageName = null, CancellationToken cancellationToken = default)
    {
        var localizer = HttpContext.RequestServices.GetRequiredService<IUiLocalizationService>();
        return await localizer.GetTextAsync(HttpContext, key, fallback, pageName, cancellationToken);
    }
}
