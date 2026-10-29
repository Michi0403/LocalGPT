using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Runs explicitly approved snippets in isolated provider processes, independently of Blazor circuits.
/// Each invocation is a separate disposable micro-VM, not a host-side Python/.NET process.
/// </summary>
public sealed class SessionSandboxExecutionService : ISessionSandboxExecutionService, IHostedService, IDisposable
{
    private const int MaximumConcurrentRuns = 3;
    private const int MaximumSourceCharacters = 160_000;
    private const int MaximumCapturedCharactersPerStream = 65_536;
    private const int ExecutionSeconds = 120;
    private const int MaximumSavedRuns = 200;

    private readonly ILogger<SessionSandboxExecutionService> logger;
    private readonly Dictionary<string, ISessionSandboxExecutionProvider> providers;
    private readonly ConcurrentDictionary<Guid, RunState> runs = new();
    private readonly CancellationTokenSource shutdown = new();
    private readonly string evidenceDirectory;
    private readonly string temporaryDirectory;
    private readonly object stateLock = new();
    private bool disposed;

    private sealed class RunState(SessionSandboxRunSnapshot initial)
    {
        public readonly object Sync = new();
        public SessionSandboxRunSnapshot Snapshot = initial;
        public readonly CancellationTokenSource Cancellation = new();
        public Task ExecutionTask = Task.CompletedTask;
    }

    /// <summary>Initializes the backend and reloads bounded persistent evidence.</summary>
    public SessionSandboxExecutionService(
        IEnumerable<ISessionSandboxExecutionProvider> availableProviders,
        ILogger<SessionSandboxExecutionService> logger)
    {
        this.logger = logger;
        providers = availableProviders.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var bundleRoot = Path.Combine(AppContext.BaseDirectory, "hyperlight");
        var cli = Path.Combine(bundleRoot, "bin", OperatingSystem.IsWindows() ? "hluk.exe" : "hluk");
        var manifest = Path.Combine(bundleRoot, "runtimes", "manifest.json");
        RuntimeReadiness = providers.Values.SelectMany(provider => provider.Languages)
            .Select(language =>
            {
                var image = Path.Combine(bundleRoot, "runtimes", language.Runtime + ".cpio");
                var ready = File.Exists(cli) && File.Exists(manifest) && File.Exists(image);
                return new SessionSandboxRuntimeReadiness(language.DisplayName, language.Runtime, ready,
                    ready ? "Provisioned; SHA-256 checked again before execution." :
                        "Not provisioned on this host. Build/publish using the PowerShell guest provisioner.");
            }).ToArray();
        evidenceDirectory = LocalGptApplicationDataPaths.ResolveUserPath("sandbox-sessions", "runs");
        temporaryDirectory = LocalGptApplicationDataPaths.ResolveUserPath("sandbox-sessions", "workspace");
        LoadEvidence();
    }

    /// <summary>Notifies connected observers without requiring them for execution.</summary>
    public event Action? Changed;

    /// <summary>Returns registered runtime capabilities.</summary>
    public IReadOnlyList<SessionSandboxLanguage> Languages => providers.Values
        .SelectMany(provider => provider.Languages)
        .OrderBy(language => language.DisplayName, StringComparer.Ordinal)
        .ToArray();

    /// <summary>Returns the guest provisioning state prepared at service startup without UI-side filesystem access.</summary>
    public IReadOnlyList<SessionSandboxRuntimeReadiness> RuntimeReadiness { get; }

    /// <summary>Retrieves runs optionally filtered by their owning session.</summary>
    public IReadOnlyList<SessionSandboxRunSnapshot> ListRuns(Guid? sessionId = null) => runs.Values
        .Select(state => Read(state))
        .Where(item => sessionId is null || item.SessionId == sessionId.Value)
        .OrderByDescending(item => item.StartedUtc)
        .Take(MaximumSavedRuns)
        .ToArray();

