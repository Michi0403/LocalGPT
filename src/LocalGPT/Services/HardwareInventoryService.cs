using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LocalGPT.Services;

/// <summary>
/// Read-only, cached hardware inventory used for council scheduling. It never changes device state.
/// GPU discovery is best-effort and falls back to explicit user-configured routes when vendor tools are unavailable.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class HardwareInventoryService(ILogger<HardwareInventoryService> logger) : IHardwareInventoryService
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

                var result = new List<OneWireHardwareDescriptor>
                {
                    new()
                    {
                        Kind = OneWireHardwareKind.Cpu,
                        Index = 0,
                        Name = $"{RuntimeInformation.ProcessArchitecture} CPU",
                        Vendor = RuntimeInformation.OSDescription,
                        LogicalProcessorCount = Environment.ProcessorCount,
                        IsOnline = true
                    }
                };

                foreach (var gpu in await ProbeNvidiaAsync(cancellationToken).ConfigureAwait(false))
                    if (result.All(existing => !string.Equals(existing.LaneKey, gpu.LaneKey, StringComparison.OrdinalIgnoreCase)))
                        result.Add(gpu);

                if (OperatingSystem.IsWindows())
                {
                    foreach (var gpu in await ProbeWindowsVideoControllersAsync(cancellationToken).ConfigureAwait(false))
                        if (result.All(existing => !string.Equals(existing.Name, gpu.Name, StringComparison.OrdinalIgnoreCase)))
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

    /// <summary>
    /// Performs probe nvidia as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeNvidiaAsync(CancellationToken cancellationToken)
    {
    try
    {
            var lines = await RunProbeAsync(
                "nvidia-smi",
                "--query-gpu=index,name,memory.total --format=csv,noheader,nounits",
                cancellationToken).ConfigureAwait(false);
            var result = new List<OneWireHardwareDescriptor>();
            foreach (var line in lines)
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                    continue;
                long? bytes = null;
                if (parts.Length >= 3 && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mib))
                    bytes = mib * 1024L * 1024L;
                result.Add(new OneWireHardwareDescriptor
                {
                    Kind = OneWireHardwareKind.Gpu,
                    Index = index,
                    Name = parts[1],
                    Vendor = "NVIDIA",
                    DedicatedMemoryBytes = bytes,
                    IsOnline = true
                });
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(ProbeNvidiaAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(ProbeNvidiaAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs probe windows video controllers as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeWindowsVideoControllersAsync(CancellationToken cancellationToken)
    {
    try
    {
            const string script = "$i=0; Get-CimInstance Win32_VideoController | ForEach-Object { '{0}|{1}|{2}' -f $i,$_.Name,$_.AdapterRAM; $i++ }";
            var lines = await RunProbeAsync("powershell", $"-NoProfile -NonInteractive -Command \"{script}\"", cancellationToken).ConfigureAwait(false);
            var result = new List<OneWireHardwareDescriptor>();
            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || !int.TryParse(parts[0], out var index))
                    continue;
                long? bytes = parts.Length >= 3 && long.TryParse(parts[2], out var parsed) ? parsed : null;
                result.Add(new OneWireHardwareDescriptor
                {
                    Kind = OneWireHardwareKind.Gpu,
                    Index = index,
                    Name = parts[1],
                    Vendor = InferVendor(parts[1]),
                    DedicatedMemoryBytes = bytes,
                    IsOnline = true
                });
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(ProbeWindowsVideoControllersAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HardwareInventoryService)}.{nameof(ProbeWindowsVideoControllersAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs run probe as part of the hardware inventory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the hardware inventory operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the hardware inventory operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<string>> RunProbeAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start()) return [];
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            if (process.ExitCode != 0) return [];
            var output = await outputTask.ConfigureAwait(false);
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
        {
            logger.LogDebug(ex, "Hardware probe {Probe} is unavailable; user-configured hardware routes remain usable.", fileName);
            return [];
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
