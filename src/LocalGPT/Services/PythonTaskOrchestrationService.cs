using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Single application-owned admission/monitoring boundary for Python work. Embedded Python.NET remains
/// serialized by its existing one-reader GIL coordinator. Approved Python scripts run only in *separate*
/// CPython processes; never call Python.NET from parallel managed workers.
/// </summary>
public sealed class PythonTaskOrchestrationService : IPythonNetRuntimeCoordinator, IPythonJobOrchestrationService, IDisposable
{
    /// <summary>
    /// Stores the python net runtime coordinator dependency used by <see cref="PythonTaskOrchestrationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PythonNetRuntimeCoordinator embedded;
    /// <summary>
    /// Stores the logger used by <see cref="PythonTaskOrchestrationService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<PythonTaskOrchestrationService> logger;
    /// <summary>
    /// Stores the internal gate state used by <see cref="PythonTaskOrchestrationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object gate = new();
    /// <summary>
    /// Holds bounded in-process Python job snapshots for UI and API inspection.
    /// </summary>
    private readonly Dictionary<Guid, JobState> jobs = new();
    /// <summary>
    /// Maps device visibility lanes to independent one-at-a-time process admission semaphores.
    /// </summary>
    private readonly Dictionary<string, SemaphoreSlim> deviceGates = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Signals pending subprocess admission requests when an active job releases its capacity slot.
    /// </summary>
    private readonly SemaphoreSlim processCapacityChanged = new(0);
    /// <summary>
    /// Stores the service scope factory dependency used by <see cref="PythonTaskOrchestrationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IServiceScopeFactory serviceScopes;
    /// <summary>
    /// Keeps the configured fallback limit when the persisted toolchain policy cannot be read.
    /// </summary>
    private readonly int defaultProcessCapacity;
    /// <summary>
    /// Counts currently reserved subprocess admission slots; embedded Python.NET work is excluded.
    /// </summary>
    private int activeProcessJobs;
    /// <summary>
    /// Limits the number of completed Python jobs retained in memory.
    /// </summary>
    private const int MaximumHistory = 256;

    /// <summary>
    /// Constructs the shared Python admission coordinator, binding the embedded runtime to bounded, policy-configured subprocess execution.
    /// </summary>
    /// <param name="embedded">Existing single-reader Python.NET coordinator that owns the embedded interpreter and GIL boundary.</param>
    /// <param name="options">Live runtime options that supply the bounded default maximum number of subprocesses.</param>
    /// <param name="serviceScopes">Service scope factory dependency used by the python task orchestration workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public PythonTaskOrchestrationService(PythonNetRuntimeCoordinator embedded, IOptionsMonitor<LocalGPT.BusinessObjects.ConfigurationRoot> options, IServiceScopeFactory serviceScopes, ILogger<PythonTaskOrchestrationService> logger)
    {
        this.embedded = embedded;
        this.logger = logger;
        this.serviceScopes = serviceScopes;
        defaultProcessCapacity = Math.Clamp(options.CurrentValue.PythonCore?.MaxParallelProcessJobs ?? 2, 1, 16);
    }

    /// <summary>
    /// Reports the initialization state of the single embedded Python.NET runtime.
    /// </summary>
    /// <value>The is initialized value exposed by <see cref="PythonTaskOrchestrationService"/>.</value>
    public bool IsInitialized => embedded.IsInitialized;
    /// <summary>
    /// Indicates that the embedded Python.NET runtime must restart before configuration changes take effect.
    /// </summary>
    /// <value>The restart required value exposed by <see cref="PythonTaskOrchestrationService"/>.</value>
    public bool RestartRequired => embedded.RestartRequired;
    /// <summary>
    /// Reports the number of requests waiting at the embedded Python.NET coordinator, not external subprocesses.
    /// </summary>
    /// <value>The queue length value exposed by <see cref="PythonTaskOrchestrationService"/>.</value>
    public int QueueLength => embedded.QueueLength;

