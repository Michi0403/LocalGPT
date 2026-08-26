namespace LocalGPT.BusinessObjects;

/// <summary>Represents one host-resolved shell invocation before it is materialized as a process.</summary>
public sealed class LocalConsolePlatformCommand
{
    /// <summary>Gets or sets the resolved executable.</summary>
    public string Executable { get; set; } = string.Empty;

    /// <summary>Gets or sets the resolved argument list.</summary>
    public List<string> Arguments { get; set; } = [];

    /// <summary>Gets or sets the shell family represented by this invocation.</summary>
    public LocalConsoleShellKind Shell { get; set; }

    /// <summary>Gets or sets a safe human-readable command description that contains no user command text.</summary>
    public string DisplayCommand { get; set; } = string.Empty;
}
