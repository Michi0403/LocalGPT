using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates Ollama process behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="platform">Resolves the operating-system-specific Ollama executable without leaking platform path policy into the shared process coordinator.</param>
/// <param name="optionsRoot">Current LocalGPT configuration containing reviewed local Ollama launch defaults.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OllamaProcessService(
    IOllamaPlatformService platform,
    IOptionsMonitor<LocalGptConfigurationRoot> optionsRoot,
    ILogger<OllamaProcessService> logger) : IOllamaProcessService
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to process gate state owned by <see cref="OllamaProcessService"/>.
    /// </summary>
    private readonly SemaphoreSlim processGate = new(1, 1);
    /// <summary>
    /// Stores the internal Ollama process names state used by <see cref="OllamaProcessService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string[] ollamaProcessNames = ["ollama", "ollamaapp"];

    /// <summary>
    /// Retrieves status as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    public async Task<OllamaProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.FromResult(BuildStatus()).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(GetStatusAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(GetStatusAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs start as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    public async Task<OllamaProcessStatus> StartAsync(CancellationToken cancellationToken = default)
    {
        await processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = BuildStatus();
            if (current.IsRunning)
                return current with { Message = $"Ollama is already running in {current.Processes.Count} process(es); no duplicate instance was started." };

            var executable = platform.ResolveExecutable();
            if (string.IsNullOrWhiteSpace(executable))
                return current with { Message = "Ollama is not installed or its executable could not be resolved." };

            var isGuiExecutable = platform.IsGuiExecutable(executable);
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory(),
                UseShellExecute = isGuiExecutable,
                CreateNoWindow = !isGuiExecutable
            };
            if (!isGuiExecutable)
            {
                startInfo.ArgumentList.Add("serve");
                ApplyLocalRuntimeEnvironment(startInfo);
            }

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
            processGate.Release();
        }
    }

    /// <summary>
    /// Performs stop as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    public async Task<OllamaProcessStatus> StopAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                            : $"Stopped {terminatedCount} Ollama process(es)."
                };
            }
            finally
            {
                processGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(StopAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(StopAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs restart as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    public async Task<OllamaProcessStatus> RestartAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await TerminateAllOllamaProcessesAsync(cancellationToken).ConfigureAwait(false);

                var executable = platform.ResolveExecutable();
                if (string.IsNullOrWhiteSpace(executable))
                    return BuildStatus() with { Message = "Ollama was stopped, but its executable could not be resolved for restart." };

                var isGuiExecutable = platform.IsGuiExecutable(executable);
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = Path.GetDirectoryName(executable) ?? LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory(),
                    UseShellExecute = isGuiExecutable,
                    CreateNoWindow = !isGuiExecutable
                };
                if (!isGuiExecutable)
                {
                    startInfo.ArgumentList.Add("serve");
                    ApplyLocalRuntimeEnvironment(startInfo);
                }

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
                processGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(RestartAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(RestartAsync)} failed.");
        throw;
    }
}

    /// <summary>Applies reviewed LocalGPT Ollama runtime settings only to a LocalGPT-owned CLI launch.</summary>
    /// <param name="startInfo">Process start information for the resolved Ollama CLI.</param>
    private void ApplyLocalRuntimeEnvironment(ProcessStartInfo startInfo)
    {
    try
    {
                var runtime = optionsRoot.CurrentValue.AICore?.OllamaRuntime ?? new OllamaRuntimeManagementOptions();
                if (!string.IsNullOrWhiteSpace(runtime.ModelDirectory))
                    startInfo.Environment["OLLAMA_MODELS"] = NormalizeConfiguredModelDirectory(runtime.ModelDirectory);
                if (!string.IsNullOrWhiteSpace(runtime.BindAddress) && runtime.Port is > 0 and <= 65535)
                    startInfo.Environment["OLLAMA_HOST"] = $"{runtime.BindAddress.Trim()}:{runtime.Port}";
                if (runtime.ContextLengthTokens > 0)
                    startInfo.Environment["OLLAMA_CONTEXT_LENGTH"] = runtime.ContextLengthTokens.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(runtime.KeepAlive))
                    startInfo.Environment["OLLAMA_KEEP_ALIVE"] = runtime.KeepAlive.Trim();
                if (runtime.MaxLoadedModels > 0)
                    startInfo.Environment["OLLAMA_MAX_LOADED_MODELS"] = runtime.MaxLoadedModels.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (runtime.ParallelRequests > 0)
                    startInfo.Environment["OLLAMA_NUM_PARALLEL"] = runtime.ParallelRequests.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (runtime.MaxQueue > 0)
                    startInfo.Environment["OLLAMA_MAX_QUEUE"] = runtime.MaxQueue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (runtime.DisableCloud)
                    startInfo.Environment["OLLAMA_NO_CLOUD"] = "1";
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper OllamaProcessService.ApplyLocalRuntimeEnvironment failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>Expands environment variables and a leading user-home marker for a persisted Ollama model directory without invoking a shell.</summary>
    /// <param name="value">Persisted model directory.</param>
    /// <returns>The absolute model-store path.</returns>
    private string NormalizeConfiguredModelDirectory(string value)
    {
    try
    {
                var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
                if (expanded.Equals("~", StringComparison.Ordinal) || expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal))
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (!string.IsNullOrWhiteSpace(home))
                        expanded = expanded.Length == 1 ? home : Path.Combine(home, expanded[2..]);
                }
                return Path.GetFullPath(expanded);
    }
    catch (Exception exception)
    {
        logger.LogDebug(exception, "Provider/runtime helper OllamaProcessService.NormalizeConfiguredModelDirectory failed; caller-controlled path/model values were omitted.");
        throw;
    }
}

    /// <summary>
    /// Builds status as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The Ollama process status produced by the operation.</returns>
    private OllamaProcessStatus BuildStatus()
    {
    try
    {
            var executable = platform.ResolveExecutable();
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(BuildStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(BuildStatus)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves Ollama processes as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<Process> GetOllamaProcesses()
    {
    try
    {
            var matches = new List<Process>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (ollamaProcessNames.Contains(NormalizeProcessName(process.ProcessName), StringComparer.Ordinal))
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(GetOllamaProcesses)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(GetOllamaProcesses)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes process name as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the Ollama process operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeProcessName(string value) {
    try
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(NormalizeProcessName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(NormalizeProcessName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs terminate all Ollama processes as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The int produced by the operation.</returns>
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

    /// <summary>
    /// Performs wait for process state as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expectedRunning">Value indicating whether expected running should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task WaitForProcessStateAsync(bool expectedRunning, CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(WaitForProcessStateAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaProcessService)}.{nameof(WaitForProcessStateAsync)} failed.");
        throw;
    }
}
}
