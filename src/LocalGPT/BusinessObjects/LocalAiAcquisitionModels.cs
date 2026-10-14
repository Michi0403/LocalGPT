namespace LocalGPT.BusinessObjects;

/// <summary>Describes one selectable model variant supplied by a reviewed upstream AI project.</summary>
public sealed class LocalAiKnownModelVariant
{
    /// <summary>Gets or sets the stable variant key passed to the Python runtime.</summary>
    /// <value>The model variant key.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the local AI known model variant state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display label.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hint value that forms part of the local AI known model variant state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The operator hint.</value>
    public string Hint { get; set; } = string.Empty;
}

/// <summary>Describes one reviewed upstream local-AI project that can be downloaded directly without a model-hub client.</summary>
public sealed class LocalAiKnownModelDefinition
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this local AI known model definition instance with related application state.
    /// </summary>
    /// <value>The source key.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the local AI known model definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the publisher value that forms part of the local AI known model definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publisher.</value>
    public string Publisher { get; set; } = string.Empty;
    /// <summary>Gets or sets a concise description of the model capability and LocalGPT integration state.</summary>
    /// <value>The catalog description.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the repository URI that identifies the network or application endpoint associated with this local AI known model definition state.
    /// </summary>
    /// <value>The project URI.</value>
    public string RepositoryUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source archive URI that identifies the network or application endpoint associated with this local AI known model definition state.
    /// </summary>
    /// <value>The HTTPS source archive URI.</value>
    public string SourceArchiveUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source file name used by this local AI known model definition instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The managed source file name.</value>
    public string SourceFileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the runtime adapter used when the project is executable through LocalGPT.</summary>
    /// <value>The runtime adapter key.</value>
    public string Adapter { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this local AI known model definition instance for downstream processing.
    /// </summary>
    /// <value>The LocalGPT capability bindings.</value>
    public List<LocalAiCapability> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the variants collection maintained or exposed by this local AI known model definition instance for downstream processing.
    /// </summary>
    /// <value>The known model variants.</value>
    public List<LocalAiKnownModelVariant> Variants { get; set; } = [];
    /// <summary>Gets or sets whether this LocalGPT release can install and execute the project instead of only downloading its source.</summary>
    /// <value><see langword="true"/> when runtime installation is implemented.</value>
    public bool CanInstall { get; set; }
    /// <summary>Gets or sets additional runtime prerequisites that the operator should review.</summary>
    /// <value>The prerequisite hint.</value>
    public string RuntimePrerequisites { get; set; } = string.Empty;
}

/// <summary>Requests direct source download or executable installation of one reviewed local-AI catalog entry.</summary>
public sealed class LocalAiKnownModelInstallRequest
{
    /// <summary>
    /// Gets or sets the stable source key used to identify or correlate this local AI known model install instance with related application state.
    /// </summary>
    /// <value>The catalog source key.</value>
    public string SourceKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the reviewed variant to install when the project supports runtime installation.</summary>
    /// <value>The selected variant key.</value>
    public string Variant { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the human operator explicitly approved download and installation.</summary>
    /// <value><see langword="true"/> when exact confirmation was supplied.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Reports a direct local-AI project source download.</summary>
public sealed class LocalAiSourceDownloadResult
{
    /// <summary>
    /// Gets or sets the stable source key used to identify or correlate this local AI source download instance with related application state.
    /// </summary>
    /// <value>The catalog source key.</value>
    public string SourceKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the local path used by this local AI source download instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The downloaded source path.</value>
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the bytes value that forms part of the local AI source download state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source archive byte count.</value>
    public long Bytes { get; set; }
    /// <summary>Gets or sets the SHA-256 digest of the source archive.</summary>
    /// <value>The lowercase hexadecimal digest.</value>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the downloaded at UTC associated with this local AI source download state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The UTC completion timestamp.</value>
    public DateTime DownloadedAtUtc { get; set; } = DateTime.UtcNow;
}
