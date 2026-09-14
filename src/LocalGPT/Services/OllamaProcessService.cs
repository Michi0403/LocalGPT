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
/// <param name="httpClientFactory">Factory used for bounded local Ollama health probes without weakening transport validation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OllamaProcessService(
    IOllamaPlatformService platform,
    IOptionsMonitor<LocalGptConfigurationRoot> optionsRoot,
    IHttpClientFactory httpClientFactory,
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
            var status = BuildStatus();
            if (!status.IsRunning)
                return status;
            var responsive = await IsLocalRuntimeResponsiveAsync(cancellationToken).ConfigureAwait(false);
            return status with
            {
                IsResponsive = responsive,
                Message = responsive
                    ? $"Ollama is running and its local API is responsive in {status.Processes.Count} process(es)."
                    : $"Ollama has {status.Processes.Count} process(es), but the configured local API is not responding."
            };
    
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
            {
                if (await IsLocalRuntimeResponsiveAsync(cancellationToken).ConfigureAwait(false))
                    return current with
                    {
                        IsResponsive = true,
                        Message = $"Ollama is already running and responsive in {current.Processes.Count} process(es); no duplicate instance was started."
                    };

                logger.LogWarning("Ollama process state exists but the configured local API is unresponsive; explicit Start will recycle the stale local runtime before relaunching it.");
                await TerminateAllOllamaProcessesAsync(cancellationToken).ConfigureAwait(false);
                await WaitForProcessStateAsync(expectedRunning: false, cancellationToken).ConfigureAwait(false);
            }

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
            var responsive = await WaitForRuntimeAvailabilityAsync(cancellationToken).ConfigureAwait(false);
            var started = BuildStatus();
            return started with
            {
                IsResponsive = responsive,
                Message = !started.IsRunning
                    ? "Ollama was launched, but no Ollama process became visible before the startup timeout."
                    : responsive
                        ? $"Ollama started successfully and its local API is responsive with {started.Processes.Count} process(es)."
                        : $"Ollama started {started.Processes.Count} process(es), but the configured local API did not become responsive before the health timeout."
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
                var responsive = await WaitForRuntimeAvailabilityAsync(cancellationToken).ConfigureAwait(false);
                var restarted = BuildStatus();
                return restarted with
                {
                    IsResponsive = responsive,
                    Message = !restarted.IsRunning
                        ? "Ollama was relaunched, but no Ollama process became visible before the startup timeout."
                        : responsive
                            ? $"Ollama restarted successfully and its local API is responsive with {restarted.Processes.Count} process(es)."
                            : $"Ollama restarted {restarted.Processes.Count} process(es), but the configured local API did not become responsive before the health timeout."
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

    /// <summary>Checks whether the configured local Ollama API responds to its machine-readable model inventory endpoint.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the bounded health probe.</param>
    /// <returns><see langword="true"/> only when the local Ollama endpoint returns a successful HTTP response.</returns>
    private async Task<bool> IsLocalRuntimeResponsiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var runtimeUri = BuildLocalRuntimeClientUri();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(1500));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(runtimeUri, "/api/tags"));
            var client = httpClientFactory.CreateClient("LocalGPTProviderRuntime");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException exception)
        {
            logger.LogDebug("Local Ollama health probe could not connect: {FailureDetail}", exception.GetBaseException().Message);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Local Ollama health probe failed before a normal HTTP response could be processed.");
            return false;
        }
    }

    /// <summary>Waits for both the Ollama process and its configured local HTTP endpoint to become usable.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the bounded availability wait.</param>
    /// <returns><see langword="true"/> when the runtime becomes responsive before the timeout.</returns>
    private async Task<bool> WaitForRuntimeAvailabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsLocalRuntimeResponsiveAsync(cancellationToken).ConfigureAwait(false))
                    return true;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Waiting for the local Ollama HTTP endpoint failed.");
            return false;
        }
    }

    /// <summary>Builds the client-side loopback URI corresponding to LocalGPT's reviewed Ollama bind settings.</summary>
    /// <returns>The local Ollama base URI used only for bounded health checks.</returns>
    private Uri BuildLocalRuntimeClientUri()
    {
        try
        {
            var runtime = optionsRoot.CurrentValue.AICore?.OllamaRuntime ?? new OllamaRuntimeManagementOptions();
            var port = runtime.Port is > 0 and <= 65535 ? runtime.Port : 11434;
            var address = runtime.BindAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(address)
                || address is "0.0.0.0" or "::" or "[::]"
                || address.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                address = "127.0.0.1";
            if (address.Contains(":", StringComparison.Ordinal) && !address.StartsWith("[", StringComparison.Ordinal))
                address = $"[{address}]";
            return new Uri($"http://{address}:{port}/", UriKind.Absolute);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Building the local Ollama health URI failed; using the provider default loopback endpoint.");
            return new Uri("http://127.0.0.1:11434/", UriKind.Absolute);
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
