using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class ItemController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;
    private readonly IInventoryFacade _inventoryFacade;

    public ItemController(IAuthenticationFacade authenticationFacade, IInventoryFacade inventoryFacade)
    {
        _authenticationFacade = authenticationFacade;
        _inventoryFacade = inventoryFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.GetItemCreatePageAsync(inventoryId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: "Index", fallbackController: "Inventory");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemCreatePageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.AddItemsAsync(
            model.InventoryId,
            model.ItemRows.Select(x => x.ItemName).ToList(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Item could not be created.");
            return View(model);
        }

        var createdIds = result.Value ?? [];
        if (createdIds.Count == 0)
        {
            SetSuccessMessage("No item was created.");
            return RedirectToAction("Details", "Inventory", new { id = model.InventoryId });
        }

        if (createdIds.Count == 1)
        {
            SetSuccessMessage("Item created. Now complete its field values.");
            return RedirectToAction(nameof(Edit), new { id = createdIds[0], inventoryId = model.InventoryId });
        }

        SetSuccessMessage("Items created. Now complete their field values.");
        return RedirectToAction(nameof(BulkEdit), new { inventoryId = model.InventoryId, itemIds = string.Join(",", createdIds) });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.GetItemEditPageAsync(inventoryId, id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: "Details", fallbackController: "Inventory");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ItemEditPageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.UploadedImages.Count > 0)
        {
            var maxDisplayOrder = model.Images.Count == 0 ? 0 : model.Images.Max(x => x.DisplayOrder);
            foreach (var uploadedImage in model.UploadedImages.Where(x => x is not null && x.Length > 0))
            {
                var uploadResult = await _inventoryFacade.UploadItemImageAsync(uploadedImage, cancellationToken);
                if (!uploadResult.IsSuccess || uploadResult.Value is null)
                {
                    ModelState.AddModelError(string.Empty, uploadResult.ErrorMessage ?? "Item image could not be uploaded.");
                    return View(model);
                }

                model.Images.Add(new ItemImageInputModel
                {
                    ImageUrl = uploadResult.Value.Url,
                    DisplayOrder = ++maxDisplayOrder,
                    IsPrimary = model.Images.Count == 0
                });
            }
        }

        model.Images = model.Images
            .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .GroupBy(x => x.ImageUrl.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .First())
            .Select((image, index) => new ItemImageInputModel
            {
                ImageUrl = image.ImageUrl.Trim(),
                Caption = image.Caption,
                DisplayOrder = index + 1,
                IsPrimary = image.IsPrimary
            })
            .ToList();

        var updateResult = await _inventoryFacade.UpdateItemAsync(model, cancellationToken);
        if (!updateResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, updateResult.ErrorMessage ?? "Item could not be updated.");
            return View(model);
        }

        var valuesResult = await _inventoryFacade.SaveItemFieldValuesAsync(model.Item.Id, model.Fields, cancellationToken);
        if (!valuesResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, valuesResult.ErrorMessage ?? "Item values could not be saved.");
            return View(model);
        }

        var imageResult = await _inventoryFacade.SaveItemImagesAsync(model.Item.Id, model.Images, cancellationToken);
        if (!imageResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, imageResult.ErrorMessage ?? "Item images could not be saved.");
            return View(model);
        }

        SetSuccessMessage("Item updated.");
        return RedirectToAction(nameof(Details), new { id = model.Item.Id, inventoryId = model.InventoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.DeleteItemAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Details), fallbackController: "Inventory");
        }

        SetSuccessMessage("Item deleted.");
        return RedirectToAction("Details", "Inventory", new { id = inventoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(Guid id, Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.ToggleItemLikeAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Details), fallbackController: "Item");
        }

        SetSuccessMessage(result.Value ? "Item liked." : "Like removed.");
        return RedirectToAction(nameof(Details), new { id, inventoryId });
    }

    [HttpGet]
    public async Task<IActionResult> BulkEdit(Guid inventoryId, string itemIds, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var ids = itemIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var parsed) ? parsed : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToList();

        var result = await _inventoryFacade.GetBatchItemEditPageAsync(inventoryId, ids, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: "Details", fallbackController: "Inventory");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkEdit(BatchItemEditPageViewModel model, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _inventoryFacade.SaveBatchItemFieldValuesAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Item values could not be saved.");
            return View(model);
        }

        SetSuccessMessage("All item field values saved.");
        return RedirectToAction("Details", "Inventory", new { id = model.InventoryId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        if (inventoryId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Inventory context is required to show the item detail page.";
            return RedirectToAction("Index", "Inventory");
        }

        var result = await _inventoryFacade.GetItemDetailsAsync(inventoryId, id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: "Details", fallbackController: "Inventory");
        }

        return View(result.Value);
    }
}
