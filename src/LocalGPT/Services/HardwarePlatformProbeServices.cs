using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LocalGPT.Services;

/// <summary>Windows-specific read-only GPU inventory fallback.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WindowsHardwarePlatformProbeService(ILogger<WindowsHardwarePlatformProbeService> logger) : IHardwarePlatformProbeService
{
    /// <summary>
    /// Performs probe platform gpus as part of the windows hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbePlatformGpusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string script = "$i=0; Get-CimInstance Win32_VideoController | ForEach-Object { '{0}|{1}' -f $i,$_.Name; $i++ }";
            var lines = await RunProbeAsync("powershell", $"-NoProfile -NonInteractive -Command \"{script}\"", cancellationToken).ConfigureAwait(false);
            var result = new List<OneWireHardwareDescriptor>();
            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Windows GPU fallback discovery was unavailable.");
            return [];
        }
    }

    /// <summary>
    /// Performs run probe as part of the windows hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the windows hardware platform probe operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the windows hardware platform probe operation and used when producing its result.</param>
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
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
        {
            logger.LogDebug(exception, "Windows GPU process probe is unavailable.");
            return [];
        }
    }

    /// <summary>
    /// Performs infer vendor as part of the windows hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the windows hardware platform probe operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferVendor(string name)
    {
        try
        {
            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            if (name.Contains("Apple", StringComparison.OrdinalIgnoreCase)) return "Apple";
            return string.Empty;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsHardwarePlatformProbeService), nameof(InferVendor), exception);
            throw;
        }
    }
}

/// <summary>Unix implementation for Linux DRM/sysfs and macOS system_profiler GPU discovery.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixHardwarePlatformProbeService(ILogger<UnixHardwarePlatformProbeService> logger) : IHardwarePlatformProbeService
{
    /// <summary>
    /// Performs probe platform gpus as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbePlatformGpusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await ProbeLinuxDrmAsync(cancellationToken).ConfigureAwait(false);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return await ProbeMacDisplaysAsync(cancellationToken).ConfigureAwait(false);
            return [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixHardwarePlatformProbeService), nameof(ProbePlatformGpusAsync), exception);
            throw;
        }
    }

    /// <summary>
    /// Performs probe linux drm as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Linux DRM hardware discovery was unavailable.");
            return [];
        }
    }

    /// <summary>
    /// Performs probe mac displays as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeMacDisplaysAsync(CancellationToken cancellationToken)
    {
        try
        {
            var lines = await RunProbeAsync("/usr/sbin/system_profiler", "SPDisplaysDataType", cancellationToken).ConfigureAwait(false);
            var result = new List<OneWireHardwareDescriptor>();
            string? currentName = null;
            string currentVendor = string.Empty;
            long? currentMemory = null;

            void Flush()
            {
                if (string.IsNullOrWhiteSpace(currentName))
                    return;
                result.Add(new OneWireHardwareDescriptor
                {
                    Kind = OneWireHardwareKind.Gpu,
                    Index = result.Count,
                    Name = currentName.Trim(),
                    Vendor = string.IsNullOrWhiteSpace(currentVendor) ? InferVendor(currentName) : currentVendor,
                    DedicatedMemoryBytes = currentMemory,
                    IsOnline = true
                });
                currentName = null;
                currentVendor = string.Empty;
                currentMemory = null;
            }

            foreach (var rawLine in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = rawLine.Trim();
                if (line.StartsWith("Chipset Model:", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    currentName = line[(line.IndexOf(':') + 1)..].Trim();
                }
                else if (line.StartsWith("Vendor:", StringComparison.OrdinalIgnoreCase))
                {
                    currentVendor = line[(line.IndexOf(':') + 1)..].Trim();
                    var parenthesis = currentVendor.IndexOf('(');
                    if (parenthesis > 0)
                        currentVendor = currentVendor[..parenthesis].Trim();
                }
                else if (line.StartsWith("VRAM", StringComparison.OrdinalIgnoreCase))
                {
                    currentMemory = ParseMemoryBytes(line);
                }
            }
            Flush();
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixHardwarePlatformProbeService), nameof(ProbeMacDisplaysAsync), exception);
            throw;
        }
    }

    /// <summary>
    /// Parses memory bytes as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="line">Line value supplied to the unix hardware platform probe operation and used when producing its result.</param>
    /// <returns>The long produced by the operation.</returns>
    private long? ParseMemoryBytes(string line)
    {
        try
        {
            var colon = line.IndexOf(':');
            if (colon < 0) return null;
            var value = line[(colon + 1)..].Trim();
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                return null;
            var multiplier = parts[1].StartsWith("GB", StringComparison.OrdinalIgnoreCase)
                ? 1024d * 1024d * 1024d
                : parts[1].StartsWith("MB", StringComparison.OrdinalIgnoreCase)
                    ? 1024d * 1024d
                    : 1d;
            return (long)(amount * multiplier);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixHardwarePlatformProbeService), nameof(ParseMemoryBytes), exception);
            throw;
        }
    }

    /// <summary>
    /// Reads trimmed file as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the unix hardware platform probe operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
            logger.LogDebug(exception, "Optional Unix hardware field could not be read.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Performs run probe as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="fileName">File name value supplied to the unix hardware platform probe operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the unix hardware platform probe operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<string>> RunProbeAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
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
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or OperationCanceledException)
        {
            logger.LogDebug(exception, "Unix hardware process probe is unavailable.");
            return [];
        }
    }

    /// <summary>
    /// Performs infer vendor as part of the unix hardware platform probe service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the unix hardware platform probe operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferVendor(string name)
    {
        try
        {
            if (name.Contains("Apple", StringComparison.OrdinalIgnoreCase)) return "Apple";
            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            return string.Empty;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixHardwarePlatformProbeService), nameof(InferVendor), exception);
            throw;
        }
    }
}
