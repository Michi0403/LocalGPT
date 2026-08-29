namespace LocalGPT.BusinessObjects;

/// <summary>Represents one host-resolved shell invocation before it is materialized as a process.</summary>
public sealed class LocalConsolePlatformCommand
{
    /// <summary>
    /// Gets or sets the executable value that forms part of the local console platform command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The executable value exposed by <see cref="LocalConsolePlatformCommand"/>.</value>
    public string Executable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments collection maintained or exposed by this local console platform command instance for downstream processing.
    /// </summary>
    /// <value>The arguments value exposed by <see cref="LocalConsolePlatformCommand"/>.</value>
    public List<string> Arguments { get; set; } = [];

    /// <summary>
    /// Gets or sets the shell value that forms part of the local console platform command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shell value exposed by <see cref="LocalConsolePlatformCommand"/>.</value>
    public LocalConsoleShellKind Shell { get; set; }

    /// <summary>Gets or sets a safe human-readable command description that contains no user command text.</summary>
    /// <value>The display command value exposed by <see cref="LocalConsolePlatformCommand"/>.</value>
    public string DisplayCommand { get; set; } = string.Empty;
}
