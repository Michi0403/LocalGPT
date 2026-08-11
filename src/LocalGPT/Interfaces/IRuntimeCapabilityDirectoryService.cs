using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the runtime capability directory service contract.
/// </summary>
public interface IRuntimeCapabilityDirectoryService
{
    /// <summary>
    /// Runs the synchronize async operation.
    /// </summary>
    Task<RuntimeCapabilityDirectorySnapshot> SynchronizeAsync(CancellationToken cancellationToken = default);
}
