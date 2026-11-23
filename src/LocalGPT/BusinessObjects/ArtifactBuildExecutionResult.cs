namespace LocalGPT.BusinessObjects;

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