    /// <summary>
    /// Returns copies of recent Python job metadata, newest first, without exposing plugin source or execution payloads.
    /// </summary>
    /// <param name="maximum">Maximum number of recent job snapshots to return; clamped to the coordinator retention limit.</param>
    /// <returns>A bounded list of detached snapshots ordered by creation time.</returns>
    public IReadOnlyList<PythonJobSnapshot> ListJobs(int maximum = 100)
    {
        try
        {
            lock (gate)
                return jobs.Values.OrderByDescending(item => item.Snapshot.CreatedAtUtc)
                    .Take(Math.Clamp(maximum, 1, 256)).Select(item => Copy(item.Snapshot)).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listing Python task state failed; no source or media contents were accessed.");
            throw;
        }
    }

    /// <summary>
    /// Requests cancellation of an active monitored Python job; completed and unknown jobs are left untouched.
    /// </summary>
    /// <param name="jobId">Identifier returned when the Python work was registered with the job coordinator.</param>
    /// <returns><see langword="true"/> if cancellation was requested for a running job; otherwise <see langword="false"/>.</returns>
    public bool CancelJob(Guid jobId)
    {
        try
        {
            JobState? state;
            lock (gate)
            {
                if (!jobs.TryGetValue(jobId, out state) || state.Snapshot.CompletedAtUtc is not null)
                    return false;
                state.Snapshot.CancellationRequested = true;
            }
            try { state.Cancellation.Cancel(); } catch (ObjectDisposedException) { return false; }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cancelling monitored Python task {JobId} failed.", jobId);
            throw;
        }
    }

