using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IArtifactBuildExecutor
{
    Task<ArtifactBuildExecutionResult> BuildAsync(
        string targetPath,
        string allowedRoot,
        string configuration,
        string? outputDirectory,
        TimeSpan requestedTimeout,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false);
}
