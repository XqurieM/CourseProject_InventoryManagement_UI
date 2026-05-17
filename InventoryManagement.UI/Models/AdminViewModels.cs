using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.CategoryCommands;

namespace InventoryManagement.UI.Models;

public sealed class AdminUsersPageViewModel
{
    public string? Query { get; set; }
    public string Role { get; set; } = "all";
    public string Status { get; set; } = "all";
    public string Sort { get; set; } = "name";
    public List<UserDto> Users { get; set; } = new();
}

public sealed class AdminUserDetailsPageViewModel
{
    public UserDto User { get; set; } = new();
}

public sealed class AdminLocalizationPageViewModel
{
    public string? LanguageCode { get; set; }
    public string? PageName { get; set; }
    public string? ResourceKey { get; set; }
    public bool? IsActive { get; set; }
    public List<LocalizationResourceRowInputModel> Resources { get; set; } = new();
    public LocalizationResourceRowInputModel NewResource { get; set; } = new();
}

public sealed class LocalizationResourceRowInputModel
{
    public Guid? Id { get; set; }
    public string ResourceKey { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string PageName { get; set; } = string.Empty;
}

public sealed class AdminCategoriesPageViewModel
{
    public Guid? EditId { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public CreateCategoryCommand CreateForm { get; set; } = new() { IsActive = true };
    public UpdateCategoryInputModel UpdateForm { get; set; } = new();
}

public sealed class UpdateCategoryInputModel
{
    public Guid CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminTagsPageViewModel
{
    public string? Query { get; set; }
    public Guid? EditId { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public CreateTagInputModel CreateForm { get; set; } = new();
    public UpdateTagInputModel UpdateForm { get; set; } = new();
}

public sealed class CreateTagInputModel
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateTagInputModel
{
    public Guid TagId { get; set; }
    public string? Name { get; set; }
}
