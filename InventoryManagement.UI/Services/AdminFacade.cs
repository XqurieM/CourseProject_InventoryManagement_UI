using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.CategoryCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.GeneralCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.TagCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.UserCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Queries.GeneralQueries;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public sealed class AdminFacade : IAdminFacade
{
    private readonly BackendApiClient _backendApiClient;

    public AdminFacade(BackendApiClient backendApiClient)
    {
        _backendApiClient = backendApiClient;
    }

    public async Task<ApiCallResult<AdminUsersPageViewModel>> GetUsersAsync(
        string? query,
        string role,
        string status,
        string sort,
        CancellationToken cancellationToken = default)
    {
        var usersResult = await _backendApiClient.GetAsync<List<UserDto>>("Admin/GetUsers", requiresAuth: true, cancellationToken);
        if (!usersResult.IsSuccess)
        {
            return ApiCallResult<AdminUsersPageViewModel>.Failure(usersResult.StatusCode, usersResult.ErrorMessage);
        }

        var users = usersResult.Value ?? new List<UserDto>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            users = users
                .Where(x =>
                    x.UserName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    x.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        users = role switch
        {
            "admin" => users.Where(x => x.IsAdmin).ToList(),
            "user" => users.Where(x => !x.IsAdmin).ToList(),
            _ => users
        };

        users = status switch
        {
            "active" => users.Where(x => !x.IsBlocked).ToList(),
            "blocked" => users.Where(x => x.IsBlocked).ToList(),
            _ => users
        };

        users = sort switch
        {
            "email" => users.OrderBy(x => x.Email).ToList(),
            _ => users.OrderBy(x => x.UserName).ToList()
        };

        return ApiCallResult<AdminUsersPageViewModel>.Success(new AdminUsersPageViewModel
        {
            Query = query,
            Role = role,
            Status = status,
            Sort = sort,
            Users = users
        });
    }

    public async Task<ApiCallResult<AdminUserDetailsPageViewModel>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userResult = await _backendApiClient.GetAsync<UserDto>($"Admin/GetUserById?id={userId}", requiresAuth: true, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            return ApiCallResult<AdminUserDetailsPageViewModel>.Failure(userResult.StatusCode, userResult.ErrorMessage);
        }

        return ApiCallResult<AdminUserDetailsPageViewModel>.Success(new AdminUserDetailsPageViewModel
        {
            User = userResult.Value
        });
    }

    public async Task<ApiCallResult<AdminCategoriesPageViewModel>> GetCategoriesPageAsync(Guid? editId, CancellationToken cancellationToken = default)
    {
        var categoriesResult = await _backendApiClient.GetAsync<List<CategoryDto>>("Category/GetAllCategories", requiresAuth: true, cancellationToken);
        if (!categoriesResult.IsSuccess)
        {
            return ApiCallResult<AdminCategoriesPageViewModel>.Failure(categoriesResult.StatusCode, categoriesResult.ErrorMessage);
        }

        var model = new AdminCategoriesPageViewModel
        {
            EditId = editId,
            Categories = (categoriesResult.Value ?? []).OrderBy(x => x.Name).ToList(),
            CreateForm = new CreateCategoryCommand { IsActive = true }
        };

        if (editId.HasValue && editId.Value != Guid.Empty)
        {
            var categoryResult = await _backendApiClient.GetAsync<CategoryDto>($"Category/GetCategoryById?categoryId={editId.Value}", requiresAuth: true, cancellationToken);
            if (!categoryResult.IsSuccess || categoryResult.Value is null)
            {
                return ApiCallResult<AdminCategoriesPageViewModel>.Failure(categoryResult.StatusCode, categoryResult.ErrorMessage);
            }

            model.UpdateForm = new UpdateCategoryInputModel
            {
                CategoryId = categoryResult.Value.Id,
                Name = categoryResult.Value.Name,
                Description = categoryResult.Value.Description,
                IsActive = categoryResult.Value.IsActive
            };
        }

        return ApiCallResult<AdminCategoriesPageViewModel>.Success(model);
    }

    public Task<ApiCallResult<Guid>> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<CreateCategoryCommand, Guid>("Category/CreateCategory", command, requiresAuth: true, cancellationToken);

    public Task<ApiCallResult<Guid>> UpdateCategoryAsync(UpdateCategoryInputModel model, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<UpdateCategoryCommand, Guid>(
            "Category/UpdateCategory",
            new UpdateCategoryCommand
            {
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Category/DeleteCategory",
            new DeleteCategoryCommand { CategoryId = categoryId },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult<AdminTagsPageViewModel>> GetTagsPageAsync(string? query, Guid? editId, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(query)
            ? "Tag/GetAllTags"
            : $"Tag/SearchTags?q={Uri.EscapeDataString(query.Trim())}";

        var tagsResult = await _backendApiClient.GetAsync<List<TagDto>>(path, requiresAuth: true, cancellationToken);
        if (!tagsResult.IsSuccess)
        {
            return ApiCallResult<AdminTagsPageViewModel>.Failure(tagsResult.StatusCode, tagsResult.ErrorMessage);
        }

        var model = new AdminTagsPageViewModel
        {
            Query = query,
            EditId = editId,
            Tags = (tagsResult.Value ?? []).OrderByDescending(x => x.InventoryCount).ThenBy(x => x.Name).ToList()
        };

        if (editId.HasValue && editId.Value != Guid.Empty)
        {
            var tagResult = await _backendApiClient.GetAsync<TagDto>($"Tag/GetTagById?tagId={editId.Value}", requiresAuth: true, cancellationToken);
            if (!tagResult.IsSuccess || tagResult.Value is null)
            {
                return ApiCallResult<AdminTagsPageViewModel>.Failure(tagResult.StatusCode, tagResult.ErrorMessage);
            }

            model.UpdateForm = new UpdateTagInputModel
            {
                TagId = tagResult.Value.Id,
                Name = tagResult.Value.Name
            };
        }

        return ApiCallResult<AdminTagsPageViewModel>.Success(model);
    }

    public Task<ApiCallResult<Guid>> CreateTagAsync(CreateTagInputModel model, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<CreateTagCommand, Guid>(
            "Tag/CreateTag",
            new CreateTagCommand { Name = model.Name },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<Guid>> UpdateTagAsync(UpdateTagInputModel model, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<UpdateTagCommand, Guid>(
            "Tag/UpdateTag",
            new UpdateTagCommand
            {
                TagId = model.TagId,
                Name = model.Name
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Tag/DeleteTag",
            new DeleteTagCommand { TagId = tagId },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult<AdminLocalizationPageViewModel>> GetLocalizationResourcesAsync(string? languageCode, string? pageName, string? resourceKey, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(languageCode)) query.Add($"languageCode={Uri.EscapeDataString(languageCode)}");
        if (!string.IsNullOrWhiteSpace(pageName)) query.Add($"pageName={Uri.EscapeDataString(pageName)}");
        if (!string.IsNullOrWhiteSpace(resourceKey)) query.Add($"resourceKey={Uri.EscapeDataString(resourceKey)}");
        if (isActive.HasValue) query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");

        var path = "General/GetLocalizationResourcesAdminList";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        var result = await _backendApiClient.GetAsync<List<LocalizationResourceAdminResult>>(path, requiresAuth: true, cancellationToken);
        if (!result.IsSuccess)
        {
            return ApiCallResult<AdminLocalizationPageViewModel>.Failure(result.StatusCode, result.ErrorMessage);
        }

        return ApiCallResult<AdminLocalizationPageViewModel>.Success(new AdminLocalizationPageViewModel
        {
            LanguageCode = languageCode,
            PageName = pageName,
            ResourceKey = resourceKey,
            IsActive = isActive,
            Resources = (result.Value ?? []).Select(x => new LocalizationResourceRowInputModel
            {
                Id = x.Id,
                ResourceKey = x.ResourceKey,
                LanguageCode = x.LanguageCode,
                Value = x.Value,
                IsActive = x.IsActive,
                PageName = x.PageName
            }).ToList(),
            NewResource = new LocalizationResourceRowInputModel
            {
                LanguageCode = languageCode ?? "tr",
                PageName = pageName ?? "shared",
                IsActive = true
            }
        });
    }

    public Task<ApiCallResult> SaveLocalizationResourcesAsync(IReadOnlyCollection<LocalizationResourceRowInputModel> resources, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "General/BulkUpsertLocalizationResources",
            new BulkUpsertLocalizationResourcesCommand
            {
                Resources = resources.Select(x => new LocalizationResourceUpsertDto
                {
                    Id = x.Id,
                    ResourceKey = x.ResourceKey,
                    LanguageCode = x.LanguageCode,
                    Value = x.Value,
                    IsActive = x.IsActive,
                    PageName = x.PageName
                }).ToList()
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<Guid>> CreateLocalizationResourceAsync(LocalizationResourceRowInputModel resource, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<UpsertLocalizationResourceCommand, Guid>(
            "General/UpsertLocalizationResource",
            new UpsertLocalizationResourceCommand
            {
                Id = resource.Id,
                ResourceKey = resource.ResourceKey,
                LanguageCode = resource.LanguageCode,
                Value = resource.Value,
                IsActive = resource.IsActive,
                PageName = resource.PageName
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> DeleteLocalizationResourceAsync(Guid id, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "General/DeleteLocalizationResource",
            new DeleteLocalizationResourceCommand { Id = id },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> BlockUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync("Admin/BlockUser", new BlockUserCommand { UserId = userId }, requiresAuth: true, cancellationToken);

    public Task<ApiCallResult> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync("Admin/UnblockUser", new UnblockUserCommand { UserId = userId }, requiresAuth: true, cancellationToken);

    public Task<ApiCallResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync("Admin/DeleteUser", new DeleteUserCommand { UserId = userId }, requiresAuth: true, cancellationToken);

    public Task<ApiCallResult> GrantAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync("Admin/GrantAdminRole", new GrantAdminRoleCommand { UserId = userId }, requiresAuth: true, cancellationToken);

    public Task<ApiCallResult> RevokeAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync("Admin/RevokeAdminRole", new RevokeAdminRoleCommand { UserId = userId }, requiresAuth: true, cancellationToken);
}
