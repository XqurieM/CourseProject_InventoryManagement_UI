using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.InventoryCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using CourseProject_InventoryManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InventoryManagement.UI.Models;

public sealed class InventoryCatalogPageViewModel
{
    public string? Query { get; set; }
    public string? CategoryName { get; set; }
    public Guid? SelectedTagId { get; set; }
    public string? SelectedTagName { get; set; }
    public string Sort { get; set; } = "newest";
    public List<GetInventoriesWithJoinInfosResult> Inventories { get; set; } = new();
    public List<string> CategoryNames { get; set; } = new();
    public bool RequiresLogin { get; set; }
}

public sealed class CategoryOptionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class InventoryCreatePageViewModel
{
    public CreateInventoryCommand Form { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    public string? CategoriesHelpText { get; set; }
    public IFormFile? UploadedImage { get; set; }
}

public sealed class InventoryUpdateInputModel
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsPublic { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class AddInventoryFieldInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryFieldType FieldType { get; set; } = InventoryFieldType.SingleLineText;
    public int DisplayOrder { get; set; } = 1;
    public bool IsRequired { get; set; }
    public bool ShowInTable { get; set; } = true;
}

public sealed class ExistingInventoryFieldInputModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryFieldType FieldType { get; set; } = InventoryFieldType.SingleLineText;
    public int DisplayOrder { get; set; } = 1;
    public bool IsRequired { get; set; }
    public bool ShowInTable { get; set; } = true;
}

public sealed class AddInventoryRuleInputModel
{
    public int PartOrder { get; set; } = 1;
    public CustomIdPartType PartType { get; set; } = CustomIdPartType.StaticText;
    public string? Format { get; set; }
    public string? StaticTextValue { get; set; }
}

public sealed class ExistingInventoryRuleInputModel
{
    public Guid Id { get; set; }
    public int PartOrder { get; set; } = 1;
    public CustomIdPartType PartType { get; set; } = CustomIdPartType.StaticText;
    public string? Format { get; set; }
    public string? StaticTextValue { get; set; }
}

public sealed class UpdateInventoryAccessInputModel
{
    public bool IsPublic { get; set; }
    public List<Guid> UserIds { get; set; } = new();
}

public sealed class InventoryAccessUserLookupViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsAlreadyAdded { get; set; }
}

public sealed class InventoryCommentInputModel
{
    public Guid InventoryId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class InventoryCommentViewModel
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool CanDelete { get; set; }
    public string AuthorLabel { get; set; } = string.Empty;
}

public sealed class InventoryDetailsPageViewModel
{
    public InventoryDto Inventory { get; set; } = new();
    public InventoryUpdateInputModel UpdateForm { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    public List<InventoryAccessListDto> ExistingAccesses { get; set; } = new();
    public List<InventoryItemRowViewModel> ItemRows { get; set; } = new();
    public List<GetInventoryFieldsByInventoryIdResult> ExistingFields { get; set; } = new();
    public List<ExistingInventoryFieldInputModel> ExistingFieldForms { get; set; } = new();
    public List<AddInventoryFieldInputModel> FieldForms { get; set; } = new();
    public int NextFieldDisplayOrderStart { get; set; } = 1;
    public List<GetInventoryCustomIdRulesByInventoryIdResult> ExistingRules { get; set; } = new();
    public List<ExistingInventoryRuleInputModel> ExistingRuleForms { get; set; } = new();
    public List<AddInventoryRuleInputModel> RuleForms { get; set; } = new();
    public int NextRulePartOrderStart { get; set; } = 1;
    public UpdateInventoryAccessInputModel AccessForm { get; set; } = new();
    public InventoryCommentInputModel CommentForm { get; set; } = new();
    public List<InventoryCommentViewModel> Comments { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public string? FieldValuesHelperText { get; set; }
    public IFormFile? UploadedImage { get; set; }
}
