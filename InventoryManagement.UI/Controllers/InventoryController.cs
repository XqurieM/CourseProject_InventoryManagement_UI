using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.InventoryCommands;
using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class InventoryController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;
    private readonly IInventoryFacade _inventoryFacade;

    public InventoryController(IAuthenticationFacade authenticationFacade, IInventoryFacade inventoryFacade)
    {
        _authenticationFacade = authenticationFacade;
        _inventoryFacade = inventoryFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? categoryName, string? sort, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetCatalogAsync(q, categoryName, sort, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result);
        }

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ByTag(Guid tagId, string? tagName, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetInventoriesByTagAsync(tagId, tagName, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Index), fallbackController: "Inventory");
        }

        return View("Index", result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.GetCreatePageAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result);
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryCreatePageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.UploadedImage is not null && model.UploadedImage.Length > 0)
        {
            var uploadResult = await _inventoryFacade.UploadInventoryImageAsync(model.UploadedImage, cancellationToken);
            if (!uploadResult.IsSuccess || uploadResult.Value is null)
            {
                var createPageResult = await _inventoryFacade.GetCreatePageAsync(cancellationToken);
                model.Categories = createPageResult.Value?.Categories ?? [];
                model.CategoriesHelpText = createPageResult.Value?.CategoriesHelpText;
                ModelState.AddModelError(string.Empty, uploadResult.ErrorMessage ?? "Image upload could not be completed.");
                return View(model);
            }

            model.Form.ImageUrl = uploadResult.Value.Url;
        }

        var result = await _inventoryFacade.CreateInventoryAsync(model.Form, cancellationToken);
        if (!result.IsSuccess || result.Value == Guid.Empty)
        {
            var pageResult = await _inventoryFacade.GetCreatePageAsync(cancellationToken);
            model.Categories = pageResult.Value?.Categories ?? [];
            model.CategoriesHelpText = pageResult.Value?.CategoriesHelpText;
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Inventory could not be created.");
            return View(model);
        }

        SetSuccessMessage("Inventory created. You can now continue with fields, custom ID rules and access settings.");
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetDetailsAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Index), fallbackController: "Inventory");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.AddFieldsAsync(id, model.FieldForms, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Field definitions could not be saved.");
        }

        SetSuccessMessage("Field definitions sent to the API.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFields(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.UpdateFieldsAsync(id, model.ExistingFieldForms, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Existing field definitions could not be updated.");
        }

        SetSuccessMessage("Existing field definitions updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var detailsResult = await _inventoryFacade.GetDetailsAsync(id, cancellationToken);
        if (!detailsResult.IsSuccess || detailsResult.Value is null)
        {
            return RedirectForFailure(detailsResult, fallbackAction: nameof(Details), fallbackController: "Inventory");
        }

        if (!detailsResult.Value.Inventory.CanManageInventory)
        {
            TempData["ErrorMessage"] = "Only the owner or an admin can change inventory settings or replace the image.";
            return RedirectToAction(nameof(Details), new { id });
        }

        model.UpdateForm.Id = id;

        if (model.UploadedImage is not null && model.UploadedImage.Length > 0)
        {
            var uploadResult = await _inventoryFacade.UploadInventoryImageAsync(model.UploadedImage, cancellationToken);
            if (!uploadResult.IsSuccess || uploadResult.Value is null)
            {
                return RedirectToInventoryDetailsForFailure(id, uploadResult, "Image upload could not be completed.");
            }

            model.UpdateForm.ImageUrl = uploadResult.Value.Url;
        }

        var result = await _inventoryFacade.UpdateInventoryAsync(model.UpdateForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Inventory settings could not be updated.");
        }

        SetSuccessMessage("Inventory settings updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.DeleteInventoryAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Inventory could not be deleted.");
        }

        SetSuccessMessage("Inventory deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCustomIdRule(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.AddCustomIdRulesAsync(id, model.RuleForms, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Custom ID rules could not be saved.");
        }

        SetSuccessMessage("Custom ID rules sent to the API.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCustomIdRules(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.UpdateCustomIdRulesAsync(id, model.ExistingRuleForms, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Existing custom ID rules could not be updated.");
        }

        SetSuccessMessage("Existing custom ID rules updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccess(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.UpdateAccessAsync(id, model.AccessForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Access settings could not be updated.");
        }

        SetSuccessMessage("Access settings updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult Access(Guid id) => RedirectToAction(nameof(Details), new { id });

    public IActionResult Fields(Guid id) => RedirectToAction(nameof(Details), new { id });

    public IActionResult CustomId(Guid id) => RedirectToAction(nameof(Details), new { id });

    [HttpGet]
    public async Task<IActionResult> SearchAccessUsers(Guid id, string? q, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return Unauthorized();
        }

        var result = await _inventoryFacade.SearchUsersForAccessAsync(id, q, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 400, new { error = result.ErrorMessage ?? "User search failed." });
        }

        return Json(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, InventoryDetailsPageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        model.CommentForm.InventoryId = id;

        var result = await _inventoryFacade.AddCommentAsync(model.CommentForm, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Comment could not be sent.");
        }

        SetSuccessMessage("Comment sent.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.DeleteCommentAsync(commentId, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectToInventoryDetailsForFailure(id, result, "Comment could not be deleted.");
        }

        SetSuccessMessage("Comment deleted.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult RedirectToInventoryDetailsForFailure(Guid inventoryId, ApiCallResult result, string fallbackMessage)
    {
        TempData["ErrorMessage"] = result.ErrorMessage ?? fallbackMessage;
        return RedirectToAction(nameof(Details), new { id = inventoryId });
    }
}
