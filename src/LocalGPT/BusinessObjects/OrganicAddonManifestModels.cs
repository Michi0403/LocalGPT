namespace LocalGPT.BusinessObjects;

/// <summary>
/// Offline, source-controlled description of an organic add-on. The manifest makes an add-on's
/// controller surface discoverable while its process is offline; invocation still requires a live,
/// trusted 1-Wire peer and the existing user-controlled catalog policy.
/// </summary>
public sealed class OrganicAddonManifest
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets organs.
    /// </summary>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets capability keys.
    /// </summary>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets UI activation keys.
    /// </summary>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets controller methods.
    /// </summary>
    public List<OrganicAddonControllerMethodManifest> ControllerMethods { get; set; } = [];
}

/// <summary>
/// Represents an organic addon controller method manifest.
/// </summary>
public sealed class OrganicAddonControllerMethodManifest
{
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets HTTP method.
    /// </summary>
    public string HttpMethod { get; set; } = "POST";
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is read only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}
