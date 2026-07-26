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
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConcurrentDictionary<Guid, CouncilSpoolerSnapshot> runs = new();
    private readonly object mutationGate = new();
    private readonly SemaphoreSlim persistenceGate = new(1, 1);
    private readonly ILogger<CouncilSpoolerService> logger;
    private CancellationTokenSource? pendingPersist;
    private bool disposed;
    private string CheckpointPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LocalGPT",
        "CouncilSpooler",
        "recent-runs.json");

    public CouncilSpoolerService(ILogger<CouncilSpoolerService> logger)
    {
        this.logger = logger;
        LoadCheckpoint();
    }

    public event Action? Changed;

    public void Begin(MultiModelCouncilResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        lock (mutationGate)
        {
            runs[result.RunId] = new CouncilSpoolerSnapshot
            {
                RunId = result.RunId,
                StartedAtUtc = result.StartedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = CouncilSpoolerStatuses.Running,
                Prompt = result.Prompt,
                CouncilTeamKey = result.CouncilTeamKey,
                ModelNames = [.. result.ModelNames]
            };
        }
        NotifyChanged();
    }

    public void Update(Guid runId, int round, string phase)
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

    public void AddStep(Guid runId, MultiModelCouncilStep step)
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

    public void Complete(MultiModelCouncilResult result, bool failed = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        lock (mutationGate)
        {
            if (!runs.TryGetValue(result.RunId, out var snapshot))
            {
                snapshot = new CouncilSpoolerSnapshot { RunId = result.RunId, StartedAtUtc = result.StartedAtUtc };
                runs[result.RunId] = snapshot;
            }
            snapshot.Status = failed ? CouncilSpoolerStatuses.Failed : CouncilSpoolerStatuses.Completed;
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

    public IReadOnlyList<CouncilSpoolerSnapshot> GetSnapshots(bool includeCompleted = true, int take = 30)
    {
        lock (mutationGate)
        {
            return runs.Values
                .Where(item => includeCompleted || item.Status == CouncilSpoolerStatuses.Running)
                .OrderByDescending(item => item.Status == CouncilSpoolerStatuses.Running)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .Take(Math.Clamp(take, 1, 100))
                .Select(CloneSnapshot)
                .ToList();
        }
    }

    public CouncilSpoolerSnapshot? GetSnapshot(Guid runId)
    {
        lock (mutationGate)
            return runs.TryGetValue(runId, out var snapshot) ? CloneSnapshot(snapshot) : null;
    }

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

    private void SchedulePersist()
    {
        if (disposed) return;
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref pendingPersist, next);
        previous?.Cancel();
        previous?.Dispose();
        _ = PersistAfterDelayAsync(next.Token);
    }

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
                await using (var stream = File.Create(temporary))
                    await JsonSerializer.SerializeAsync(stream, snapshots, JsonOptions, cancellationToken).ConfigureAwait(false);
                File.Move(temporary, CheckpointPath, overwrite: true);
            }
            finally { persistenceGate.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { logger.LogWarning(ex, "Could not persist the bounded Council spooler checkpoint."); }
    }

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
                if (snapshot.Status == CouncilSpoolerStatuses.Running)
                {
                    snapshot.Status = CouncilSpoolerStatuses.Failed;
                    snapshot.CompletedAtUtc ??= DateTime.UtcNow;
                    snapshot.Warnings.Add("The LocalGPT process restarted. The saved transcript remains rejoinable, but the former in-flight model call cannot be resumed automatically.");
                }
                runs[snapshot.RunId] = snapshot;
            }
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not load the previous Council spooler checkpoint; active database and chat memory remain unchanged."); }
    }

    private static CouncilSpoolerSnapshot CloneSnapshot(CouncilSpoolerSnapshot source) => new()
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

    private static MultiModelCouncilStep CloneStep(MultiModelCouncilStep source) => new()
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

    public void Dispose()
    {
        disposed = true;
        var pending = Interlocked.Exchange(ref pendingPersist, null);
        pending?.Cancel();
        pending?.Dispose();
        persistenceGate.Dispose();
    }
}
