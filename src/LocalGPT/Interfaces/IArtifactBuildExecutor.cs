using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the artifact build executor contract.
/// </summary>
public interface IArtifactBuildExecutor
{
    /// <summary>
    /// Builds async.
    /// </summary>
    Task<ArtifactBuildExecutionResult> BuildAsync(
        string targetPath,
        string allowedRoot,
        string configuration,
        string? outputDirectory,
        TimeSpan requestedTimeout,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false);
}