    /// <summary>
    /// Runs a Python.NET request through the existing single-reader embedded interpreter and records its progress without adding parallel GIL callers.
    /// </summary>
    /// <param name="request">Embedded Python.NET request, dispatched only to the existing serialized interpreter worker.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded runtime result, including the monitoring job identifier.</returns>
    public async Task<LocalAiRuntimeJobResult> ExecuteAsync(LocalAiRuntimeJobRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var job = Register(request.Operation, "embedded-pythonnet", cancellationToken);
            try
            {
                Start(job);
                var result = await embedded.ExecuteAsync(request, job.Cancellation.Token).ConfigureAwait(false);
                Complete(job, result.Succeeded ? "Completed" : result.Status, result.Succeeded);
                // Return the monitoring identifier so the UI / calling AI can inspect the same job.
                result.JobId = job.Snapshot.JobId;
                return result;
            }
            catch (OperationCanceledException)
            {
                Complete(job, "Cancelled", false);
                throw;
            }
            catch
            {
                Complete(job, "Failed", false);
                throw;
            }
            finally { job.Cancellation.Dispose(); }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                logger.LogDebug(ex, "Embedded Python task cancelled.");
            else
                logger.LogError(ex, "Embedded Python task failed; request payload omitted.");
            throw;
        }
    }

    /// <summary>
    /// Runs an explicitly approved script in an independent CPython process, reserving the global process budget and one device lane.
    /// </summary>
    /// <param name="compiler">Enabled and previously validated Python toolchain profile specifying the CPython executable and worker environment.</param>
    /// <param name="scriptPath">Path to the approved, published Python plugin snapshot.</param>
    /// <param name="parameters">JSON input passed to the isolated plugin process through redirected standard input.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The process exit code and bounded standard output and error streams.</returns>
    public async Task<PythonPluginProcessResult> RunApprovedPythonPluginAsync(ProjectCompilerInstallation compiler, string scriptPath, JsonElement parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(compiler);
            if (!compiler.IsEnabled || !compiler.LastValidationSucceeded || !compiler.Language.Equals("Python", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Python runtime plugin requires a validated, enabled Python compiler profile.");
            if (!File.Exists(compiler.ExecutablePath) || !File.Exists(scriptPath))
                throw new FileNotFoundException("The validated Python runtime or approved script file is missing.");
            var lane = ResourceLane(compiler.EnvironmentVariablesJson);
            var job = Register("runtime-plugin.python", lane, cancellationToken);
            SemaphoreSlim device;
            lock (gate)
            {
                if (!deviceGates.TryGetValue(lane, out device!))
                    deviceGates[lane] = device = new SemaphoreSlim(1, 1);
            }
            var globalHeld = false;
            var deviceHeld = false;
            try
            {
                await ReserveProcessCapacityAsync(job.Cancellation.Token).ConfigureAwait(false);
                globalHeld = true;
                await device.WaitAsync(job.Cancellation.Token).ConfigureAwait(false);
                deviceHeld = true;
                Start(job);
                var result = await LaunchPythonProcessAsync(compiler, scriptPath, parameters, job.Cancellation.Token).ConfigureAwait(false);
                Complete(job, result.ExitCode == 0 ? "Completed" : "Failed", result.ExitCode == 0);
                return result;
            }
            catch (OperationCanceledException)
            {
                Complete(job, "Cancelled", false);
                throw;
            }
            catch
            {
                Complete(job, "Failed", false);
                throw;
            }
            finally
            {
                if (deviceHeld) device.Release();
                if (globalHeld) ReleaseProcessCapacity();
                job.Cancellation.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                logger.LogDebug(ex, "Approved Python plugin process cancelled.");
            else
                logger.LogError(ex, "Approved Python plugin process failed; source and parameters omitted.");
            throw;
        }
    }

    /// <summary>
    /// Starts a validated CPython executable in the approved script directory, exchanges bounded JSON and output, and terminates the process on cancellation.
    /// </summary>
    /// <param name="compiler">Validated Python executable, working directory environment and device visibility settings.</param>
    /// <param name="scriptPath">Approved Python script file whose containing directory becomes the subprocess working directory.</param>
    /// <param name="parameters">JSON document serialized to the subprocess standard input.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The python plugin process result produced by the operation.</returns>
    private async Task<PythonPluginProcessResult> LaunchPythonProcessAsync(ProjectCompilerInstallation compiler, string scriptPath, JsonElement parameters, CancellationToken cancellationToken)
    {
        try
        {
        var start = new ProcessStartInfo
        {
            FileName = compiler.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(scriptPath);
        if (!string.IsNullOrWhiteSpace(compiler.EnvironmentVariablesJson))
        {
            using var document = JsonDocument.Parse(compiler.EnvironmentVariablesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Python toolchain environment must be an object.");
            foreach (var variable in document.RootElement.EnumerateObject())
                start.Environment[variable.Name] = variable.Value.GetString() ?? string.Empty;
        }
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new InvalidOperationException("Approved Python process did not start.");
        // Begin draining BOTH redirected pipes before writing stdin. Python plugins may emit
        // diagnostics immediately; sequentially waiting for stdin first can deadlock on a full pipe.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            var output = ReadBoundedAsync(process.StandardOutput, 16_384, timeout.Token);
            var error = ReadBoundedAsync(process.StandardError, 4_096, timeout.Token);
            await process.StandardInput.WriteAsync(parameters.GetRawText().AsMemory(), timeout.Token).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(timeout.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return new PythonPluginProcessResult
            {
                ExitCode = process.ExitCode,
                Output = await output.ConfigureAwait(false),
                Error = await error.ConfigureAwait(false)
            };
        }
        catch
        {
            if (!process.HasExited)
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Launching approved Python plugin subprocess failed. ");
            throw;
        }
    }

    /// <summary>
    /// Continuously drains process output while retaining at most the configured number of characters to prevent pipe deadlocks and unbounded memory growth.
    /// </summary>
    /// <param name="input">Redirected process stream to drain fully even when the retained output limit is reached.</param>
    /// <param name="maximum">Maximum number of characters retained in the returned output string.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>Captured stream content limited to the specified maximum number of characters.</returns>
    private async Task<string> ReadBoundedAsync(StreamReader input, int maximum, CancellationToken cancellationToken)
    {
        try
        {
        var buffer = new char[2048];
        var result = new StringBuilder();
        while (true)
        {
            var count = await input.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (count == 0) break;
            if (result.Length < maximum)
                result.Append(buffer, 0, Math.Min(count, maximum - result.Length));
        }
        return result.ToString();
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Reading bounded subprocess output failed. ");
            throw;
        }
    }

    // The persisted toolchain execution profile owns capacity. A singleton coordinator cannot
    // directly depend on the scoped profile service, so a short-lived scope reads policy at admission.
    // Only external *processes* use this capacity: Python.NET remains one serial GIL worker.
    /// <summary>
    /// Reads the persisted Python subprocess concurrency limit through a short-lived DI scope, falling back to the bounded configured default.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted concurrency limit (between 1 and 16) or the configured fallback.</returns>
    private async Task<int> ReadProcessCapacityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopes.CreateScope();
            var profiles = scope.ServiceProvider.GetRequiredService<IToolchainExecutionProfileService>();
            var profile = await profiles.GetProfileAsync("localai.runtime-policy", cancellationToken).ConfigureAwait(false);
            if (profile is null || string.IsNullOrWhiteSpace(profile.ConfigurationJson)) return defaultProcessCapacity;
            var configured = JsonSerializer.Deserialize<LocalAiRuntimeConfigurationChangeRequest>(profile.ConfigurationJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return Math.Clamp(configured?.MaxParallelProcessJobs ?? defaultProcessCapacity, 1, 16);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Python process admission could not read persisted capacity. Using the configured bounded fallback.");
            return defaultProcessCapacity;
        }
    }

    /// <summary>
    /// Waits asynchronously until the configured global subprocess concurrency budget has a free slot, then atomically reserves it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes only after a process slot has been reserved.</returns>
    private async Task ReserveProcessCapacityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var capacity = await ReadProcessCapacityAsync(cancellationToken).ConfigureAwait(false);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (gate)
                {
                    if (activeProcessJobs < capacity)
                    {
                        activeProcessJobs++;
                        return;
                    }
                }
                await processCapacityChanged.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python process admission cancelled.");
            else logger.LogError(exception, "Python process admission failed.");
            throw;
        }
    }

    /// <summary>
    /// Releases a previously reserved subprocess slot and wakes one pending admission request.
    /// </summary>
    private void ReleaseProcessCapacity()
    {
        try
        {
            lock (gate)
                activeProcessJobs = Math.Max(0, activeProcessJobs - 1);
            processCapacityChanged.Release();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Releasing Python process capacity failed.");
            throw;
        }
    }

    /// <summary>
    /// Maps a worker environment to its explicitly selected GPU lane; unspecified device visibility shares one conservative lane.
    /// </summary>
    /// <param name="json">JSON environment override containing device visibility entries, if any.</param>
    /// <returns>The device-specific lane identifier, or a common conservative lane for unassigned processes.</returns>
    private string ResourceLane(string? json)
    {
        try
        {
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Python worker resource environment must be a JSON object.");
            foreach (var key in new[] { "ROCR_VISIBLE_DEVICES", "HIP_VISIBLE_DEVICES", "CUDA_VISIBLE_DEVICES" })
                if (document.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                    return key + ":" + value.GetString();
        }
        // No explicit device isolation: conservatively serialize all unassigned processes.
        return "unassigned-device";
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Resolving Python resource lane failed. ");
            throw;
        }
    }

    /// <summary>
    /// Creates the cancellable monitoring record for a Python operation and trims completed history to its bounded retention limit.
    /// </summary>
    /// <param name="operation">Non-sensitive operation identifier used for UI and diagnostics.</param>
    /// <param name="lane">Device resource lane reserved by an isolated subprocess or embedded runtime identifier.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The new linked cancellation source and scheduler-owned monitoring record.</returns>
    private JobState Register(string operation, string lane, CancellationToken cancellationToken)
    {
        try
        {
        var state = new JobState(new PythonJobSnapshot
        {
            JobId = Guid.NewGuid(), Operation = operation, Lane = lane, Status = "Queued", CreatedAtUtc = DateTime.UtcNow
        }, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
        lock (gate)
        {
            jobs[state.Snapshot.JobId] = state;
            foreach (var obsolete in jobs.Values.Where(item => item.Snapshot.CompletedAtUtc is not null)
                         .OrderByDescending(item => item.Snapshot.CreatedAtUtc).Skip(MaximumHistory).ToArray())
                jobs.Remove(obsolete.Snapshot.JobId);
        }
        return state;
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Registering Python job metadata failed. ");
            throw;
        }
    }

    /// <summary>
    /// Marks a monitored Python job as running and records its start timestamp under the coordinator lock.
    /// </summary>
    /// <param name="state">Coordinator-owned job record to transition from queued to running.</param>
    private void Start(JobState state)
    {
        try
        {
        lock (gate)
        {
            state.Snapshot.Status = "Running";
            state.Snapshot.StartedAtUtc = DateTime.UtcNow;
        }
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Starting Python job metadata failed. ");
            throw;
        }
    }

    /// <summary>
    /// Records the terminal job outcome and timestamp without logging arguments, output or script contents.
    /// </summary>
    /// <param name="state">Coordinator-owned job record to transition to its terminal state.</param>
    /// <param name="status">Terminal status reflecting completion, cancellation or failure.</param>
    /// <param name="succeeded">Whether the Python work completed successfully.</param>
    private void Complete(JobState state, string status, bool succeeded)
    {
        try
        {
        lock (gate)
        {
            state.Snapshot.Status = status;
            state.Snapshot.CompletedAtUtc = DateTime.UtcNow;
            state.Snapshot.Succeeded = succeeded;
        }
        logger.LogInformation("Python task {JobId} finished with {Status} on {Lane}; no payload recorded.", state.Snapshot.JobId, status, state.Snapshot.Lane);
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Completing Python job metadata failed. ");
            throw;
        }
    }

    /// <summary>
    /// Builds a detached Python job snapshot so callers cannot mutate scheduler state through returned references.
    /// </summary>
    /// <param name="source">Current coordinator-owned snapshot whose values are copied into a detached response.</param>
    /// <returns>A value snapshot independent of the coordinator-owned mutable instance.</returns>
    private PythonJobSnapshot Copy(PythonJobSnapshot source)
    {
        try
        {
            return new PythonJobSnapshot
            {
                JobId = source.JobId, Operation = source.Operation, Lane = source.Lane, Status = source.Status,
                CreatedAtUtc = source.CreatedAtUtc, StartedAtUtc = source.StartedAtUtc, CompletedAtUtc = source.CompletedAtUtc,
                CancellationRequested = source.CancellationRequested, Succeeded = source.Succeeded
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying bounded Python job metadata failed.");
            throw;
        }
    }

    /// <summary>
    /// Cancels outstanding monitored work and disposes owned admission and per-device synchronization primitives.
    /// </summary>
    public void Dispose()
    {
        try
        {
        lock (gate)
            foreach (var state in jobs.Values.Where(item => item.Snapshot.CompletedAtUtc is null))
                state.Cancellation.Cancel();
        processCapacityChanged.Dispose();
        foreach (var entry in deviceGates.Values) entry.Dispose();
    
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Python task operation cancelled.");
            else logger.LogError(exception, "Disposing Python task supervisor failed. ");
            throw;
        }
    }

    /// <summary>
    /// Associates a mutable scheduler-owned job snapshot with the cancellation source for that operation.
    /// </summary>
    /// <param name="Snapshot">Mutable monitoring state protected by the scheduler lock.</param>
    /// <param name="Cancellation">Linked cancellation source used to interrupt this job.</param>
    private sealed record JobState(PythonJobSnapshot Snapshot, CancellationTokenSource Cancellation);
}
