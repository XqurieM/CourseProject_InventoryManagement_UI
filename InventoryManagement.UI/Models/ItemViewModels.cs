using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.ItemCommands;
using CourseProject_InventoryManagement.Domain.Enums;

namespace InventoryManagement.UI.Models;

public sealed class ItemCreatePageViewModel
{
    public Guid InventoryId { get; set; }
    public string InventoryTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool CanWriteItems { get; set; }
    public List<ItemCreateRowInputModel> ItemRows { get; set; } = new();
    public List<CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults.GetInventoryFieldsByInventoryIdResult> InventoryFields { get; set; } = new();
    public string? HelperText { get; set; }
}

public sealed class ItemCreateRowInputModel
{
    public string ItemName { get; set; } = string.Empty;
}

public sealed class ItemFieldValueDisplayViewModel
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid InventoryFieldId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryFieldType? FieldType { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowInTable { get; set; }
    public bool IsRequired { get; set; }
    public string DisplayValue { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class InventoryItemRowViewModel
{
    public ItemDto Item { get; set; } = new();
    public List<ItemFieldValueDisplayViewModel> FieldValues { get; set; } = new();
}

public sealed class ItemDetailsPageViewModel
{
    public Guid InventoryId { get; set; }
    public string InventoryTitle { get; set; } = string.Empty;
    public bool CanWriteItems { get; set; }
    public ItemDto Item { get; set; } = new();
    public List<ItemImageInputModel> Images { get; set; } = new();
    public List<ItemFieldValueDisplayViewModel> FieldValues { get; set; } = new();
    public string? HelperText { get; set; }
}

public sealed class ItemImageInputModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class ItemFieldValueInputModel
{
    public Guid InventoryFieldId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryFieldType FieldType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool ShowInTable { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool BooleanValue { get; set; }
}

public sealed class ItemEditPageViewModel
{
    public Guid InventoryId { get; set; }
    public string InventoryTitle { get; set; } = string.Empty;
    public bool CanWriteItems { get; set; }
    public ItemDto Item { get; set; } = new();
    public string ItemName { get; set; } = string.Empty;
    public string CustomId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<ItemImageInputModel> Images { get; set; } = new();
    public List<IFormFile> UploadedImages { get; set; } = new();
    public List<ItemFieldValueInputModel> Fields { get; set; } = new();
    public string? HelperText { get; set; }
}

public sealed class BatchItemEditRowViewModel
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string CustomId { get; set; } = string.Empty;
    public List<ItemFieldValueInputModel> Fields { get; set; } = new();
}

public sealed class BatchItemEditPageViewModel
{
    public Guid InventoryId { get; set; }
    public string InventoryTitle { get; set; } = string.Empty;
    public List<BatchItemEditRowViewModel> Items { get; set; } = new();
    public string? HelperText { get; set; }
}
