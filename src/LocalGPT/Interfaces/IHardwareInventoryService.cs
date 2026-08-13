using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for hardware inventory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IHardwareInventoryService
{
    /// <summary>
    /// Retrieves hardware as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default);
}
