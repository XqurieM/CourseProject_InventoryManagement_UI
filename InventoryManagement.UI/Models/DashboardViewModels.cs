using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using CourseProject_InventoryManagement.Application.DTOs;

namespace InventoryManagement.UI.Models;

public sealed class DashboardPageViewModel
{
    public GetDashboardStatisticsResult Statistics { get; set; } = new();
    public List<GetInventoriesWithJoinInfosResult> LatestInventories { get; set; } = new();
    public List<GetInventoriesWithJoinInfosResult> PopularInventories { get; set; } = new();
    public bool UsedPopularTagsService { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}
