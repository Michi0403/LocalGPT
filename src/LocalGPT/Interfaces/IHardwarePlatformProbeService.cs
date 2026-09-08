using LocalGPT.WireProtocol;

namespace LocalGPT.Interfaces;

/// <summary>Provides host-specific read-only hardware probes behind one cross-platform inventory contract.</summary>
public interface IHardwarePlatformProbeService
{
    /// <summary>Returns host-specific GPU descriptors that are not covered by vendor-neutral probes.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbePlatformGpusAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns NVIDIA GPU descriptors through an optional vendor tool when the current platform supports it safely.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the read-only probe.</param>
    /// <returns>NVIDIA GPU descriptors, or an empty collection when the optional vendor tool is unavailable.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeNvidiaGpusAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns total physical/system memory in bytes when the current platform can report it safely.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the read-only probe.</param>
    /// <returns>Total physical memory in bytes, or <see langword="null"/> when unavailable.</returns>
    Task<long?> ProbeSystemMemoryBytesAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns the current host CPU/model name when the platform can report it safely.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the read-only probe.</param>
    /// <returns>The CPU/model name, or an empty string when unavailable.</returns>
    Task<string> ProbeCpuNameAsync(CancellationToken cancellationToken = default);
}
