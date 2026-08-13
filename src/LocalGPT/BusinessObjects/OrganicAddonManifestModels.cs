namespace LocalGPT.BusinessObjects;

/// <summary>
/// Offline, source-controlled description of an organic add-on. The manifest makes an add-on's
/// controller surface discoverable while its process is offline; invocation still requires a live,
/// trusted 1-Wire peer and the existing user-controlled catalog policy.
/// </summary>
public sealed class OrganicAddonManifest
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this organic addon manifest instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the organic addon manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the organic addon manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this organic addon manifest instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this organic addon manifest instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets the capability keys collection maintained or exposed by this organic addon manifest instance for downstream processing.
    /// </summary>
    /// <value>The capability keys value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI activation keys collection maintained or exposed by this organic addon manifest instance for downstream processing.
    /// </summary>
    /// <value>The UI activation keys value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the controller methods collection maintained or exposed by this organic addon manifest instance for downstream processing.
    /// </summary>
    /// <value>The controller methods value exposed by <see cref="OrganicAddonManifest"/>.</value>
    public List<OrganicAddonControllerMethodManifest> ControllerMethods { get; set; } = [];
}

/// <summary>
/// Represents an organic addon controller method manifest application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicAddonControllerMethodManifest
{
    /// <summary>
    /// Gets or sets the controller value that forms part of the organic addon controller method manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the method name value that forms part of the organic addon controller method manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method name value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the HTTP method value that forms part of the organic addon controller method manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTTP method value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public string HttpMethod { get; set; } = "POST";
    /// <summary>
    /// Gets or sets the route value that forms part of the organic addon controller method manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the organic addon controller method manifest state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the organic addon controller method manifest state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="OrganicAddonControllerMethodManifest"/>.</value>
    public bool IsReadOnly { get; set; }
}
