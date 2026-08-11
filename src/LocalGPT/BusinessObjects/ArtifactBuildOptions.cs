namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an artifact build options.
/// </summary>
public sealed class ArtifactBuildOptions
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "ArtifactBuilds";

    /// <summary>
    /// Gets or sets enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets max duration seconds.
    /// </summary>
    public int MaxDurationSeconds { get; set; } = 180;
}

/// <summary>
/// Represents an artifact build execution result.
/// </summary>
public sealed record ArtifactBuildExecutionResult(
    string Status,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded => string.Equals(Status, "BuildPassed", StringComparison.Ordinal);
    /// <summary>
    /// Gets or sets timed out.
    /// </summary>
    public bool TimedOut => string.Equals(Status, "TimedOut", StringComparison.Ordinal);
}