    /// <summary>Queues an explicitly submitted isolated guest run.</summary>
    public SessionSandboxRunSnapshot Start(SessionSandboxRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required to launch an isolated execution.");
        if (request.SessionId == Guid.Empty)
            throw new ArgumentException("An owning Chat or Council session is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Source))
            throw new ArgumentException("Enter Python or C# source before requesting execution.", nameof(request));
        if (request.Source.Length > MaximumSourceCharacters)
            throw new ArgumentException("The submitted source exceeds the isolated execution size limit.", nameof(request));
        var language = Languages.FirstOrDefault(item => item.Key == request.LanguageKey)
            ?? throw new InvalidOperationException("No registered sandbox provider supports the selected language.");
        var provider = providers[language.ProviderId];

        SessionSandboxRunSnapshot snapshot;
        RunState state;
        lock (stateLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (shutdown.IsCancellationRequested) throw new InvalidOperationException("The sandbox execution host is shutting down.");
            if (runs.Values.Count(item => !IsTerminal(Read(item).Status)) >= MaximumConcurrentRuns)
                throw new InvalidOperationException("All isolated execution slots are occupied. Stop or finish a running sandbox first.");
            snapshot = new SessionSandboxRunSnapshot
            {
                RunId = Guid.NewGuid(), SessionId = request.SessionId,
                ConversationId = request.ConversationId, ProjectId = request.ProjectId,
                ProviderId = provider.Id, LanguageKey = language.Key, Runtime = language.Runtime,
                StartedUtc = DateTimeOffset.UtcNow, Status = "Queued",
                SourceKind = request.SourceKind == "CodeDOM-derived" ? "CodeDOM-derived" : "Manual",
                SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Source)))
            };
            state = new RunState(snapshot);
            runs[snapshot.RunId] = state;
        }

        Persist(snapshot);
        SignalChanged();
        // The execution lifetime belongs to this singleton; never await it in a UI event handler.
        state.ExecutionTask = Task.Run(() => ExecuteAsync(state, provider, language, request.Source));
        logger.LogInformation("Queued isolated {Runtime} sandbox {SandboxRunId} for session {SessionId}.", language.Runtime, snapshot.RunId, snapshot.SessionId);
        return snapshot;
    }

    /// <summary>Requests cancellation and termination of a guest run.</summary>
    public bool Stop(Guid runId)
    {
        if (!runs.TryGetValue(runId, out var state) || IsTerminal(Read(state).Status))
            return false;
        state.Cancellation.Cancel();
        logger.LogInformation("Requested stop of isolated sandbox {SandboxRunId}.", runId);
        return true;
    }

    /// <summary>Starts the process-wide hosted service.</summary>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Terminates guest processes before host shutdown.</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        shutdown.Cancel();
        foreach (var state in runs.Values)
            if (!IsTerminal(Read(state).Status)) state.Cancellation.Cancel();
        // Wait for guest processes to terminate; do not treat navigation or a closed browser as cancellation.
        var executing = runs.Values.Select(state => state.ExecutionTask).ToArray();
        try { await Task.WhenAll(executing).WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { logger.LogWarning("Host shutdown stopped awaiting guest cleanup; process cancellation was requested."); }

    }

    private async Task ExecuteAsync(RunState state, ISessionSandboxExecutionProvider provider,
        SessionSandboxLanguage language, string source)
    {
        var runId = Read(state).RunId;
        var workspace = Path.Combine(temporaryDirectory, runId.ToString("N"));
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(ExecutionSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            budget.Token, state.Cancellation.Token, shutdown.Token);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(workspace);
            var sourceFile = Path.Combine(workspace, "submitted" + language.SourceExtension);
            await File.WriteAllTextAsync(sourceFile, source, linked.Token).ConfigureAwait(false);
            var start = provider.PrepareLaunch(language, sourceFile, workspace);
            using var process = new Process { StartInfo = start };
            if (!process.Start())
                throw new InvalidOperationException("The isolated execution provider could not start.");

            Set(state, item => item with { Status = "Running" });
            var stdout = CaptureStreamAsync(process.StandardOutput, state, isError: false);
            var stderr = CaptureStreamAsync(process.StandardError, state, isError: true);
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Do not leave an untrusted guest running after cancellation or deadline.
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "Could not terminate the complete sandbox process tree for {RunId}; attempting direct kill.", runId);
                        try { process.Kill(); }
                        catch (Exception directKillFailure)
                        {
                            logger.LogError(directKillFailure, "Could not stop sandbox provider process {RunId}.", runId);
                            throw new InvalidOperationException("The sandbox provider process could not be terminated safely.", directKillFailure);
                        }
                    }
                }
                using var stopGracePeriod = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await process.WaitForExitAsync(stopGracePeriod.Token).ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    throw new InvalidOperationException("The sandbox provider did not terminate within the shutdown grace period.");
                }
                throw;
            }
            finally
            {
                // Draining must not prevent the host shutdown from completing if the provider is stuck.
                try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); }
                catch (TimeoutException) { logger.LogWarning("Sandbox output streams did not close within the bounded grace period for {RunId}.", runId); }
            }
            Set(state, item => item with
            {
                Status = process.ExitCode == 0 ? "Completed" : "Failed",
                ExitCode = process.ExitCode,
                CompletedUtc = DateTimeOffset.UtcNow,
                Diagnostic = process.ExitCode == 0 ? "Guest completed successfully." :
                    "Isolated runtime returned a nonzero exit code; inspect the captured stderr."
            });
        }
        catch (OperationCanceledException)
        {
            Set(state, item => item with
            {
                Status = budget.IsCancellationRequested ? "Timed out" : "Stopped",
                CompletedUtc = DateTimeOffset.UtcNow,
                Diagnostic = budget.IsCancellationRequested
                    ? "Execution exceeded its deadline; the sandbox process tree was terminated."
                    : "Execution was stopped; the sandbox process tree was terminated."
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Isolated sandbox {SandboxRunId} could not complete.", runId);
            Set(state, item => item with
            {
                Status = "Failed", CompletedUtc = DateTimeOffset.UtcNow,
                Diagnostic = exception is System.ComponentModel.Win32Exception
                    ? "The Hyperlight Unikraft 'hluk' executable was not found or could not launch. Install and configure it; host execution was not attempted."
                    : $"Isolated execution provider failed: {exception.Message}"
            });
        }
        finally
        {
            try { if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true); }
            catch (Exception exception) { logger.LogWarning(exception, "Unable to clean isolated run workspace {SandboxRunId}.", runId); }
        }
    }

    private async Task CaptureStreamAsync(StreamReader reader, RunState state, bool isError)
    {
        var buffer = new char[2048];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) > 0)
        {
            var fragment = new string(buffer, 0, count);
            lock (state.Sync)
            {
                var snapshot = state.Snapshot;
                var existing = isError ? snapshot.StandardError : snapshot.StandardOutput;
                var room = Math.Max(0, MaximumCapturedCharactersPerStream - existing.Length);
                var next = room > 0 ? existing + fragment[..Math.Min(room, fragment.Length)] : existing;
                state.Snapshot = isError
                    ? snapshot with { StandardError = next, OutputTruncated = snapshot.OutputTruncated || fragment.Length > room }
                    : snapshot with { StandardOutput = next, OutputTruncated = snapshot.OutputTruncated || fragment.Length > room };
            }
            SignalChanged();
        }
    }

    private SessionSandboxRunSnapshot Read(RunState state)
    {
        lock (state.Sync) return state.Snapshot;
    }

    private void Set(RunState state, Func<SessionSandboxRunSnapshot, SessionSandboxRunSnapshot> update)
    {
        SessionSandboxRunSnapshot snapshot;
        lock (state.Sync) snapshot = state.Snapshot = update(state.Snapshot);
        Persist(snapshot);
        if (IsTerminal(snapshot.Status)) PruneCompletedHistory();
        SignalChanged();
    }

    private void PruneCompletedHistory()
    {
        // Bound both in-memory receipts and per-user evidence; never evict a running VM.
        var expired = runs.Values.Select(Read)
            .Where(item => IsTerminal(item.Status))
            .OrderByDescending(item => item.StartedUtc)
            .Skip(MaximumSavedRuns)
            .ToArray();
        foreach (var item in expired)
        {
            if (!runs.TryRemove(item.RunId, out _)) continue;
            try
            {
                File.Delete(Path.Combine(evidenceDirectory, item.RunId.ToString("N") + ".json"));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to remove expired sandbox evidence {RunId}.", item.RunId);
            }
        }
    }

    private void LoadEvidence()
    {
        try
        {
            if (!Directory.Exists(evidenceDirectory)) return;
            foreach (var filename in Directory.EnumerateFiles(evidenceDirectory, "*.json")
                         .OrderByDescending(File.GetLastWriteTimeUtc).Take(MaximumSavedRuns))
            {
                try
                {
                    var item = JsonSerializer.Deserialize<SessionSandboxRunSnapshot>(File.ReadAllText(filename));
                    if (item is null || item.RunId == Guid.Empty || item.SessionId == Guid.Empty) continue;
                    if (!IsTerminal(item.Status))
                        item = item with
                        {
                            Status = "Interrupted", CompletedUtc = DateTimeOffset.UtcNow,
                            Diagnostic = "LocalGPT exited before the run finished. Sandbox processes are not resumed after restart."
                        };
                    if (item.Status == "Interrupted") Persist(item);
                    runs[item.RunId] = new RunState(item);
                }
                catch (Exception exception) { logger.LogWarning(exception, "Ignoring invalid sandbox evidence file {Filename}.", filename); }
            }
        }
        catch (Exception exception) { logger.LogWarning(exception, "Sandbox evidence could not be loaded."); }
    }

    private void Persist(SessionSandboxRunSnapshot snapshot)
    {
        try
        {
            Directory.CreateDirectory(evidenceDirectory);
            var filename = Path.Combine(evidenceDirectory, snapshot.RunId.ToString("N") + ".json");
            var temporary = filename + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot));
            File.Move(temporary, filename, overwrite: true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Sandbox {SandboxRunId} evidence could not be persisted.", snapshot.RunId);
        }
    }

    private bool IsTerminal(string status) => status is "Completed" or "Failed" or "Stopped" or "Timed out" or "Interrupted";

    private void SignalChanged()
    {
        try { Changed?.Invoke(); }
        catch (Exception exception) { logger.LogDebug(exception, "A sandbox UI subscriber disconnected while the backend kept running."); }
    }

    /// <summary>Requests cancellation when the host disposes the service.</summary>
    public void Dispose()
    {
        lock (stateLock) disposed = true;
        shutdown.Cancel();
        foreach (var state in runs.Values) state.Cancellation.Cancel();
        // Do not dispose cancellation sources while detached executions may still be unwinding.
    }
}
