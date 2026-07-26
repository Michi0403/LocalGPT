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
public sealed class HardwareInventoryService(ILogger<HardwareInventoryService> logger) : IHardwareInventoryService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<OneWireHardwareDescriptor>? cached;
    private DateTimeOffset cacheUtc;

    public async Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default)
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

    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeNvidiaAsync(CancellationToken cancellationToken)
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

    private async Task<IReadOnlyList<OneWireHardwareDescriptor>> ProbeWindowsVideoControllersAsync(CancellationToken cancellationToken)
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

    private static string InferVendor(string name)
    {
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
        return string.Empty;
    }

    private static OneWireHardwareDescriptor Clone(OneWireHardwareDescriptor item) => new()
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
