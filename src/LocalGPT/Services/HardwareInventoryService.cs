using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Runtime.InteropServices;

namespace LocalGPT.Services;

/// <summary>
/// Read-only, cached hardware inventory used for council scheduling. It never changes device state.
/// GPU discovery is best-effort and falls back to explicit user-configured routes when vendor tools are unavailable.
/// </summary>
/// <param name="platformProbe">Platform-specific read-only hardware probe used to discover local devices without changing device state.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class HardwareInventoryService(
    IHardwarePlatformProbeService platformProbe,
    ILogger<HardwareInventoryService> logger) : IHardwareInventoryService
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="HardwareInventoryService"/>.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>
    /// Stores the in-memory cached collection maintained internally by <see cref="HardwareInventoryService"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<OneWireHardwareDescriptor>? cached;
    /// <summary>
    /// Stores the internal cache UTC state used by <see cref="HardwareInventoryService"/> while executing its surrounding workflow.
    /// </summary>
    private DateTimeOffset cacheUtc;

    /// <summary>
    /// Retrieves hardware as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            if (cached is not null && DateTimeOffset.UtcNow - cacheUtc < TimeSpan.FromMinutes(2))
                return cached.Select(Clone).ToList();

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (cached is not null && DateTimeOffset.UtcNow - cacheUtc < TimeSpan.FromMinutes(2))
                    return cached.Select(Clone).ToList();

                var cpuName = await GetCpuNameAsync(cancellationToken).ConfigureAwait(false);
                var result = new List<OneWireHardwareDescriptor>
                {
                    new()
                    {
                        Kind = OneWireHardwareKind.Cpu,
                        Index = 0,
                        Name = string.IsNullOrWhiteSpace(cpuName) ? $"{RuntimeInformation.ProcessArchitecture} CPU" : cpuName,
                        Vendor = InferVendor(cpuName),
                        LogicalProcessorCount = Environment.ProcessorCount,
                        IsOnline = true
                    }
                };

                foreach (var gpu in await platformProbe.ProbeNvidiaGpusAsync(cancellationToken).ConfigureAwait(false))
                    if (result.All(existing => !string.Equals(existing.LaneKey, gpu.LaneKey, StringComparison.OrdinalIgnoreCase)))
                        result.Add(gpu);

                IReadOnlyList<OneWireHardwareDescriptor> platformGpus;
                try
                {
                    platformGpus = await platformProbe.ProbePlatformGpusAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogDebug(exception, "Platform GPU discovery was unavailable; the remaining hardware inventory stays usable.");
                    platformGpus = [];
                }

                foreach (var gpu in platformGpus)
                {
                    if (result.All(existing => !string.Equals(existing.LaneKey, gpu.LaneKey, StringComparison.OrdinalIgnoreCase) &&
                                               !string.Equals(existing.Name, gpu.Name, StringComparison.OrdinalIgnoreCase)))
                        result.Add(gpu);
                }

                cached = result;
                cacheUtc = DateTimeOffset.UtcNow;
                return result.Select(Clone).ToList();
            }
            finally
            {
                gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(GetHardwareAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(GetHardwareAsync)} failed.");
        throw;
    }
}

    /// <summary>Returns the CPU/model name through the existing platform-specific read-only probe.</summary>
    /// <inheritdoc />
    public async Task<string> GetCpuNameAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return (await platformProbe.ProbeCpuNameAsync(cancellationToken).ConfigureAwait(false)).Trim();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "CPU hardware probe was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "CPU hardware probe was unavailable.");
            return string.Empty;
        }
    }

    /// <summary>Returns total physical/system memory through the existing platform-specific read-only probe.</summary>
    /// <inheritdoc />
    public async Task<long?> GetSystemMemoryBytesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await platformProbe.ProbeSystemMemoryBytesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "System-memory hardware probe was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "System-memory hardware probe was unavailable.");
            return null;
        }
    }

    /// <summary>
    /// Performs infer vendor as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the hardware inventory operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferVendor(string name)
    {
    try
    {
            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            return string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(InferVendor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(InferVendor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="item">Item value supplied to the hardware inventory operation and used when producing its result.</param>
    /// <returns>The one wire hardware descriptor produced by the operation.</returns>
    private OneWireHardwareDescriptor Clone(OneWireHardwareDescriptor item) {
    try
    {
        return new()
    {
        Kind = item.Kind,
        Index = item.Index,
        Name = item.Name,
        Vendor = item.Vendor,
        DedicatedMemoryBytes = item.DedicatedMemoryBytes,
        LogicalProcessorCount = item.LogicalProcessorCount,
        IsOnline = item.IsOnline
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(Clone)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(Clone)} failed.");
        throw;
    }
}
}
