using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryManagement.UI.Filters;

public sealed class PopulateLayoutStateFilter : IAsyncActionFilter
{
    private readonly IUserSessionService _userSessionService;
    private readonly IUiLocalizationService _uiLocalizationService;

    public PopulateLayoutStateFilter(IUserSessionService userSessionService, IUiLocalizationService uiLocalizationService)
    {
        _userSessionService = userSessionService;
        _uiLocalizationService = uiLocalizationService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            var user = _userSessionService.GetUser();
            controller.ViewData["LayoutState"] = new LayoutViewModel
            {
                IsAuthenticated = _userSessionService.IsAuthenticated,
                DisplayName = user?.UserName ?? "Guest",
                RoleLabel = user?.IsAdmin == true ? "Admin" : "User",
                User = user
            };

            controller.ViewData["LocalizationState"] =
                await _uiLocalizationService.GetForRequestAsync(context.HttpContext, context.HttpContext.RequestAborted);
        }

        await next();
    }
}
