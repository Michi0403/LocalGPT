using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LocalGPT.Services;

public sealed class OllamaProcessService(
    ILogger<OllamaProcessService> logger) : IOllamaProcessService
{
    private static readonly SemaphoreSlim ProcessGate = new(1, 1);
    private static readonly string[] OllamaProcessNames = ["ollama", "ollamaapp"];

    public async Task<OllamaProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(BuildStatus()).ConfigureAwait(false);
    }

    public async Task<OllamaProcessStatus> StartAsync(CancellationToken cancellationToken = default)
    {
        await ProcessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = BuildStatus();
            if (current.IsRunning)
                return current with { Message = $"Ollama is already running in {current.Processes.Count} process(es); no duplicate instance was started." };

            var executable = ResolveOllamaExecutable();
            if (string.IsNullOrWhiteSpace(executable))
                return current with { Message = "Ollama is not installed or its executable could not be resolved." };

            var isGuiExecutable = IsOllamaAppExecutable(executable);
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = isGuiExecutable,
                CreateNoWindow = !isGuiExecutable
            };
            if (!isGuiExecutable)
                startInfo.ArgumentList.Add("serve");

            Process.Start(startInfo)?.Dispose();
            logger.LogInformation("Started Ollama through the resolved local executable; executable path was omitted from logs.");
            await WaitForProcessStateAsync(expectedRunning: true, cancellationToken).ConfigureAwait(false);
            var started = BuildStatus();
            return started with
            {
                Message = started.IsRunning
                    ? $"Ollama started successfully with {started.Processes.Count} process(es)."
                    : "Ollama was launched, but no Ollama process became visible before the startup timeout."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not start Ollama; executable path was omitted from logs.");
            var status = BuildStatus();
            return status with { Message = $"Ollama could not be started: {ex.Message}" };
        }
        finally
        {
            ProcessGate.Release();
        }
    }

    public async Task<OllamaProcessStatus> StopAsync(CancellationToken cancellationToken = default)
    {
        await ProcessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var terminatedCount = await TerminateAllOllamaProcessesAsync(cancellationToken).ConfigureAwait(false);
            var stopped = BuildStatus();
            return stopped with
            {
                Message = stopped.IsRunning
                    ? $"Ollama stop was requested, but {stopped.Processes.Count} process(es) are still running."
                    : terminatedCount == 0
                        ? "Ollama was not running."
                        : $"Stopped {terminatedCount} Ollama process(es), including ollama.exe and Ollama app processes."
            };
        }
        finally
        {
            ProcessGate.Release();
        }
    }

    public async Task<OllamaProcessStatus> RestartAsync(CancellationToken cancellationToken = default)
    {
        await ProcessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await TerminateAllOllamaProcessesAsync(cancellationToken).ConfigureAwait(false);

            var executable = ResolveOllamaExecutable();
            if (string.IsNullOrWhiteSpace(executable))
                return BuildStatus() with { Message = "Ollama was stopped, but its executable could not be resolved for restart." };

            var isGuiExecutable = IsOllamaAppExecutable(executable);
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = isGuiExecutable,
                CreateNoWindow = !isGuiExecutable
            };
            if (!isGuiExecutable)
                startInfo.ArgumentList.Add("serve");

            Process.Start(startInfo)?.Dispose();
            await WaitForProcessStateAsync(expectedRunning: true, cancellationToken).ConfigureAwait(false);
            var restarted = BuildStatus();
            return restarted with
            {
                Message = restarted.IsRunning
                    ? $"Ollama restarted successfully with {restarted.Processes.Count} process(es)."
                    : "Ollama was relaunched, but no Ollama process became visible before the startup timeout."
            };
        }
        finally
        {
            ProcessGate.Release();
        }
    }

    private OllamaProcessStatus BuildStatus()
    {
        var executable = ResolveOllamaExecutable();
        var processes = GetOllamaProcesses()
            .Select(process =>
            {
                using (process)
                {
                    string? path = null;
                    try { path = process.MainModule?.FileName; }
                    catch { }
                    return new OllamaProcessInfo(process.Id, process.ProcessName, path);
                }
            })
            .OrderBy(process => process.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.ProcessId)
            .ToList();

        var processSummary = string.Join(
            ", ",
            processes.Select(process => $"{process.ProcessName} ({process.ProcessId})"));

        return new OllamaProcessStatus(
            !string.IsNullOrWhiteSpace(executable),
            processes.Count > 0,
            executable,
            processes,
            processSummary,
            processes.Count > 0
                ? $"Ollama is running in {processes.Count} process(es)."
                : !string.IsNullOrWhiteSpace(executable)
                    ? "Ollama is installed but not running."
                    : "Ollama is not installed or could not be found.");
    }

    private static List<Process> GetOllamaProcesses()
    {
        var matches = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (OllamaProcessNames.Contains(NormalizeProcessName(process.ProcessName), StringComparer.Ordinal))
                    matches.Add(process);
                else
                    process.Dispose();
            }
            catch
            {
                process.Dispose();
            }
        }
        return matches;
    }

    private static string NormalizeProcessName(string value) =>
        new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool IsOllamaAppExecutable(string executable) =>
        NormalizeProcessName(Path.GetFileNameWithoutExtension(executable)) == "ollamaapp";

    private static string? ResolveOllamaExecutable()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama";
        var candidates = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", executableName));
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama app.exe"));
                candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", "ollama.app.exe"));
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                candidates.Add(Path.Combine(programFiles, "Ollama", executableName));
                candidates.Add(Path.Combine(programFiles, "Ollama", "ollama app.exe"));
                candidates.Add(Path.Combine(programFiles, "Ollama", "ollama.app.exe"));
            }
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        candidates.AddRange(path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, executableName)));

        return candidates.FirstOrDefault(File.Exists);
    }

    private async Task<int> TerminateAllOllamaProcessesAsync(CancellationToken cancellationToken)
    {
        var terminatedProcessIds = new HashSet<int>();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var processes = GetOllamaProcesses();
            if (processes.Count == 0)
                return terminatedProcessIds.Count;

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        var processId = process.Id;
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                        terminatedProcessIds.Add(processId);
                    }
                    catch (InvalidOperationException)
                    {
                        // The process exited between discovery and termination.
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not terminate one Ollama process; process identifiers and paths were omitted.");
                    }
                }
            }

            await Task.Delay(175, cancellationToken).ConfigureAwait(false);
        }
        while (DateTime.UtcNow < deadline);

        return terminatedProcessIds.Count;
    }

    private static async Task WaitForProcessStateAsync(bool expectedRunning, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(expectedRunning ? 10 : 5);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var processes = GetOllamaProcesses();
            var isRunning = processes.Count > 0;
            foreach (var process in processes)
                process.Dispose();
            if (isRunning == expectedRunning)
                return;
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }
}
