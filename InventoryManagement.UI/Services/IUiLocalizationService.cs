using InventoryManagement.UI.Models;
using Microsoft.AspNetCore.Http;

namespace InventoryManagement.UI.Services;

public interface IUiLocalizationService
{
    Task<UiLocalizationViewModel> GetForRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<string> GetTextAsync(HttpContext httpContext, string key, string fallback, string? pageName = null, CancellationToken cancellationToken = default);
}
