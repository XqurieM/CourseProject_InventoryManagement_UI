namespace InventoryManagement.UI.Models;

public sealed class UiLocalizationViewModel
{
    public string LanguageCode { get; init; } = "tr";
    public string PageName { get; init; } = "shared";
    public Dictionary<string, string> Resources { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
