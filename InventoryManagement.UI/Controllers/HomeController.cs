using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class HomeController : AppController
{
    private readonly IAuthenticationFacade _authenticationFacade;
    private readonly IDashboardFacade _dashboardFacade;

    public HomeController(IAuthenticationFacade authenticationFacade, IDashboardFacade dashboardFacade)
    {
        _authenticationFacade = authenticationFacade;
        _dashboardFacade = dashboardFacade;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_authenticationFacade.IsSignedIn)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _dashboardFacade.GetDashboardAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return RedirectForFailure(result, fallbackAction: "Index", fallbackController: "Error");
        }

        return View(result.Value);
    }
}
