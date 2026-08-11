namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a browser theme state.
/// </summary>
internal sealed class BrowserThemeState
{
    /// <summary>
    /// Gets or sets shell theme name.
    /// </summary>
    public string? ShellThemeName { get; set; }
    /// <summary>
    /// Gets or sets component theme name.
    /// </summary>
    public string? ComponentThemeName { get; set; }
    /// <summary>
    /// Gets or sets fusion route.
    /// </summary>
    public List<BrowserThemeFusionStep>? FusionRoute { get; set; }
}

/// <summary>
/// Represents a browser theme fusion step.
/// </summary>
internal sealed class BrowserThemeFusionStep
{
    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public int Sequence { get; set; }
    /// <summary>
    /// Gets or sets target.
    /// </summary>
    public string? Target { get; set; }
    /// <summary>
    /// Gets or sets theme name.
    /// </summary>
    public string? ThemeName { get; set; }
}

/// <summary>
/// Represents a logger null scope.
/// </summary>
internal sealed class LoggerNullScope : IDisposable
{
    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
    public void Dispose()
    {
    }
}
