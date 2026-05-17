using InventoryManagement.UI.Models;

namespace InventoryManagement.UI.Services;

public interface IDashboardFacade
{
    Task<ApiCallResult<DashboardPageViewModel>> GetDashboardAsync(CancellationToken cancellationToken = default);
}
