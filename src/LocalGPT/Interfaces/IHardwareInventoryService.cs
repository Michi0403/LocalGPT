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
    /// <summary>Returns total physical/system memory through the maintained platform hardware probe.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the read-only probe.</param>
    /// <returns>Total physical memory in bytes, or <see langword="null"/> when unavailable.</returns>
    Task<long?> GetSystemMemoryBytesAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns the detected CPU/model name through the maintained platform hardware probe.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the read-only probe.</param>
    /// <returns>The CPU/model name, or an empty string when unavailable.</returns>
    Task<string> GetCpuNameAsync(CancellationToken cancellationToken = default);
}
