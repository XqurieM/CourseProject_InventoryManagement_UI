using CourseProject_InventoryManagement.Application.Features.CQRS.Queries.GeneralQueries;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using CourseProject_InventoryManagement.Application.DTOs;
using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public sealed class DashboardFacade : IDashboardFacade
{
    private readonly BackendApiClient _backendApiClient;

    public DashboardFacade(BackendApiClient backendApiClient)
    {
        _backendApiClient = backendApiClient;
    }

    public async Task<ApiCallResult<DashboardPageViewModel>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var statsTask = _backendApiClient.PostAsync<GetDashboardStatisticsQuery, GetDashboardStatisticsResult>(
            "General/GetDashboardStatistics",
            new GetDashboardStatisticsQuery(),
            requiresAuth: true,
            cancellationToken);
        var latestTask = _backendApiClient.GetAsync<List<GetInventoriesWithJoinInfosResult>>(
            "Inventory/GetLast10Inventories",
            requiresAuth: true,
            cancellationToken);
        var popularTask = _backendApiClient.GetAsync<List<GetInventoriesWithJoinInfosResult>>(
            "Inventory/GetPopular5Inventories",
            requiresAuth: true,
            cancellationToken);
        var tagsTask = _backendApiClient.GetAsync<List<TagDto>>(
            "Tag/GetPopularTags?take=12",
            requiresAuth: true,
            cancellationToken);

        await Task.WhenAll(statsTask, latestTask, popularTask, tagsTask);

        if (!statsTask.Result.IsSuccess)
        {
            return ApiCallResult<DashboardPageViewModel>.Failure(statsTask.Result.StatusCode, statsTask.Result.ErrorMessage);
        }

        if (!latestTask.Result.IsSuccess)
        {
            return ApiCallResult<DashboardPageViewModel>.Failure(latestTask.Result.StatusCode, latestTask.Result.ErrorMessage);
        }

        if (!popularTask.Result.IsSuccess)
        {
            return ApiCallResult<DashboardPageViewModel>.Failure(popularTask.Result.StatusCode, popularTask.Result.ErrorMessage);
        }

        var latest = latestTask.Result.Value ?? new List<GetInventoriesWithJoinInfosResult>();
        var popular = popularTask.Result.Value ?? new List<GetInventoriesWithJoinInfosResult>();
        var tags = tagsTask.Result.IsSuccess
            ? (tagsTask.Result.Value ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderByDescending(x => x.InventoryCount)
                .ThenBy(x => x.Name)
                .Take(12)
                .ToList()
            : new List<TagDto>();

        var model = new DashboardPageViewModel
        {
            Statistics = statsTask.Result.Value ?? new GetDashboardStatisticsResult(),
            LatestInventories = latest,
            PopularInventories = popular,
            UsedPopularTagsService = tagsTask.Result.IsSuccess,
            Tags = tags.Count > 0
                ? tags
                : latest.Concat(popular)
                    .SelectMany(x => x.Tags ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new TagDto
                    {
                        Id = x.First().Id,
                        Name = x.First().Name,
                        NormalizedName = x.First().Name.ToUpperInvariant(),
                        InventoryCount = x.Count()
                    })
                    .OrderBy(x => x.Name)
                    .Take(12)
                    .ToList()
        };

        return ApiCallResult<DashboardPageViewModel>.Success(model);
    }
}
