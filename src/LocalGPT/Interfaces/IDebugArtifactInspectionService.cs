using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for debug artifact inspection behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDebugArtifactInspectionService
{
    /// <summary>
    /// Performs inspect as part of the debug artifact inspection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filePath">File path value supplied to the debug artifact inspection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The debug artifact inspection result produced by the operation.</returns>
    Task<DebugArtifactInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}
