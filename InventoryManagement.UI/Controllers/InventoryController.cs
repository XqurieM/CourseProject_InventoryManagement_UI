using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.InventoryCommands;
using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

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

    [HttpGet]
    public async Task<IActionResult> ExportToCsv(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetDetailsAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: nameof(Index), fallbackController: "Inventory");
        }

        var inventory = result.Value.Inventory;
        var itemRows = result.Value.ItemRows;
        var fields = result.Value.ExistingFields.OrderBy(x => x.DisplayOrder).ToList();

        var builder = new System.Text.StringBuilder();

        builder.Append("Custom ID,Item Name,Likes");
        foreach (var field in fields)
        {
            builder.Append($",\"{field.Name.Replace("\"", "\"\"")}\"");
        }
        builder.AppendLine();

        foreach (var row in itemRows)
        {
            var customId = $"\"{row.Item.CustomId?.Replace("\"", "\"\"")}\"";
            var itemName = $"\"{row.Item.ItemName?.Replace("\"", "\"\"")}\"";
            var likes = row.Item.LikeCount;
            
            builder.Append($"{customId},{itemName},{likes}");

            foreach (var field in fields)
            {
                var fieldValue = row.FieldValues.FirstOrDefault(x => x.InventoryFieldId == field.Id);
                var displayVal = fieldValue?.DisplayValue ?? string.Empty;
                builder.Append($",\"{displayVal.Replace("\"", "\"\"")}\"");
            }
            builder.AppendLine();
        }

        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        var finalBytes = preamble.Concat(bytes).ToArray();

        return File(finalBytes, "text/csv", $"{inventory.Title}_Export.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportToExcel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetDetailsAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null) return RedirectForFailure(result, fallbackAction: nameof(Index), fallbackController: "Inventory");

        var inventory = result.Value.Inventory;
        var itemRows = result.Value.ItemRows;
        var fields = result.Value.ExistingFields.OrderBy(x => x.DisplayOrder).ToList();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inventory Items");

        var headers = new List<string> { "Custom ID", "Item Name", "Likes" };
        headers.AddRange(fields.Select(f => f.Name));

        for (int i = 0; i < headers.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            cell.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        }

        for (int rowIdx = 0; rowIdx < itemRows.Count; rowIdx++)
        {
            var row = itemRows[rowIdx];
            worksheet.Cell(rowIdx + 2, 1).Value = row.Item.CustomId;
            worksheet.Cell(rowIdx + 2, 2).Value = row.Item.ItemName;
            worksheet.Cell(rowIdx + 2, 3).Value = row.Item.LikeCount;

            for (int colIdx = 0; colIdx < fields.Count; colIdx++)
            {
                var field = fields[colIdx];
                var fieldValue = row.FieldValues.FirstOrDefault(x => x.InventoryFieldId == field.Id);
                worksheet.Cell(rowIdx + 2, 4 + colIdx).Value = fieldValue?.DisplayValue ?? string.Empty;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{inventory.Title}_Export.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportToPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inventoryFacade.GetDetailsAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null) return RedirectForFailure(result, fallbackAction: nameof(Index), fallbackController: "Inventory");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var inventory = result.Value.Inventory;
        var itemRows = result.Value.ItemRows;
        var fields = result.Value.ExistingFields.OrderBy(x => x.DisplayOrder).ToList();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(compose => 
                {
                    compose.Column(column =>
                    {
                        column.Item().Text($"{inventory.Title} - Inventory Report").SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                        column.Item().Text($"Category: {inventory.CategoryName} | Exported: {DateTime.Now:g}").FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        column.Item().PaddingBottom(10).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });
                });

                page.Content().Element(compose =>
                {
                    compose.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(40);
                            foreach (var _ in fields)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(2).Text("Custom ID").SemiBold();
                            header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(2).Text("Item Name").SemiBold();
                            header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(2).Text("Likes").SemiBold();
                            foreach (var field in fields)
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(2).Text(field.Name).SemiBold();
                            }
                        });

                        foreach (var row in itemRows)
                        {
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).Padding(2).Text(row.Item.CustomId);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).Padding(2).Text(row.Item.ItemName);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).Padding(2).Text(row.Item.LikeCount.ToString());

                            foreach (var field in fields)
                            {
                                var fieldValue = row.FieldValues.FirstOrDefault(x => x.InventoryFieldId == field.Id);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).Padding(2).Text(fieldValue?.DisplayValue ?? string.Empty);
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        var bytes = document.GeneratePdf();
        return File(bytes, "application/pdf", $"{inventory.Title}_Export.pdf");
    }

    private IActionResult RedirectToInventoryDetailsForFailure(Guid inventoryId, ApiCallResult result, string fallbackMessage)
    {
        TempData["ErrorMessage"] = result.ErrorMessage ?? fallbackMessage;
        return RedirectToAction(nameof(Details), new { id = inventoryId });
    }
}
