using CourseProject_InventoryManagement.Application.DTOs;
using CourseProject_InventoryManagement.Application.Features.CQRS.Results.InventoryResults;
using CourseProject_InventoryManagement.Domain.Enums;

namespace InventoryManagement.UI.Models;

public sealed class ProfilePageViewModel
{
    public UserDto User { get; set; } = new();
    public LanguageType PreferredLanguage { get; set; }
    public ThemeType PreferredTheme { get; set; }
    public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
    public List<GetProfileInventoriesResult> OwnedInventories { get; set; } = new();
    public List<GetProfileInventoriesResult> WritableInventories { get; set; } = new();
    public string? InventoryNote { get; set; }
}
