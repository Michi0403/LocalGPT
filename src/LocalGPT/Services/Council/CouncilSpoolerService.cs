using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LocalGPT.Services.Council;

/// <summary>
/// Process-lifetime Council spooler. Browser circuits can disconnect and later rejoin the same run.
/// A bounded checkpoint file preserves recent run context across application restarts, while active
/// model calls themselves intentionally remain process-owned and cannot be resurrected after a crash.
/// </summary>
public sealed class CouncilSpoolerService : ICouncilSpoolerService, IDisposable
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="CouncilSpoolerService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    /// <summary>
    /// Stores the in-memory runs collection maintained internally by <see cref="CouncilSpoolerService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CouncilSpoolerSnapshot> runs = new();
    /// <summary>
    /// Stores the internal mutation gate state used by <see cref="CouncilSpoolerService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object mutationGate = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to persistence gate state owned by <see cref="CouncilSpoolerService"/>.
    /// </summary>
    private readonly SemaphoreSlim persistenceGate = new(1, 1);
    /// <summary>
    /// Stores the logger used by <see cref="CouncilSpoolerService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<CouncilSpoolerService> logger;
    /// <summary>
    /// Stores the LocalGPT vocabulary service dependency used by <see cref="CouncilSpoolerService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ILocalGptVocabularyService vocabulary;
    /// <summary>
    /// Stores the cancellation source used by <see cref="CouncilSpoolerService"/> to stop its current background or asynchronous operation.
    /// </summary>
    private CancellationTokenSource? pendingPersist;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="CouncilSpoolerService"/> while executing its surrounding workflow.
    /// </summary>
    private bool disposed;
    /// <summary>
    /// Gets the checkpoint path used by this council spooler instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The checkpoint path value exposed by <see cref="CouncilSpoolerService"/>.</value>
    private string CheckpointPath { get; } = LocalGptApplicationDataPaths.ResolveUserPath("CouncilSpooler", "recent-runs.json");

    /// <summary>
    /// Initializes a new <see cref="CouncilSpoolerService"/> instance and captures the dependencies or initial state required by its council spooler workflow.
    /// </summary>
    /// <param name="vocabulary">Local gpt vocabulary service dependency used by the council spooler workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public CouncilSpoolerService(ILocalGptVocabularyService vocabulary, ILogger<CouncilSpoolerService> logger)
    {
        this.vocabulary = vocabulary;
        this.logger = logger;
        LoadCheckpoint();
    }

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="CouncilSpoolerService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Performs begin as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the council spooler operation and used when producing its result.</param>
    public void Begin(MultiModelCouncilResult result)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(result);
            lock (mutationGate)
            {
                runs[result.RunId] = new CouncilSpoolerSnapshot
                {
                    RunId = result.RunId,
                    StartedAtUtc = result.StartedAtUtc,
                    UpdatedAtUtc = DateTime.UtcNow,
                    Status = vocabulary.Get().CouncilSpoolerRunning,
                    Prompt = result.Prompt,
                    CouncilTeamKey = result.CouncilTeamKey,
                    ModelNames = [.. result.ModelNames]
                };
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Begin)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Begin)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs update as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council spooler operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council spooler operation and used when producing its result.</param>
    public void Update(Guid runId, int round, string phase)
    {
    try
    {
            lock (mutationGate)
            {
                if (!runs.TryGetValue(runId, out var snapshot)) return;
                snapshot.CurrentRound = Math.Max(0, round);
                snapshot.Phase = string.IsNullOrWhiteSpace(phase) ? snapshot.Phase : phase.Trim();
                snapshot.UpdatedAtUtc = DateTime.UtcNow;
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Update)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Update)} failed.");
        throw;
    }
}

    /// <summary>
    /// Adds step as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="step">Step value supplied to the council spooler operation and used when producing its result.</param>
    public void AddStep(Guid runId, MultiModelCouncilStep step)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(step);
            lock (mutationGate)
            {
                if (!runs.TryGetValue(runId, out var snapshot)) return;
                var existing = snapshot.Steps.FindIndex(item => item.SortOrder == step.SortOrder &&
                    string.Equals(item.ModelName, step.ModelName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Phase, step.Phase, StringComparison.OrdinalIgnoreCase));
                var copy = CloneStep(step);
                if (existing >= 0) snapshot.Steps[existing] = copy;
                else snapshot.Steps.Add(copy);
                snapshot.Steps = snapshot.Steps.OrderBy(item => item.SortOrder).TakeLast(512).ToList();
                snapshot.CurrentRound = Math.Max(snapshot.CurrentRound, step.Round);
                snapshot.Phase = step.Phase;
                snapshot.UpdatedAtUtc = DateTime.UtcNow;
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(AddStep)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(AddStep)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs complete as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the council spooler operation and used when producing its result.</param>
    /// <param name="failed">Value indicating whether failed should apply to this operation.</param>
    public void Complete(MultiModelCouncilResult result, bool failed = false)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(result);
            lock (mutationGate)
            {
                if (!runs.TryGetValue(result.RunId, out var snapshot))
                {
                    snapshot = new CouncilSpoolerSnapshot { RunId = result.RunId, StartedAtUtc = result.StartedAtUtc };
                    runs[result.RunId] = snapshot;
                }
                snapshot.Status = failed ? vocabulary.Get().CouncilSpoolerFailed : vocabulary.Get().CouncilSpoolerCompleted;
                snapshot.CompletedAtUtc = result.CompletedAtUtc ?? DateTime.UtcNow;
                snapshot.UpdatedAtUtc = DateTime.UtcNow;
                snapshot.FinalAnswer = result.FinalAnswer;
                snapshot.Warnings = [.. result.Warnings];
                snapshot.ModelNames = [.. result.ModelNames];
                snapshot.CouncilTeamKey = result.CouncilTeamKey;
                snapshot.Prompt = result.Prompt;
                snapshot.Steps = result.Steps.OrderBy(item => item.SortOrder).TakeLast(512).Select(CloneStep).ToList();
            }
            NotifyChanged();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Complete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Complete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves snapshots as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeCompleted">Value indicating whether include completed should apply to this operation.</param>
    /// <param name="take">Take value supplied to the council spooler operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<CouncilSpoolerSnapshot> GetSnapshots(bool includeCompleted = true, int take = 30)
    {
    try
    {
            lock (mutationGate)
            {
                return runs.Values
                    .Where(item => includeCompleted || item.Status == vocabulary.Get().CouncilSpoolerRunning)
                    .OrderByDescending(item => item.Status == vocabulary.Get().CouncilSpoolerRunning)
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(CloneSnapshot)
                    .ToList();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(GetSnapshots)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(GetSnapshots)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves snapshot as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council spooler snapshot produced by the operation.</returns>
    public CouncilSpoolerSnapshot? GetSnapshot(Guid runId)
    {
    try
    {
            lock (mutationGate)
                return runs.TryGetValue(runId, out var snapshot) ? CloneSnapshot(snapshot) : null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(GetSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(GetSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs notify changed as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void NotifyChanged()
    {
        var listeners = Changed?.GetInvocationList().Cast<Action>().ToArray() ?? [];
        foreach (var listener in listeners)
        {
            try { listener(); }
            catch (Exception ex) { logger.LogDebug(ex, "A Council spooler UI listener disconnected."); }
        }
        SchedulePersist();
    }

    /// <summary>
    /// Performs schedule persist as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void SchedulePersist()
    {
    try
    {
            if (disposed) return;
            var next = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref pendingPersist, next);
            previous?.Cancel();
            previous?.Dispose();
            _ = PersistAfterDelayAsync(next.Token);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(SchedulePersist)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(SchedulePersist)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists after delay as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PersistAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            await persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var snapshots = GetSnapshots(includeCompleted: true, take: 50);
                Directory.CreateDirectory(Path.GetDirectoryName(CheckpointPath)!);
                var temporary = CheckpointPath + ".tmp";
                var stream = File.Create(temporary);
                await using (stream.ConfigureAwait(false))
                    await JsonSerializer.SerializeAsync(stream, snapshots, JsonOptions, cancellationToken).ConfigureAwait(false);
                File.Move(temporary, CheckpointPath, overwrite: true);
            }
            finally { persistenceGate.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { logger.LogWarning(ex, "Could not persist the bounded Council spooler checkpoint."); }
    }

    /// <summary>
    /// Loads checkpoint as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void LoadCheckpoint()
    {
        try
        {
            if (!File.Exists(CheckpointPath)) return;
            using var stream = File.OpenRead(CheckpointPath);
            var saved = JsonSerializer.Deserialize<List<CouncilSpoolerSnapshot>>(stream, JsonOptions) ?? [];
            foreach (var snapshot in saved.Take(50))
            {
                // A process restart cannot keep the actual model task alive. Preserve the transcript and
                // mark previously-running checkpoints as failed/recoverable context instead of pretending.
                if (snapshot.Status == vocabulary.Get().CouncilSpoolerRunning)
                {
                    snapshot.Status = vocabulary.Get().CouncilSpoolerFailed;
                    snapshot.CompletedAtUtc ??= DateTime.UtcNow;
                    snapshot.Warnings.Add("The LocalGPT process restarted. The saved transcript remains rejoinable, but the former in-flight model call cannot be resumed automatically.");
                }
                runs[snapshot.RunId] = snapshot;
            }
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not load the previous Council spooler checkpoint; active database and chat memory remain unchanged."); }
    }

    /// <summary>
    /// Performs clone snapshot as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the council spooler operation and used when producing its result.</param>
    /// <returns>The council spooler snapshot produced by the operation.</returns>
    private CouncilSpoolerSnapshot CloneSnapshot(CouncilSpoolerSnapshot source) {
    try
    {
        return new()
    {
        RunId = source.RunId,
        StartedAtUtc = source.StartedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc,
        CompletedAtUtc = source.CompletedAtUtc,
        Status = source.Status,
        CurrentRound = source.CurrentRound,
        Phase = source.Phase,
        Prompt = source.Prompt,
        CouncilTeamKey = source.CouncilTeamKey,
        ModelNames = [.. source.ModelNames],
        Steps = source.Steps.Select(CloneStep).ToList(),
        FinalAnswer = source.FinalAnswer,
        Warnings = [.. source.Warnings]
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(CloneSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(CloneSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone step as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the council spooler operation and used when producing its result.</param>
    /// <returns>The multi model council step produced by the operation.</returns>
    private MultiModelCouncilStep CloneStep(MultiModelCouncilStep source) {
    try
    {
        return new()
    {
        SortOrder = source.SortOrder,
        Round = source.Round,
        Phase = source.Phase,
        ModelName = source.ModelName,
        CouncilMembers = [.. source.CouncilMembers],
        Role = source.Role,
        HardwareLane = source.HardwareLane,
        HardwareKind = source.HardwareKind,
        HardwareIndex = source.HardwareIndex,
        EffectiveLoadPercent = source.EffectiveLoadPercent,
        EffectiveMaxOutputTokens = source.EffectiveMaxOutputTokens,
        EffectiveMaxContextTokens = source.EffectiveMaxContextTokens,
        Content = source.Content,
        VisibleContent = source.VisibleContent,
        Thinking = source.Thinking,
        StartedAtUtc = source.StartedAtUtc,
        CompletedAtUtc = source.CompletedAtUtc,
        DurationSeconds = source.DurationSeconds,
        Error = source.Error
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(CloneStep)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(CloneStep)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="CouncilSpoolerService"/> and leaves the council spooler workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
    try
    {
            disposed = true;
            var pending = Interlocked.Exchange(ref pendingPersist, null);
            pending?.Cancel();
            pending?.Dispose();
            persistenceGate.Dispose();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Dispose)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilSpoolerService)}.{nameof(Dispose)} failed.");
        throw;
    }
}
}
