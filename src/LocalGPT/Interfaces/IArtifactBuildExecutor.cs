using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for artifact build executor behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IArtifactBuildExecutor
{
    /// <summary>
    /// Performs build for <see cref="IArtifactBuildExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding artifact build executor workflow.
    /// </summary>
    /// <param name="targetPath">Target path value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="allowedRoot">Allowed root value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <param name="outputDirectory">Output directory value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="requestedTimeout">Requested timeout value supplied to the artifact build executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <returns>The artifact build execution result produced by the operation.</returns>
    Task<ArtifactBuildExecutionResult> BuildAsync(
        string targetPath,
        string allowedRoot,
        string configuration,
        string? outputDirectory,
        TimeSpan requestedTimeout,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false);
}
