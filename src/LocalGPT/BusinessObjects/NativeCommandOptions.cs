namespace LocalGPT.BusinessObjects;

/// <summary>
/// Carries the configurable native command settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class NativeCommandOptions
{
    /// <summary>
    /// Defines the section name constant used by <see cref="NativeCommandOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "NativeCommands";

    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the native command state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="NativeCommandOptions"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether power shell workspace scripts applies to the native command state.
    /// </summary>
    /// <value>The allow power shell workspace scripts value exposed by <see cref="NativeCommandOptions"/>.</value>
    public bool AllowPowerShellWorkspaceScripts { get; set; }
    /// <summary>
    /// Gets or sets the max duration seconds value that forms part of the native command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max duration seconds value exposed by <see cref="NativeCommandOptions"/>.</value>
    public int MaxDurationSeconds { get; set; } = 600;
}
