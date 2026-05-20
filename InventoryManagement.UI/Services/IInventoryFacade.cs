using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.InventoryCommands;
using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.ItemResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public interface IInventoryFacade
{
    Task<ApiCallResult<InventoryCatalogPageViewModel>> GetCatalogAsync(string? query, string? categoryName, string? sort, CancellationToken cancellationToken = default);
    Task<ApiCallResult<InventoryCatalogPageViewModel>> GetInventoriesByTagAsync(Guid tagId, string? tagName, CancellationToken cancellationToken = default);
    Task<ApiCallResult<InventoryCreatePageViewModel>> GetCreatePageAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateInventoryAsync(CreateInventoryCommand command, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateInventoryAsync(InventoryUpdateInputModel input, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteInventoryAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> AddCommentAsync(InventoryCommentInputModel input, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UpdateCommentAsync(Guid commentId, string content, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UploadedFileResultDto>> UploadInventoryImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<ApiCallResult<InventoryDetailsPageViewModel>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<GetInventoryFieldsByInventoryIdResult>>> GetInventoryFieldsAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<TagDto>>> GetInventoryTagsAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<GetInventoryCustomIdRulesByInventoryIdResult>>> GetInventoryCustomIdRulesAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<InventoryAccessListDto>>> GetInventoryAccessListAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<InventoryAccessUserLookupViewModel>>> SearchUsersForAccessAsync(Guid inventoryId, string? query, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<GetProfileInventoriesResult>>> GetOwnInventoriesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<GetProfileInventoriesResult>>> GetEditableInventoriesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> AddFieldsAsync(Guid inventoryId, IReadOnlyCollection<AddInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UpdateFieldsAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteFieldAsync(Guid inventoryId, Guid fieldId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> ReorderFieldsAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> AddCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<AddInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UpdateCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteCustomIdRuleAsync(Guid inventoryId, Guid ruleId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> ReorderCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UpdateInventoryTagsAsync(Guid inventoryId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default);
    Task<ApiCallResult> UpdateAccessAsync(Guid inventoryId, UpdateInventoryAccessInputModel input, CancellationToken cancellationToken = default);
    Task<ApiCallResult<ItemCreatePageViewModel>> GetItemCreatePageAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<ItemEditPageViewModel>> GetItemEditPageAsync(Guid inventoryId, Guid itemId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<BatchItemEditPageViewModel>> GetBatchItemEditPageAsync(Guid inventoryId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken = default);
    Task<ApiCallResult<ItemDetailsPageViewModel>> GetItemDetailsAsync(Guid inventoryId, Guid itemId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UploadedFileResultDto>> UploadItemImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<Guid>>> AddItemsAsync(Guid inventoryId, IReadOnlyCollection<string> itemNames, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateItemAsync(ItemEditPageViewModel model, CancellationToken cancellationToken = default);
    Task<ApiCallResult> SaveItemImagesAsync(Guid itemId, IReadOnlyCollection<ItemImageInputModel> images, CancellationToken cancellationToken = default);
    Task<ApiCallResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ApiCallResult> SaveItemFieldValuesAsync(Guid itemId, IReadOnlyCollection<ItemFieldValueInputModel> fields, CancellationToken cancellationToken = default);
    Task<ApiCallResult> SaveBatchItemFieldValuesAsync(BatchItemEditPageViewModel model, CancellationToken cancellationToken = default);
    Task<ApiCallResult<List<TagDto>>> GetAllTagsAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<GlobalSearchResult>> GetGlobalSearchAsync(string? query, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> ToggleItemLikeAsync(Guid itemId, CancellationToken cancellationToken = default);
}
