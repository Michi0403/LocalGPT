namespace LocalGPT.BusinessObjects;

/// <summary>
/// Carries the configurable artifact build settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class ArtifactBuildOptions
{
    /// <summary>
    /// Defines the section name constant used by <see cref="ArtifactBuildOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "ArtifactBuilds";

    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the artifact build state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="ArtifactBuildOptions"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets the max duration seconds value that forms part of the artifact build state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max duration seconds value exposed by <see cref="ArtifactBuildOptions"/>.</value>
    public int MaxDurationSeconds { get; set; } = 180;
}

/// <summary>
/// Represents the outcome of artifact build execution, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="Status">Status value supplied to the artifact build execution operation and used when producing its result.</param>
/// <param name="ExitCode">Exit code value supplied to the artifact build execution operation and used when producing its result.</param>
/// <param name="StandardOutput">Standard output value supplied to the artifact build execution operation and used when producing its result.</param>
/// <param name="StandardError">Standard error value supplied to the artifact build execution operation and used when producing its result.</param>
/// <param name="Duration">Duration value supplied to the artifact build execution operation and used when producing its result.</param>
public sealed record ArtifactBuildExecutionResult(
    string Status,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded applies to the artifact build execution state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="ArtifactBuildExecutionResult"/>.</value>
    public bool Succeeded => string.Equals(Status, "BuildPassed", StringComparison.Ordinal);
    /// <summary>
    /// Gets a value indicating whether timed out applies to the artifact build execution state.
    /// </summary>
    /// <value>The timed out value exposed by <see cref="ArtifactBuildExecutionResult"/>.</value>
    public bool TimedOut => string.Equals(Status, "TimedOut", StringComparison.Ordinal);
}
