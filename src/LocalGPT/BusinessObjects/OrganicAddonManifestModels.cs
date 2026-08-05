namespace LocalGPT.BusinessObjects;

/// <summary>
/// Offline, source-controlled description of an organic add-on. The manifest makes an add-on's
/// controller surface discoverable while its process is offline; invocation still requires a live,
/// trusted 1-Wire peer and the existing user-controlled catalog policy.
/// </summary>
public sealed class OrganicAddonManifest
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourcePeerId { get; set; } = string.Empty;
    public List<string> Organs { get; set; } = [];
    public List<string> CapabilityKeys { get; set; } = [];
    public List<string> UiActivationKeys { get; set; } = [];
    public List<OrganicAddonControllerMethodManifest> ControllerMethods { get; set; } = [];
}

public sealed class OrganicAddonControllerMethodManifest
{
    public string Controller { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string Route { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}
