using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.CategoryCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.TagCommands;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public interface IAdminFacade
{
    Task<ApiCallResult<AdminUsersPageViewModel>> GetUsersAsync(string? query, string role, string status, string sort, CancellationToken cancellationToken = default);
    Task<ApiCallResult<AdminUserDetailsPageViewModel>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<AdminCategoriesPageViewModel>> GetCategoriesPageAsync(Guid? editId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateCategoryAsync(UpdateCategoryInputModel model, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<AdminTagsPageViewModel>> GetTagsPageAsync(string? query, Guid? editId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateTagAsync(CreateTagInputModel model, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateTagAsync(UpdateTagInputModel model, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<AdminLocalizationPageViewModel>> GetLocalizationResourcesAsync(string? languageCode, string? pageName, string? resourceKey, bool? isActive, CancellationToken cancellationToken = default);
    Task<ApiCallResult> SaveLocalizationResourcesAsync(IReadOnlyCollection<LocalizationResourceRowInputModel> resources, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateLocalizationResourceAsync(LocalizationResourceRowInputModel resource, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteLocalizationResourceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiCallResult> BlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> GrantAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> RevokeAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
