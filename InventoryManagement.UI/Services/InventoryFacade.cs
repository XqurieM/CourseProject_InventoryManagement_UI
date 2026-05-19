using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.CommentCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.InventoryCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Commands.ItemCommands;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.ItemResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using CourseProject_InventoryManagement.Domain.Enums;
using InventoryManagement.UI.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace InventoryManagement.UI.Services;

public sealed class InventoryFacade : IInventoryFacade
{
    private readonly BackendApiClient _backendApiClient;
    private readonly IUserSessionService _userSessionService;

    public InventoryFacade(BackendApiClient backendApiClient, IUserSessionService userSessionService)
    {
        _backendApiClient = backendApiClient;
        _userSessionService = userSessionService;
    }

    public async Task<ApiCallResult<InventoryCatalogPageViewModel>> GetCatalogAsync(
        string? query,
        string? categoryName,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        if (!_userSessionService.IsAuthenticated)
        {
            return ApiCallResult<InventoryCatalogPageViewModel>.Success(new InventoryCatalogPageViewModel
            {
                RequiresLogin = true
            });
        }

        var latestResult = await _backendApiClient.GetAsync<List<GetInventoriesWithJoinInfosResult>>(
            "Inventory/GetLast10Inventories",
            requiresAuth: true,
            cancellationToken);

        if (!latestResult.IsSuccess)
        {
            return ApiCallResult<InventoryCatalogPageViewModel>.Failure(latestResult.StatusCode, latestResult.ErrorMessage);
        }

        var popularResult = await _backendApiClient.GetAsync<List<GetInventoriesWithJoinInfosResult>>(
            "Inventory/GetPopular5Inventories",
            requiresAuth: true,
            cancellationToken);

        if (!popularResult.IsSuccess)
        {
            return ApiCallResult<InventoryCatalogPageViewModel>.Failure(popularResult.StatusCode, popularResult.ErrorMessage);
        }

        var inventories = (latestResult.Value ?? new List<GetInventoriesWithJoinInfosResult>())
            .Concat(popularResult.Value ?? [])
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (!string.IsNullOrWhiteSpace(query))
        {
            inventories = inventories
                .Where(x =>
                    x.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (x.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Tags?.Any(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            inventories = inventories
                .Where(x => string.Equals(x.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        inventories = (sort ?? "newest").ToLowerInvariant() switch
        {
            "items" => inventories.OrderByDescending(x => x.ItemCount).ToList(),
            "az" => inventories.OrderBy(x => x.Title).ToList(),
            _ => inventories.OrderByDescending(x => x.Id).ToList()
        };

        var categoriesResult = await GetCategoryOptionsAsync(cancellationToken);

        return ApiCallResult<InventoryCatalogPageViewModel>.Success(new InventoryCatalogPageViewModel
        {
            Query = query,
            CategoryName = categoryName,
            Sort = sort ?? "newest",
            Inventories = inventories,
            CategoryNames = (categoriesResult.Value ?? [])
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
        });
    }

    public async Task<ApiCallResult<InventoryCatalogPageViewModel>> GetInventoriesByTagAsync(Guid tagId, string? tagName, CancellationToken cancellationToken = default)
    {
        var result = await _backendApiClient.GetAsync<List<GetInventoriesWithJoinInfosResult>>(
            $"Tag/GetInventoriesByTag?tagId={tagId}",
            requiresAuth: _userSessionService.IsAuthenticated,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiCallResult<InventoryCatalogPageViewModel>.Failure(result.StatusCode, result.ErrorMessage);
        }

        var categoriesResult = await GetCategoryOptionsAsync(cancellationToken);

        return ApiCallResult<InventoryCatalogPageViewModel>.Success(new InventoryCatalogPageViewModel
        {
            SelectedTagId = tagId,
            SelectedTagName = tagName,
            Sort = "newest",
            Inventories = result.Value ?? [],
            CategoryNames = (categoriesResult.Value ?? [])
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
        });
    }

    public async Task<ApiCallResult<InventoryCreatePageViewModel>> GetCreatePageAsync(CancellationToken cancellationToken = default)
    {
        var categoriesResult = await _backendApiClient.GetAsync<List<CategoryDto>>(
            "Category/GetAllCategories",
            requiresAuth: true,
            cancellationToken);

        if (!categoriesResult.IsSuccess)
        {
            return ApiCallResult<InventoryCreatePageViewModel>.Failure(categoriesResult.StatusCode, categoriesResult.ErrorMessage);
        }

        var categories = MapCategories(categoriesResult.Value);

        return ApiCallResult<InventoryCreatePageViewModel>.Success(new InventoryCreatePageViewModel
        {
            Categories = categories,
            CategoriesHelpText = categories.Count == 0
                ? "No active categories are currently available."
                : "Categories are loaded directly from the backend category service."
        });
    }

    public async Task<ApiCallResult<Guid>> CreateInventoryAsync(CreateInventoryCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CategoryId == Guid.Empty)
        {
            return ApiCallResult<Guid>.Failure(StatusCodes.Status400BadRequest, "Please choose a category.");
        }

        return await _backendApiClient.PostAsync<CreateInventoryCommand, Guid>(
            "Inventory/CreateInventory",
            command,
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult<Guid>> UpdateInventoryAsync(InventoryUpdateInputModel input, CancellationToken cancellationToken = default)
    {
        if (input.Id == Guid.Empty)
        {
            return ApiCallResult<Guid>.Failure(StatusCodes.Status400BadRequest, "Inventory id is required.");
        }

        if (input.RowVersion is null || input.RowVersion.Length == 0)
        {
            return ApiCallResult<Guid>.Failure(StatusCodes.Status400BadRequest, "Inventory version is missing. Refresh and try again.");
        }

        var command = new UpdateInventoryCommand
        {
            Id = input.Id,
            Title = input.Title,
            Description = input.Description,
            CategoryId = input.CategoryId,
            ImageUrl = input.ImageUrl,
            IsPublic = input.IsPublic,
            RowVersion = input.RowVersion
        };

        return await _backendApiClient.PostAsync<UpdateInventoryCommand, Guid>(
            "Inventory/UpdateInventory",
            command,
            requiresAuth: true,
            cancellationToken);
    }

    public Task<ApiCallResult> DeleteInventoryAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Inventory/DeleteInventory",
            new DeleteInventoryCommand { Id = inventoryId },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> AddCommentAsync(InventoryCommentInputModel input, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Comment/CreateNewComment",
            new CreateNewCommentCommand
            {
                InventoryId = input.InventoryId,
                Content = input.Content
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> UpdateCommentAsync(Guid commentId, string content, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Comment/UpdateComment",
            new UpdateCommentCommand
            {
                CommentId = commentId,
                Content = content
            },
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            $"Comment/DeleteComment?commentId={commentId}",
            new { },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult<UploadedFileResultDto>> UploadInventoryImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return ApiCallResult<UploadedFileResultDto>.Failure(StatusCodes.Status400BadRequest, "Please choose a valid image file.");
        }

        using var stream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        return await _backendApiClient.PostMultipartAsync<UploadedFileResultDto>(
            "General/UploadInventoryImage",
            content,
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult<InventoryDetailsPageViewModel>> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inventoryResult = await _backendApiClient.GetAsync<InventoryDto>(
            $"Inventory/GetInventoryById?id={id}",
            requiresAuth: _userSessionService.IsAuthenticated,
            cancellationToken);

        if (!inventoryResult.IsSuccess || inventoryResult.Value is null)
        {
            return ApiCallResult<InventoryDetailsPageViewModel>.Failure(inventoryResult.StatusCode, inventoryResult.ErrorMessage);
        }

        var fieldsResult = _userSessionService.IsAuthenticated
            ? await GetInventoryFieldsAsync(id, cancellationToken)
            : ApiCallResult<List<GetInventoryFieldsByInventoryIdResult>>.Success(new List<GetInventoryFieldsByInventoryIdResult>());
        var tagsResult = _userSessionService.IsAuthenticated
            ? await GetInventoryTagsAsync(id, cancellationToken)
            : ApiCallResult<List<TagDto>>.Success(new List<TagDto>());
        var rulesResult = _userSessionService.IsAuthenticated
            ? await GetInventoryCustomIdRulesAsync(id, cancellationToken)
            : ApiCallResult<List<GetInventoryCustomIdRulesByInventoryIdResult>>.Success(new List<GetInventoryCustomIdRulesByInventoryIdResult>());
        var accessResult = _userSessionService.IsAuthenticated
            ? await GetInventoryAccessListAsync(id, cancellationToken)
            : ApiCallResult<List<InventoryAccessListDto>>.Success(new List<InventoryAccessListDto>());
        var statisticsResult = _userSessionService.IsAuthenticated
            ? await _backendApiClient.GetAsync<GetInventoryStatisticsResult>(
                $"Inventory/GetInventoryStatistics?inventoryId={id}",
                requiresAuth: true,
                cancellationToken)
            : ApiCallResult<GetInventoryStatisticsResult>.Success(new GetInventoryStatisticsResult());
        var commentsResult = _userSessionService.IsAuthenticated
            ? await _backendApiClient.GetAsync<List<CommentDto>>(
                $"Comment/GetInventoryComments?inventoryId={id}",
                requiresAuth: true,
                cancellationToken)
            : ApiCallResult<List<CommentDto>>.Success(new List<CommentDto>());
        var categoriesResult = await GetCreatePageAsync(cancellationToken);
        var availableTagsResult = _userSessionService.IsAuthenticated
            ? await GetAllTagsAsync(cancellationToken)
            : ApiCallResult<List<TagDto>>.Success(new List<TagDto>());
        var currentUser = _userSessionService.GetUser();

        return ApiCallResult<InventoryDetailsPageViewModel>.Success(new InventoryDetailsPageViewModel
        {
            Inventory = inventoryResult.Value,
            Statistics = statisticsResult.IsSuccess && statisticsResult.Value is not null
                ? statisticsResult.Value
                : new GetInventoryStatisticsResult(),
            UpdateForm = new InventoryUpdateInputModel
            {
                Id = inventoryResult.Value.Id,
                Title = inventoryResult.Value.Title,
                Description = inventoryResult.Value.Description,
                CategoryId = inventoryResult.Value.CategoryId,
                ImageUrl = inventoryResult.Value.ImageUrl,
                IsPublic = inventoryResult.Value.IsPublic,
                RowVersion = inventoryResult.Value.RowVersion
            },
            Categories = categoriesResult.IsSuccess && categoriesResult.Value is not null
                ? categoriesResult.Value.Categories
                : [],
            AvailableTags = availableTagsResult.IsSuccess && availableTagsResult.Value is not null
                ? availableTagsResult.Value.OrderBy(x => x.Name).ToList()
                : [],
            ExistingTags = tagsResult.IsSuccess && tagsResult.Value is not null
                ? tagsResult.Value.OrderBy(x => x.Name).ToList()
                : [],
            TagForm = new InventoryTagSelectionInputModel
            {
                TagIds = tagsResult.IsSuccess && tagsResult.Value is not null
                    ? tagsResult.Value.Select(x => x.Id).ToList()
                    : []
            },
            ExistingAccesses = accessResult.IsSuccess && accessResult.Value is not null
                ? accessResult.Value.OrderBy(x => x.CreatedAtUtc).ToList()
                : [],
            ItemRows = await LoadItemRowsAsync(id, cancellationToken),
            ExistingFields = fieldsResult.IsSuccess && fieldsResult.Value is not null
                ? fieldsResult.Value.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToList()
                : [],
            ExistingFieldForms = fieldsResult.IsSuccess && fieldsResult.Value is not null
                ? fieldsResult.Value
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ExistingInventoryFieldInputModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        FieldType = x.FieldType,
                        DisplayOrder = x.DisplayOrder,
                        IsRequired = x.IsRequired,
                        ShowInTable = x.ShowInTable
                    })
                    .ToList()
                : [],
            IsAuthenticated = _userSessionService.IsAuthenticated,
            FieldForms =
            [
                new AddInventoryFieldInputModel { DisplayOrder = (fieldsResult.Value?.Select(x => x.DisplayOrder).DefaultIfEmpty(0).Max() ?? 0) + 1, ShowInTable = true, FieldType = InventoryFieldType.SingleLineText },
                new AddInventoryFieldInputModel { DisplayOrder = (fieldsResult.Value?.Select(x => x.DisplayOrder).DefaultIfEmpty(0).Max() ?? 0) + 2, ShowInTable = true, FieldType = InventoryFieldType.SingleLineText },
                new AddInventoryFieldInputModel { DisplayOrder = (fieldsResult.Value?.Select(x => x.DisplayOrder).DefaultIfEmpty(0).Max() ?? 0) + 3, ShowInTable = false, FieldType = InventoryFieldType.Number }
            ],
            NextFieldDisplayOrderStart = (fieldsResult.Value?.Select(x => x.DisplayOrder).DefaultIfEmpty(0).Max() ?? 0) + 1,
            ExistingRules = rulesResult.IsSuccess && rulesResult.Value is not null
                ? rulesResult.Value.OrderBy(x => x.PartOrder).ToList()
                : [],
            ExistingRuleForms = rulesResult.IsSuccess && rulesResult.Value is not null
                ? rulesResult.Value
                    .OrderBy(x => x.PartOrder)
                    .Select(x => new ExistingInventoryRuleInputModel
                    {
                        Id = x.Id,
                        PartOrder = x.PartOrder,
                        PartType = x.PartType,
                        Format = x.Format,
                        StaticTextValue = x.StaticTextValue
                    })
                    .ToList()
                : [],
            RuleForms =
            [
                new AddInventoryRuleInputModel { PartOrder = (rulesResult.Value?.Select(x => x.PartOrder).DefaultIfEmpty(0).Max() ?? 0) + 1, PartType = CustomIdPartType.StaticText, StaticTextValue = "INV-" },
                new AddInventoryRuleInputModel { PartOrder = (rulesResult.Value?.Select(x => x.PartOrder).DefaultIfEmpty(0).Max() ?? 0) + 2, PartType = CustomIdPartType.DateTime, Format = "yyyy" },
                new AddInventoryRuleInputModel { PartOrder = (rulesResult.Value?.Select(x => x.PartOrder).DefaultIfEmpty(0).Max() ?? 0) + 3, PartType = CustomIdPartType.Sequence, Format = "D3" }
            ],
            NextRulePartOrderStart = (rulesResult.Value?.Select(x => x.PartOrder).DefaultIfEmpty(0).Max() ?? 0) + 1,
            AccessForm = new UpdateInventoryAccessInputModel
            {
                IsPublic = inventoryResult.Value.IsPublic,
                UserIds = accessResult.IsSuccess && accessResult.Value is not null
                    ? accessResult.Value.Select(x => x.UserId).Distinct().ToList()
                    : []
            },
            CommentForm = new InventoryCommentInputModel
            {
                InventoryId = inventoryResult.Value.Id
            },
            Comments = (commentsResult.IsSuccess && commentsResult.Value is not null
                ? commentsResult.Value
                : [])
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Select(x => new InventoryCommentViewModel
                {
                    Id = x.Id,
                    InventoryId = x.InventoryId,
                    Content = x.Content,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    CanDelete = inventoryResult.Value.CanManageInventory || currentUser?.Id == x.CreatedByUserId,
                    CanEdit = inventoryResult.Value.CanManageInventory || currentUser?.Id == x.CreatedByUserId,
                    AuthorLabel = currentUser?.Id == x.CreatedByUserId ? "You" : x.CreatedByUserId.ToString()
                })
                .ToList(),
            FieldValuesHelperText = "Item field values are matched with inventory field definitions, so names, order and table visibility now come from the backend."
        });
    }

    public Task<ApiCallResult<List<GetInventoryFieldsByInventoryIdResult>>> GetInventoryFieldsAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<GetInventoryFieldsByInventoryIdResult>>(
            $"Inventory/GetInventoryFieldsByInventoryId?inventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<List<TagDto>>> GetInventoryTagsAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<TagDto>>(
            $"Inventory/GetInventoryTagsByInventoryId?inventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<List<GetInventoryCustomIdRulesByInventoryIdResult>>> GetInventoryCustomIdRulesAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<GetInventoryCustomIdRulesByInventoryIdResult>>(
            $"Inventory/GetInventoryCustomIdRulesByInventoryId?inventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<List<InventoryAccessListDto>>> GetInventoryAccessListAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<InventoryAccessListDto>>(
            $"Inventory/GetInventoryAccessList?InventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult<List<InventoryAccessUserLookupViewModel>>> SearchUsersForAccessAsync(Guid inventoryId, string? query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ApiCallResult<List<InventoryAccessUserLookupViewModel>>.Success([]);
        }

        var result = await _backendApiClient.GetAsync<List<InventoryAccessUserLookupDto>>(
            $"Inventory/SearchUsersForAccess?inventoryId={inventoryId}&searchTerm={Uri.EscapeDataString(query.Trim())}",
            requiresAuth: true,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiCallResult<List<InventoryAccessUserLookupViewModel>>.Failure(result.StatusCode, result.ErrorMessage);
        }

        return ApiCallResult<List<InventoryAccessUserLookupViewModel>>.Success(
            (result.Value ?? [])
                .Select(x => new InventoryAccessUserLookupViewModel
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    Email = x.Email,
                    IsAdmin = x.IsAdmin,
                    IsAlreadyAdded = x.IsAlreadyAdded
                })
                .ToList(),
            result.StatusCode);
    }

    public Task<ApiCallResult<List<GetProfileInventoriesResult>>> GetOwnInventoriesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<GetProfileInventoriesResult>>(
            $"Inventory/GetOwnInventories?UserId={userId}",
            requiresAuth: true,
            cancellationToken);

    public Task<ApiCallResult<List<GetProfileInventoriesResult>>> GetEditableInventoriesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<GetProfileInventoriesResult>>(
            $"Inventory/GetMyEditableInventories?UserId={userId}",
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult> AddFieldsAsync(Guid inventoryId, IReadOnlyCollection<AddInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var validInputs = inputs
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        if (validInputs.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "Add at least one field before saving.");
        }

        var command = new AddInventoryFieldCommand
        {
            InventoryId = inventoryId,
            Fields = validInputs.Select(input => new InventoryFieldDto
            {
                InventoryId = inventoryId,
                Name = input.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
                FieldType = input.FieldType,
                DisplayOrder = input.DisplayOrder,
                IsRequired = input.IsRequired,
                ShowInTable = input.ShowInTable
            }).ToList()
        };

        return await _backendApiClient.PostAsync("Inventory/AddInventoryField", command, requiresAuth: true, cancellationToken);
    }

    public async Task<ApiCallResult> UpdateFieldsAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var validInputs = inputs
            .Where(x => x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name))
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        if (validInputs.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "There are no existing fields to update.");
        }

        var command = new UpdateInventoryFieldsCommand
        {
            InventoryId = inventoryId,
            Fields = validInputs.Select(input => new UpdateInventoryFieldDto
            {
                Id = input.Id,
                Name = input.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
                FieldType = input.FieldType,
                DisplayOrder = input.DisplayOrder,
                IsRequired = input.IsRequired,
                ShowInTable = input.ShowInTable
            }).ToList()
        };

        return await _backendApiClient.PostAsync("Inventory/UpdateInventoryFields", command, requiresAuth: true, cancellationToken);
    }

    public Task<ApiCallResult> DeleteFieldAsync(Guid inventoryId, Guid fieldId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Inventory/DeleteInventoryField",
            new DeleteInventoryFieldCommand
            {
                InventoryId = inventoryId,
                FieldId = fieldId
            },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult> ReorderFieldsAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryFieldInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var reordered = inputs
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.DisplayOrder)
            .Select((x, index) => new ReorderInventoryFieldDto
            {
                FieldId = x.Id,
                DisplayOrder = index + 1
            })
            .ToList();

        if (reordered.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "There are no fields to reorder.");
        }

        return await _backendApiClient.PostAsync(
            "Inventory/ReorderInventoryFields",
            new ReorderInventoryFieldsCommand
            {
                InventoryId = inventoryId,
                Fields = reordered
            },
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult> AddCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<AddInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var validInputs = inputs
            .Where(x =>
                x.PartOrder > 0 &&
                (x.PartType != CustomIdPartType.StaticText || !string.IsNullOrWhiteSpace(x.StaticTextValue)))
            .OrderBy(x => x.PartOrder)
            .ToList();

        if (validInputs.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "Add at least one valid custom ID rule before saving.");
        }

        var command = new AddInventoryCustomIdRulesCommand
        {
            InventoryId = inventoryId,
            Rules = validInputs.Select(input => new CustomIdRulePartDto
                {
                    PartOrder = input.PartOrder,
                    PartType = input.PartType,
                    Format = string.IsNullOrWhiteSpace(input.Format) ? null : input.Format.Trim(),
                    StaticTextValue = string.IsNullOrWhiteSpace(input.StaticTextValue) ? null : input.StaticTextValue.Trim()
                })
                .ToList()
        };

        return await _backendApiClient.PostAsync("Inventory/AddInventoryCustomIdRules", command, requiresAuth: true, cancellationToken);
    }

