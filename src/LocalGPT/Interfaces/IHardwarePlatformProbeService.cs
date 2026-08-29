using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>Provides host-specific read-only hardware probes behind one cross-platform inventory contract.</summary>
public interface IHardwarePlatformProbeService
{
    /// <summary>Returns host-specific GPU descriptors that are not covered by vendor-neutral probes.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbePlatformGpusAsync(CancellationToken cancellationToken = default);
}
