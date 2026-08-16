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

                if (OperatingSystem.IsLinux())
                {
                    foreach (var gpu in await ProbeLinuxDrmAsync(cancellationToken).ConfigureAwait(false))
                        if (result.All(existing => !string.Equals(existing.LaneKey, gpu.LaneKey, StringComparison.OrdinalIgnoreCase) &&
                                                   !(string.Equals(existing.Vendor, gpu.Vendor, StringComparison.OrdinalIgnoreCase) && existing.Index == gpu.Index)))
                            result.Add(gpu);
                }

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
    /// Reads Linux DRM/sysfs GPU identity and dedicated VRAM without depending on a desktop environment or Windows APIs.
    /// AMDGPU exposes <c>mem_info_vram_total</c> in bytes; other DRM drivers remain useful identity evidence when that file is absent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop local read-only discovery.</param>
    /// <returns>The GPU descriptors discovered through Linux DRM/sysfs.</returns>
    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeLinuxDrmAsync(CancellationToken cancellationToken)
    {
        try
        {
            const string drmRoot = "/sys/class/drm";
            if (!Directory.Exists(drmRoot))
                return [];

            var result = new List<OneWireHardwareDescriptor>();
            foreach (var cardPath in Directory.EnumerateDirectories(drmRoot, "card*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cardName = Path.GetFileName(cardPath);
                if (cardName.Length <= 4 || !int.TryParse(cardName[4..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                    continue;

                var devicePath = Path.Combine(cardPath, "device");
                if (!Directory.Exists(devicePath))
                    continue;

                var vendorCode = await ReadTrimmedFileAsync(Path.Combine(devicePath, "vendor"), cancellationToken).ConfigureAwait(false);
                var deviceCode = await ReadTrimmedFileAsync(Path.Combine(devicePath, "device"), cancellationToken).ConfigureAwait(false);
                var vendor = vendorCode.ToLowerInvariant() switch
                {
                    "0x1002" => "AMD",
                    "0x10de" => "NVIDIA",
                    "0x8086" => "Intel",
                    _ => string.IsNullOrWhiteSpace(vendorCode) ? "DRM" : vendorCode
                };
                long? dedicatedMemoryBytes = null;
                var vramText = await ReadTrimmedFileAsync(Path.Combine(devicePath, "mem_info_vram_total"), cancellationToken).ConfigureAwait(false);
                if (long.TryParse(vramText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vramBytes) && vramBytes > 0)
                    dedicatedMemoryBytes = vramBytes;

                var driver = string.Empty;
                try
                {
                    var driverPath = Path.Combine(devicePath, "driver");
                    if (Directory.Exists(driverPath))
                        driver = new DirectoryInfo(driverPath).LinkTarget is { Length: > 0 } linkTarget
                            ? Path.GetFileName(linkTarget.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                            : string.Empty;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    logger.LogDebug(exception, "Linux DRM driver link could not be read for card {CardIndex}.", index);
                }

                var identity = string.IsNullOrWhiteSpace(deviceCode)
                    ? $"{vendor} GPU {cardName}"
                    : $"{vendor} GPU {cardName} ({deviceCode})";
                if (!string.IsNullOrWhiteSpace(driver))
                    identity += $" · {driver}";

                result.Add(new OneWireHardwareDescriptor
                {
                    Kind = OneWireHardwareKind.Gpu,
                    Index = index,
                    Name = identity,
                    Vendor = vendor,
                    DedicatedMemoryBytes = dedicatedMemoryBytes,
                    IsOnline = true
                });
            }
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Linux DRM hardware discovery was cancelled.");
            else
                logger.LogError(exception, "Linux DRM hardware discovery failed; device details were omitted.");
            throw;
        }
    }

    /// <summary>Reads one small Linux sysfs text file and treats unavailable optional fields as empty evidence.</summary>
    /// <param name="path">Absolute sysfs file path.</param>
    /// <param name="cancellationToken">Cancellation token for the read.</param>
    /// <returns>The trimmed file text, or an empty string when the optional sysfs field is unavailable.</returns>
    private async Task<string> ReadTrimmedFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
                return string.Empty;
            return (await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Optional Linux hardware sysfs field could not be read.");
            return string.Empty;
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
            // Win32_VideoController.AdapterRAM is a legacy 32-bit field that saturates near 4 GiB on modern GPUs.
            // Use this Windows fallback only for device identity; authoritative VRAM comes from vendor probes or configured-host hardware.
            const string script = "$i=0; Get-CimInstance Win32_VideoController | ForEach-Object { '{0}|{1}' -f $i,$_.Name; $i++ }";
            var lines = await RunProbeAsync("powershell", $"-NoProfile -NonInteractive -Command \"{script}\"", cancellationToken).ConfigureAwait(false);
            var result = new List<OneWireHardwareDescriptor>();
            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || !int.TryParse(parts[0], out var index))
                    continue;
                result.Add(new OneWireHardwareDescriptor
                {
                    Kind = OneWireHardwareKind.Gpu,
                    Index = index,
                    Name = parts[1],
                    Vendor = InferVendor(parts[1]),
                    DedicatedMemoryBytes = null,
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
