using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for runtime capability directory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRuntimeCapabilityDirectoryService
{
    /// <summary>
    /// Performs synchronize as part of the runtime capability directory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The runtime capability directory snapshot produced by the operation.</returns>
    Task<RuntimeCapabilityDirectorySnapshot> SynchronizeAsync(CancellationToken cancellationToken = default);
}
