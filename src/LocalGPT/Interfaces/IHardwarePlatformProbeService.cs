using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>Provides host-specific read-only hardware probes behind one cross-platform inventory contract.</summary>
public interface IHardwarePlatformProbeService
{
    /// <summary>Returns host-specific GPU descriptors that are not covered by vendor-neutral probes.</summary>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbePlatformGpusAsync(CancellationToken cancellationToken = default);
}
