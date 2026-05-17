using CourseProject_InventoryManagement.Application.DTOs;

namespace InventoryManagement.UI.Models;

public sealed class SearchResultsPageViewModel
{
    public string Query { get; set; } = string.Empty;
    public List<InventoryDto> Inventories { get; set; } = new();
    public List<ItemDto> Items { get; set; } = new();
    public bool WasSearched { get; set; }
}
