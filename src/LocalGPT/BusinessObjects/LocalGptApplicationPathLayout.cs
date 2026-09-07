namespace LocalGPT.BusinessObjects;

/// <summary>Describes the effective LocalGPT folder contract detected for the current host and user.</summary>
public sealed class LocalGptApplicationPathLayout
{
    /// <summary>
    /// Gets or sets the platform value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string Platform { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the user data root value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user data root value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string UserDataRoot { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the configuration file value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The configuration file value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string ConfigurationFile { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the database file value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The database file value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string DatabaseFile { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the runtime directory used by this local GPT application path layout instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The runtime directory value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string RuntimeDirectory { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the logs directory used by this local GPT application path layout instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The logs directory value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string LogsDirectory { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the knowledge directory used by this local GPT application path layout instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The knowledge directory value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string KnowledgeDirectory { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the portable application root value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The portable application root value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string PortableApplicationRoot { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the system wide discovery roots collection maintained or exposed by this local GPT application path layout instance for downstream processing.
    /// </summary>
    /// <value>The system wide discovery roots value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public IReadOnlyList<string> SystemWideDiscoveryRoots { get; init; } = Array.Empty<string>();
    /// <summary>
    /// Gets or sets the layout report file value that forms part of the local GPT application path layout state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The layout report file value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public string LayoutReportFile { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether first boot detected applies to the local GPT application path layout state.
    /// </summary>
    /// <value>The first boot detected value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public bool FirstBootDetected { get; init; }
    /// <summary>
    /// Gets or sets the generated at UTC associated with this local GPT application path layout state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated at UTC value exposed by <see cref="LocalGptApplicationPathLayout"/>.</value>
    public DateTime GeneratedAtUtc { get; init; }
}
