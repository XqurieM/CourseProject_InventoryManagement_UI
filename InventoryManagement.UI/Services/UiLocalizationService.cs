using CourseProject_InventoryManagement.Application.Features.CQRS.Results.GeneralResults;
using InventoryManagement.UI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryManagement.UI.Services;

public sealed class UiLocalizationService : IUiLocalizationService
{
    private readonly BackendApiClient _backendApiClient;
    private readonly IUserSessionService _userSessionService;
    private readonly IMemoryCache _memoryCache;

    public UiLocalizationService(
        BackendApiClient backendApiClient,
        IUserSessionService userSessionService,
        IMemoryCache memoryCache)
    {
        _backendApiClient = backendApiClient;
        _userSessionService = userSessionService;
        _memoryCache = memoryCache;
    }

    public async Task<UiLocalizationViewModel> GetForRequestAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var languageCode = ResolveLanguageCode();
        var pageName = ResolvePageName(httpContext);

        var sharedResources = await GetPageResourcesAsync(languageCode, "shared", cancellationToken);
        var pageResources = pageName.Equals("shared", StringComparison.OrdinalIgnoreCase)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await GetPageResourcesAsync(languageCode, pageName, cancellationToken);

        var merged = new Dictionary<string, string>(sharedResources, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pageResources)
        {
            merged[pair.Key] = pair.Value;
        }

        return new UiLocalizationViewModel
        {
            LanguageCode = languageCode,
            PageName = pageName,
            Resources = merged
        };
    }

    public async Task<string> GetTextAsync(HttpContext httpContext, string key, string fallback, string? pageName = null, CancellationToken cancellationToken = default)
    {
        var state = await GetForRequestAsync(httpContext, cancellationToken);
        if (!string.IsNullOrWhiteSpace(pageName) &&
            !pageName.Equals(state.PageName, StringComparison.OrdinalIgnoreCase))
        {
            var directPageResources = await GetPageResourcesAsync(state.LanguageCode, pageName, cancellationToken);
            if (directPageResources.TryGetValue(key, out var directValue) && !string.IsNullOrWhiteSpace(directValue))
            {
                return directValue;
            }
        }

        return state.Resources.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private async Task<Dictionary<string, string>> GetPageResourcesAsync(string languageCode, string pageName, CancellationToken cancellationToken)
    {
        var cacheKey = $"ui-localization:{languageCode}:{pageName}";
        if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, string>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _backendApiClient.GetAsync<GetLocalizationResourcesResult>(
            $"General/GetLocalizationResources?languageCode={Uri.EscapeDataString(languageCode)}&pageName={Uri.EscapeDataString(pageName)}",
            requiresAuth: false,
            cancellationToken);

        var resources = result.IsSuccess && result.Value is not null
            ? new Dictionary<string, string>(result.Value.Resources, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _memoryCache.Set(cacheKey, resources, TimeSpan.FromMinutes(10));
        return resources;
    }

    private string ResolveLanguageCode()
    {
        var storedLanguage = _userSessionService.GetLanguageCode();
        if (!string.IsNullOrWhiteSpace(storedLanguage))
        {
            return NormalizeLanguageCode(storedLanguage);
        }

        var currentUser = _userSessionService.GetUser();
        if (currentUser is not null)
        {
            return NormalizeLanguageCode(MapLanguageCode(currentUser.PreferredLanguage));
        }

        return "tr";
    }

    private static string ResolvePageName(HttpContext httpContext)
    {
        var controller = httpContext.GetRouteValue("controller")?.ToString()?.Trim().ToLowerInvariant();
        var action = httpContext.GetRouteValue("action")?.ToString()?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
        {
            return "shared";
        }

        return $"{controller}.{action}";
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        var normalized = languageCode.Trim().ToLowerInvariant();
        return normalized switch
        {
            "tr-tr" => "tr",
            "en-us" or "en-gb" => "en",
            _ => normalized
        };
    }

    private static string MapLanguageCode(CourseProject_InventoryManagement.Domain.Enums.LanguageType languageType) =>
        languageType switch
        {
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Turkish => "tr",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.English => "en",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.German => "de",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.French => "fr",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Spanish => "es",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Italian => "it",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Russian => "ru",
            CourseProject_InventoryManagement.Domain.Enums.LanguageType.Chinese => "zh",
            _ => "en"
        };
}