    public async Task<ApiCallResult> UpdateCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var validInputs = inputs
            .Where(x => x.Id != Guid.Empty && x.PartOrder > 0 && (x.PartType != CustomIdPartType.StaticText || !string.IsNullOrWhiteSpace(x.StaticTextValue)))
            .OrderBy(x => x.PartOrder)
            .ToList();

        if (validInputs.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "There are no existing custom ID rules to update.");
        }

        var command = new UpdateInventoryCustomIdRulesCommand
        {
            InventoryId = inventoryId,
            Rules = validInputs.Select(input => new UpdateInventoryCustomIdRuleDto
            {
                Id = input.Id,
                PartOrder = input.PartOrder,
                PartType = input.PartType,
                Format = string.IsNullOrWhiteSpace(input.Format) ? null : input.Format.Trim(),
                StaticTextValue = string.IsNullOrWhiteSpace(input.StaticTextValue) ? null : input.StaticTextValue.Trim()
            }).ToList()
        };

        return await _backendApiClient.PostAsync("Inventory/UpdateInventoryCustomIdRules", command, requiresAuth: true, cancellationToken);
    }

    public Task<ApiCallResult> DeleteCustomIdRuleAsync(Guid inventoryId, Guid ruleId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Inventory/DeleteInventoryCustomIdRule",
            new DeleteInventoryCustomIdRuleCommand
            {
                InventoryId = inventoryId,
                RuleId = ruleId
            },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult> ReorderCustomIdRulesAsync(Guid inventoryId, IReadOnlyCollection<ExistingInventoryRuleInputModel> inputs, CancellationToken cancellationToken = default)
    {
        var reordered = inputs
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.PartOrder)
            .Select((x, index) => new ReorderInventoryCustomIdRuleDto
            {
                RuleId = x.Id,
                PartOrder = index + 1
            })
            .ToList();

        if (reordered.Count == 0)
        {
            return ApiCallResult.Failure(StatusCodes.Status400BadRequest, "There are no custom ID rules to reorder.");
        }

        return await _backendApiClient.PostAsync(
            "Inventory/ReorderInventoryCustomIdRules",
            new ReorderInventoryCustomIdRulesCommand
            {
                InventoryId = inventoryId,
                Rules = reordered
            },
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult> UpdateInventoryTagsAsync(Guid inventoryId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        var distinctTagIds = tagIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        return await _backendApiClient.PostAsync(
            "Inventory/UpdateInventoryTags",
            new UpdateInventoryTagsCommand
            {
                InventoryId = inventoryId,
                TagIds = distinctTagIds
            },
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult> UpdateAccessAsync(Guid inventoryId, UpdateInventoryAccessInputModel input, CancellationToken cancellationToken = default)
    {
        var userIds = input.UserIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var command = new UpdateInventoryAccessCommand
        {
            InventoryId = inventoryId,
            IsPublic = input.IsPublic,
            UserIds = userIds
        };

        return await _backendApiClient.PostAsync("Inventory/UpdateInventoryAccess", command, requiresAuth: true, cancellationToken);
    }

    public async Task<ApiCallResult<ItemCreatePageViewModel>> GetItemCreatePageAsync(Guid inventoryId, CancellationToken cancellationToken = default)
    {
        var inventoryResult = await GetDetailsAsync(inventoryId, cancellationToken);
        if (!inventoryResult.IsSuccess || inventoryResult.Value is null)
        {
            return ApiCallResult<ItemCreatePageViewModel>.Failure(inventoryResult.StatusCode, inventoryResult.ErrorMessage);
        }

        return ApiCallResult<ItemCreatePageViewModel>.Success(new ItemCreatePageViewModel
        {
            InventoryId = inventoryId,
            InventoryTitle = inventoryResult.Value.Inventory.Title,
            CategoryName = inventoryResult.Value.Inventory.CategoryName,
            CanWriteItems = inventoryResult.Value.Inventory.CanWriteItems,
            ItemRows =
            [
                new ItemCreateRowInputModel(),
                new ItemCreateRowInputModel(),
                new ItemCreateRowInputModel()
            ],
            InventoryFields = inventoryResult.Value.ExistingFields,
            HelperText = "You can add multiple items in one submit. After creation, the UI will move straight to field value entry."
        });
    }

    public async Task<ApiCallResult<ItemEditPageViewModel>> GetItemEditPageAsync(Guid inventoryId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var detailsResult = await GetItemDetailsAsync(inventoryId, itemId, cancellationToken);
        if (!detailsResult.IsSuccess || detailsResult.Value is null)
        {
            return ApiCallResult<ItemEditPageViewModel>.Failure(detailsResult.StatusCode, detailsResult.ErrorMessage);
        }

        var fieldsResult = await GetInventoryFieldsAsync(inventoryId, cancellationToken);
        if (!fieldsResult.IsSuccess || fieldsResult.Value is null)
        {
            return ApiCallResult<ItemEditPageViewModel>.Failure(fieldsResult.StatusCode, fieldsResult.ErrorMessage);
        }

        var fieldValuesById = detailsResult.Value.FieldValues.ToDictionary(x => x.InventoryFieldId, x => x);

        return ApiCallResult<ItemEditPageViewModel>.Success(new ItemEditPageViewModel
        {
            InventoryId = inventoryId,
            InventoryTitle = detailsResult.Value.InventoryTitle,
            CanWriteItems = detailsResult.Value.CanWriteItems,
            Item = detailsResult.Value.Item,
            ItemName = detailsResult.Value.Item.ItemName,
            CustomId = detailsResult.Value.Item.CustomId,
            RowVersion = detailsResult.Value.Item.RowVersion,
            Fields = BuildEditableFieldInputs(fieldsResult.Value, fieldValuesById),
            HelperText = "This screen updates item name, custom ID and field values together."
        });
    }

    public async Task<ApiCallResult<BatchItemEditPageViewModel>> GetBatchItemEditPageAsync(Guid inventoryId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        var inventoryResult = await GetDetailsAsync(inventoryId, cancellationToken);
        if (!inventoryResult.IsSuccess || inventoryResult.Value is null)
        {
            return ApiCallResult<BatchItemEditPageViewModel>.Failure(inventoryResult.StatusCode, inventoryResult.ErrorMessage);
        }

        var targetIds = itemIds.Distinct().ToHashSet();
        var itemRows = inventoryResult.Value.ItemRows.Where(x => targetIds.Contains(x.Item.Id)).ToList();
        var editors = new List<BatchItemEditRowViewModel>();

        foreach (var row in itemRows)
        {
            var itemEdit = await GetItemEditPageAsync(inventoryId, row.Item.Id, cancellationToken);
            if (!itemEdit.IsSuccess || itemEdit.Value is null)
            {
                return ApiCallResult<BatchItemEditPageViewModel>.Failure(itemEdit.StatusCode, itemEdit.ErrorMessage);
            }

            editors.Add(new BatchItemEditRowViewModel
            {
                ItemId = row.Item.Id,
                ItemName = row.Item.ItemName,
                CustomId = row.Item.CustomId,
                Fields = itemEdit.Value.Fields
            });
        }

        return ApiCallResult<BatchItemEditPageViewModel>.Success(new BatchItemEditPageViewModel
        {
            InventoryId = inventoryId,
            InventoryTitle = inventoryResult.Value.Inventory.Title,
            Items = editors,
            HelperText = "Items were created successfully. You can fill field values for each one here and save in a single pass."
        });
    }

    public async Task<ApiCallResult<ItemDetailsPageViewModel>> GetItemDetailsAsync(Guid inventoryId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var inventoryResult = await GetDetailsAsync(inventoryId, cancellationToken);
        if (!inventoryResult.IsSuccess || inventoryResult.Value is null)
        {
            return ApiCallResult<ItemDetailsPageViewModel>.Failure(inventoryResult.StatusCode, inventoryResult.ErrorMessage);
        }

        var itemResult = await _backendApiClient.GetAsync<ItemDto>(
            $"Item/GetItemById?itemId={itemId}",
            requiresAuth: true,
            cancellationToken);

        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            return ApiCallResult<ItemDetailsPageViewModel>.Failure(itemResult.StatusCode, itemResult.ErrorMessage);
        }

        var fieldValuesResult = await _backendApiClient.GetAsync<List<ItemFieldValuesResult>>(
            $"Item/GetItemFieldValuesByItemId?itemId={itemId}",
            requiresAuth: true,
            cancellationToken);

        if (!fieldValuesResult.IsSuccess)
        {
            return ApiCallResult<ItemDetailsPageViewModel>.Failure(fieldValuesResult.StatusCode, fieldValuesResult.ErrorMessage);
        }

        var fieldsResult = await GetInventoryFieldsAsync(inventoryId, cancellationToken);
        if (!fieldsResult.IsSuccess)
        {
            return ApiCallResult<ItemDetailsPageViewModel>.Failure(fieldsResult.StatusCode, fieldsResult.ErrorMessage);
        }

        return ApiCallResult<ItemDetailsPageViewModel>.Success(new ItemDetailsPageViewModel
        {
            InventoryId = inventoryId,
            InventoryTitle = inventoryResult.Value.Inventory.Title,
            CanWriteItems = inventoryResult.Value.Inventory.CanWriteItems,
            Item = itemResult.Value,
            FieldValues = BuildFieldValueDisplays(fieldValuesResult.Value ?? [], fieldsResult.Value ?? []),
            HelperText = "Field names, order and descriptions come from the inventory field definitions. Values are matched by InventoryFieldId."
        });
    }

    public async Task<ApiCallResult<List<Guid>>> AddItemsAsync(Guid inventoryId, IReadOnlyCollection<string> itemNames, CancellationToken cancellationToken = default)
    {
        var validNames = itemNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validNames.Count == 0)
        {
            return ApiCallResult<List<Guid>>.Failure(StatusCodes.Status400BadRequest, "Add at least one item name.");
        }

        var command = new AddItemCommand
        {
            InventoryId = inventoryId,
            Items = validNames.Select(x => new ItemAddDto { ItemName = x }).ToList()
        };

        return await _backendApiClient.PostAsync<AddItemCommand, List<Guid>>(
            "Item/AddItem",
            command,
            requiresAuth: true,
            cancellationToken);
    }

    public async Task<ApiCallResult<Guid>> UpdateItemAsync(ItemEditPageViewModel model, CancellationToken cancellationToken = default)
    {
        var command = new UpdateItemCommand
        {
            Id = model.Item.Id,
            InventoryId = model.InventoryId,
            ItemName = model.ItemName,
            CustomId = model.CustomId,
            RowVersion = model.RowVersion
        };

        return await _backendApiClient.PostAsync<UpdateItemCommand, Guid>(
            "Item/UpdateItem",
            command,
            requiresAuth: true,
            cancellationToken);
    }

    public Task<ApiCallResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync(
            "Item/DeleteItem",
            new DeleteItemCommand { Id = itemId },
            requiresAuth: true,
            cancellationToken);

    public async Task<ApiCallResult> SaveItemFieldValuesAsync(Guid itemId, IReadOnlyCollection<ItemFieldValueInputModel> fields, CancellationToken cancellationToken = default)
    {
        var command = new UpdateItemFieldValuesCommand
        {
            ItemId = itemId,
            Values = fields.Select(MapFieldValue).ToList()
        };

        return await _backendApiClient.PostAsync("Item/UpdateItemFieldValues", command, requiresAuth: true, cancellationToken);
    }

    public async Task<ApiCallResult> SaveBatchItemFieldValuesAsync(BatchItemEditPageViewModel model, CancellationToken cancellationToken = default)
    {
        foreach (var item in model.Items)
        {
            var result = await SaveItemFieldValuesAsync(item.ItemId, item.Fields, cancellationToken);
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return ApiCallResult.Success(StatusCodes.Status200OK);
    }

    public Task<ApiCallResult<List<TagDto>>> GetAllTagsAsync(CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<List<TagDto>>("Tag/GetAllTags", requiresAuth: true, cancellationToken);

    public Task<ApiCallResult<GlobalSearchResult>> GetGlobalSearchAsync(string? query, CancellationToken cancellationToken = default) =>
        _backendApiClient.GetAsync<GlobalSearchResult>(
            $"Search/GlobalSearch?q={Uri.EscapeDataString(query?.Trim() ?? string.Empty)}",
            requiresAuth: _userSessionService.IsAuthenticated,
            cancellationToken);

    public Task<ApiCallResult<bool>> ToggleItemLikeAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _backendApiClient.PostAsync<ToggleItemLikeCommand, bool>(
            "Item/ToggleItemLike",
            new ToggleItemLikeCommand
            {
                ItemId = itemId
            },
            requiresAuth: true,
            cancellationToken);

    private async Task<ApiCallResult<List<CategoryOptionViewModel>>> GetCategoryOptionsAsync(CancellationToken cancellationToken)
    {
        var result = await _backendApiClient.GetAsync<List<CategoryDto>>(
            "Category/GetAllCategories",
            requiresAuth: true,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiCallResult<List<CategoryOptionViewModel>>.Failure(result.StatusCode, result.ErrorMessage);
        }

        return ApiCallResult<List<CategoryOptionViewModel>>.Success(MapCategories(result.Value));
    }

    private static List<CategoryOptionViewModel> MapCategories(IEnumerable<CategoryDto>? categories) =>
        (categories ?? [])
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryOptionViewModel
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToList();

    private async Task<List<InventoryItemRowViewModel>> LoadItemRowsAsync(Guid inventoryId, CancellationToken cancellationToken)
    {
        if (!_userSessionService.IsAuthenticated)
        {
            return [];
        }

        var itemsResult = await _backendApiClient.GetAsync<List<ItemDto>>(
            $"Item/GetItemsByInventoryId?inventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

        if (!itemsResult.IsSuccess || itemsResult.Value is null)
        {
            return [];
        }

        var fieldValuesResult = await _backendApiClient.GetAsync<List<ItemFieldValuesResult>>(
            $"Item/GetItemFieldValuesByInventoryId?inventoryId={inventoryId}",
            requiresAuth: true,
            cancellationToken);

        var fieldValues = fieldValuesResult.IsSuccess && fieldValuesResult.Value is not null
            ? fieldValuesResult.Value
            : [];

        var fieldsResult = await GetInventoryFieldsAsync(inventoryId, cancellationToken);
        var fields = fieldsResult.IsSuccess && fieldsResult.Value is not null
            ? fieldsResult.Value
            : [];

        var fieldGroups = fieldValues
            .GroupBy(x => x.ItemId)
            .ToDictionary(
                x => x.Key,
                x => BuildFieldValueDisplays(x.ToList(), fields)
                    .OrderBy(v => v.DisplayOrder)
                    .ThenBy(v => v.Label)
                    .ToList());

        return itemsResult.Value
            .Select(item => new InventoryItemRowViewModel
            {
                Item = item,
                FieldValues = fieldGroups.TryGetValue(item.Id, out var values) ? values : []
            })
            .ToList();
    }

    private static List<ItemFieldValueDisplayViewModel> BuildFieldValueDisplays(
        IEnumerable<ItemFieldValuesResult> fieldValues,
        IReadOnlyCollection<GetInventoryFieldsByInventoryIdResult> fields)
    {
        var byId = fields.ToDictionary(x => x.Id, x => x);

        return fieldValues
            .Select(value => new ItemFieldValueDisplayViewModel
            {
                Id = value.Id,
                ItemId = value.ItemId,
                InventoryFieldId = value.InventoryFieldId,
                Label = TryResolveField(byId, value.InventoryFieldId)?.Name ?? $"Field {value.InventoryFieldId.ToString()[..8]}",
                Description = TryResolveField(byId, value.InventoryFieldId)?.Description,
                FieldType = TryResolveField(byId, value.InventoryFieldId)?.FieldType,
                DisplayOrder = TryResolveField(byId, value.InventoryFieldId)?.DisplayOrder ?? int.MaxValue,
                ShowInTable = TryResolveField(byId, value.InventoryFieldId)?.ShowInTable ?? true,
                IsRequired = TryResolveField(byId, value.InventoryFieldId)?.IsRequired ?? false,
                DisplayValue = FormatFieldValue(value),
                CreatedAtUtc = value.CreatedAtUtc,
                UpdatedAtUtc = value.UpdatedAtUtc
            })
            .ToList();
    }

    private static GetInventoryFieldsByInventoryIdResult? TryResolveField(
        IReadOnlyDictionary<Guid, GetInventoryFieldsByInventoryIdResult> byId,
        Guid inventoryFieldId) =>
        byId.TryGetValue(inventoryFieldId, out var field)
            ? field
            : null;

    private static string FormatFieldValue(ItemFieldValuesResult value)
    {
        if (!string.IsNullOrWhiteSpace(value.StringValue))
        {
            return value.StringValue;
        }

        if (value.NumberValue != 0)
        {
            return value.NumberValue.ToString("0.##");
        }

        if (value.BooleanValue)
        {
            return "Yes";
        }

        return "0 / False / empty";
    }

    private static List<ItemFieldValueInputModel> BuildEditableFieldInputs(
        IReadOnlyCollection<GetInventoryFieldsByInventoryIdResult> fields,
        IReadOnlyDictionary<Guid, ItemFieldValueDisplayViewModel> currentValues) =>
        fields
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(field =>
            {
                currentValues.TryGetValue(field.Id, out var current);
                var input = new ItemFieldValueInputModel
                {
                    InventoryFieldId = field.Id,
                    Label = field.Name,
                    Description = field.Description,
                    FieldType = field.FieldType,
                    DisplayOrder = field.DisplayOrder,
                    IsRequired = field.IsRequired,
                    ShowInTable = field.ShowInTable
                };

                switch (field.FieldType)
                {
                    case InventoryFieldType.Number:
                        if (decimal.TryParse(current?.DisplayValue, out var number))
                        {
                            input.NumberValue = number;
                        }
                        break;
                    case InventoryFieldType.Boolean:
                        input.BooleanValue = string.Equals(current?.DisplayValue, "Yes", StringComparison.OrdinalIgnoreCase);
                        break;
                    default:
                        input.StringValue = current?.DisplayValue;
                        break;
                }

                return input;
            })
            .ToList();

    private static ItemFieldValueDto MapFieldValue(ItemFieldValueInputModel input) =>
        input.FieldType switch
        {
            InventoryFieldType.Number => new ItemFieldValueDto
            {
                InventoryFieldId = input.InventoryFieldId,
                NumberValue = input.NumberValue
            },
            InventoryFieldType.Boolean => new ItemFieldValueDto
            {
                InventoryFieldId = input.InventoryFieldId,
                BooleanValue = input.BooleanValue
            },
            _ => new ItemFieldValueDto
            {
                InventoryFieldId = input.InventoryFieldId,
                StringValue = string.IsNullOrWhiteSpace(input.StringValue) ? null : input.StringValue.Trim()
            }
        };
}
