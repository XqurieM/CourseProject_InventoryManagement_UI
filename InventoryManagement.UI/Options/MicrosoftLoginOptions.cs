namespace InventoryManagement.UI.Options;

public sealed class MicrosoftLoginOptions
{
    public const string SectionName = "MicrosoftLogin";

    public string ClientId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
