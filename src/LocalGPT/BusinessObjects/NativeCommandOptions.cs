namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a native command options.
/// </summary>
public sealed class NativeCommandOptions
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "NativeCommands";

    /// <summary>
    /// Gets or sets enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets allow power shell workspace scripts.
    /// </summary>
    public bool AllowPowerShellWorkspaceScripts { get; set; }
    /// <summary>
    /// Gets or sets max duration seconds.
    /// </summary>
    public int MaxDurationSeconds { get; set; } = 600;
}
