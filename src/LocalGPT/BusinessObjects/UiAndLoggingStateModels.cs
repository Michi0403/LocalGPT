namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents browser theme state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
internal sealed class BrowserThemeState
{
    /// <summary>
    /// Gets or sets the shell theme name value that forms part of the browser theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shell theme name value exposed by <see cref="BrowserThemeState"/>.</value>
    public string? ShellThemeName { get; set; }
    /// <summary>
    /// Gets or sets the component theme name value that forms part of the browser theme state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The component theme name value exposed by <see cref="BrowserThemeState"/>.</value>
    public string? ComponentThemeName { get; set; }
    /// <summary>
    /// Gets or sets the fusion route collection maintained or exposed by this browser theme instance for downstream processing.
    /// </summary>
    /// <value>The fusion route value exposed by <see cref="BrowserThemeState"/>.</value>
    public List<BrowserThemeFusionStep>? FusionRoute { get; set; }
}

/// <summary>
/// Represents a browser theme fusion step application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class BrowserThemeFusionStep
{
    /// <summary>
    /// Gets or sets the sequence value that forms part of the browser theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="BrowserThemeFusionStep"/>.</value>
    public int Sequence { get; set; }
    /// <summary>
    /// Gets or sets the target value that forms part of the browser theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target value exposed by <see cref="BrowserThemeFusionStep"/>.</value>
    public string? Target { get; set; }
    /// <summary>
    /// Gets or sets the theme name value that forms part of the browser theme fusion step state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The theme name value exposed by <see cref="BrowserThemeFusionStep"/>.</value>
    public string? ThemeName { get; set; }
}

/// <summary>
/// Represents a logger null scope application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class LoggerNullScope : IDisposable
{
    /// <summary>
    /// Releases resources owned by <see cref="LoggerNullScope"/> and leaves the logger null scope workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
    }
}
