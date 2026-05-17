using InventoryManagement.UI.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryManagement.UI.Services;

public static class HtmlLocalizationExtensions
{
    public static IHtmlContent T(this IHtmlHelper html, string key, string fallback)
    {
        var state = html.ViewData["LocalizationState"] as UiLocalizationViewModel;
        var value = state?.Resources.TryGetValue(key, out var localizedValue) == true && !string.IsNullOrWhiteSpace(localizedValue)
            ? localizedValue
            : fallback;

        return new HtmlString(System.Net.WebUtility.HtmlEncode(value));
    }
}
