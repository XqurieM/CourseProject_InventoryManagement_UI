using CourseProject_InventoryManagement.Application.DTOs;

namespace InventoryManagement.UI.Models;

public sealed class LayoutViewModel
{
    public bool IsAuthenticated { get; init; }
    public string DisplayName { get; init; } = "Guest";
    public string RoleLabel { get; init; } = "Guest";
    public UserDto? User { get; init; }
}
