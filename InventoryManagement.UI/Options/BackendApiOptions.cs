namespace InventoryManagement.UI.Options;

public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    public string BaseUrl { get; set; } = "https://localhost:7203";
}
