using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services;

public sealed class CouncilLiveSessionService(
    ILogger<CouncilLiveSessionService> logger) : ICouncilLiveSessionService
{
    private const int MaxTranscriptCharacters = 2_000_000;
    private readonly ConcurrentDictionary<Guid, LiveSessionState> sessions = new();

    public event Action<Guid>? Changed;

    public CancellationToken Begin(Guid runId, IReadOnlyList<string> councilMembers, string initialTranscript)
    {
        var state = new LiveSessionState(runId, councilMembers, initialTranscript);
        if (sessions.TryGetValue(runId, out var previous))
            previous.Dispose();
        sessions[runId] = state;
        ScheduleChanged(state);
        logger.LogInformation("Registered live Council session {RunId} with {MemberCount} member(s).", runId, councilMembers.Count);
        return state.Cancellation.Token;
    }

    public void Append(Guid runId, string text)
    {
        if (string.IsNullOrEmpty(text) || !sessions.TryGetValue(runId, out var state))
            return;

        lock (state.SyncRoot)
        {
            state.Transcript.Append(text);
            if (state.Transcript.Length > MaxTranscriptCharacters)
                state.Transcript.Remove(0, state.Transcript.Length - MaxTranscriptCharacters);
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
        ScheduleChanged(state);
    }

    public void Complete(Guid runId)
    {
        if (!sessions.TryGetValue(runId, out var state))
            return;
        lock (state.SyncRoot)
        {
            state.IsRunning = false;
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
        ScheduleChanged(state);
        logger.LogInformation("Completed live Council session {RunId}.", runId);
    }

    public bool Cancel(Guid runId)
    {
        if (!sessions.TryGetValue(runId, out var state))
            return false;
        if (!state.Cancellation.IsCancellationRequested)
            state.Cancellation.Cancel();
        lock (state.SyncRoot)
        {
            state.IsRunning = false;
            state.UpdatedAtUtc = DateTime.UtcNow;
        }
        ScheduleChanged(state);
        logger.LogInformation("Cancellation was requested for live Council session {RunId}.", runId);
        return true;
    }

    public CouncilLiveSessionSnapshot? Get(Guid runId) =>
        sessions.TryGetValue(runId, out var state) ? CreateSnapshot(state) : null;

    public IReadOnlyList<CouncilLiveSessionSnapshot> GetActive() =>
        sessions.Values
            .Select(CreateSnapshot)
            .Where(snapshot => snapshot.IsRunning)
            .OrderByDescending(snapshot => snapshot.UpdatedAtUtc)
            .ToList();

    private CouncilLiveSessionSnapshot CreateSnapshot(LiveSessionState state)
    {
        lock (state.SyncRoot)
        {
            return new CouncilLiveSessionSnapshot(
                state.RunId,
                state.IsRunning,
                state.StartedAtUtc,
                state.UpdatedAtUtc,
                state.CouncilMembers,
                state.Transcript.ToString());
        }
    }

    private void ScheduleChanged(LiveSessionState state)
    {
        if (Interlocked.Exchange(ref state.NotificationScheduled, 1) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(180).ConfigureAwait(false);
                Interlocked.Exchange(ref state.NotificationScheduled, 0);
                var listeners = Changed?.GetInvocationList().Cast<Action<Guid>>().ToArray() ?? [];
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener(state.RunId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "A live Council session listener failed for run {RunId}; remaining listeners will still be notified.", state.RunId);
                    }
                }
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref state.NotificationScheduled, 0);
                logger.LogWarning(ex, "Could not publish a live Council session change notification for run {RunId}.", state.RunId);
            }
        });
    }

    private sealed class LiveSessionState : IDisposable
    {
        public LiveSessionState(Guid runId, IReadOnlyList<string> councilMembers, string initialTranscript)
        {
            RunId = runId;
            CouncilMembers = councilMembers.ToArray();
            Transcript = new StringBuilder(initialTranscript ?? string.Empty);
        }

        public object SyncRoot { get; } = new();
        public Guid RunId { get; }
        public IReadOnlyList<string> CouncilMembers { get; }
        public StringBuilder Transcript { get; }
        public CancellationTokenSource Cancellation { get; } = new();
        public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsRunning { get; set; } = true;
        public int NotificationScheduled;

        public void Dispose() => Cancellation.Dispose();
    }
}
