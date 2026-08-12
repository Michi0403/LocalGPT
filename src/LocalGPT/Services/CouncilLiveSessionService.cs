using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Provides council live session service operations.
/// </summary>
public sealed class CouncilLiveSessionService(
    ILogger<CouncilLiveSessionService> logger) : ICouncilLiveSessionService
{
    private const int MaxTranscriptCharacters = 2_000_000;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CouncilLiveSessionState> sessions = new();

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action<Guid>? Changed;

    /// <summary>
    /// Runs the begin operation.
    /// </summary>
    public CancellationToken Begin(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript)
    {
    try
    {
            var state = new CouncilLiveSessionState(runId, councilMembers, userMessage, initialTranscript);
            if (sessions.TryGetValue(runId, out var previous))
                previous.Dispose();
            sessions[runId] = state;
            ScheduleChanged(state);
            logger.LogInformation("Registered live Council session {RunId} with {MemberCount} member(s).", runId, councilMembers.Count);
            return state.Cancellation.Token;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Begin)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Begin)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the append operation.
    /// </summary>
    public void Append(Guid runId, string text)
    {
    try
    {
            if (string.IsNullOrEmpty(text) || !sessions.TryGetValue(runId, out var state))
                return;

            lock (state.SyncRoot)
            {
                AppendWithBlockBoundary(state.Transcript, text);
                if (state.Transcript.Length > MaxTranscriptCharacters)
                    state.Transcript.Remove(0, state.Transcript.Length - MaxTranscriptCharacters);
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Append)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Append)} failed.");
        throw;
    }
}

    /// <summary>
    /// Starts or refreshes one provider-qualified participant activity stream.
    /// </summary>
    public void BeginParticipantActivity(Guid runId, string activityKey, string modelName, string phase, string role, string routeLabel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(activityKey) || !sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                state.ParticipantActivities[activityKey] = new CouncilLiveParticipantActivityState(
                    activityKey, modelName, phase, role, routeLabel);
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not begin live Council participant activity {ActivityKey} for run {RunId}.", activityKey, runId);
            throw;
        }
    }

    /// <summary>
    /// Appends live provider output to one participant activity independently from ordered transcript presentation.
    /// </summary>
    public void AppendParticipantActivity(Guid runId, string activityKey, string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text) || !sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                if (!state.ParticipantActivities.TryGetValue(activityKey, out var activity))
                    return;
                AppendWithBlockBoundary(activity.Content, text);
                activity.StatusMessage = "Streaming live from the model runtime.";
                activity.UpdatedAtUtc = DateTime.UtcNow;
                state.UpdatedAtUtc = activity.UpdatedAtUtc;
            }
            ScheduleChanged(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not append live Council participant activity {ActivityKey} for run {RunId}.", activityKey, runId);
            throw;
        }
    }

    /// <summary>
    /// Updates one participant activity status while a host queue is running.
    /// </summary>
    public void SetParticipantActivityStatus(Guid runId, string activityKey, string statusMessage)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                if (!state.ParticipantActivities.TryGetValue(activityKey, out var activity))
                    return;
                activity.StatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "Council participant is running." : statusMessage.Trim();
                activity.UpdatedAtUtc = DateTime.UtcNow;
                state.UpdatedAtUtc = activity.UpdatedAtUtc;
            }
            ScheduleChanged(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not update live Council participant activity {ActivityKey} for run {RunId}.", activityKey, runId);
            throw;
        }
    }

    /// <summary>
    /// Stores one participant's authoritative final answer separately from transient streamed provider markup.
    /// </summary>
    public void SetParticipantActivityResult(Guid runId, string activityKey, string finalContent)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                if (!state.ParticipantActivities.TryGetValue(activityKey, out var activity))
                    return;
                activity.FinalContent = finalContent?.Trim() ?? string.Empty;
                activity.UpdatedAtUtc = DateTime.UtcNow;
                state.UpdatedAtUtc = activity.UpdatedAtUtc;
            }
            ScheduleChanged(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not store final live Council participant result {ActivityKey} for run {RunId}.", activityKey, runId);
            throw;
        }
    }

    /// <summary>
    /// Marks one participant stream complete while leaving the final ordered transcript unchanged.
    /// </summary>
    public void CompleteParticipantActivity(Guid runId, string activityKey, string statusMessage)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                if (!state.ParticipantActivities.TryGetValue(activityKey, out var activity))
                    return;
                activity.IsRunning = false;
                activity.StatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "Participant completed; ordered transcript integration is pending." : statusMessage.Trim();
                activity.UpdatedAtUtc = DateTime.UtcNow;
                state.UpdatedAtUtc = activity.UpdatedAtUtc;
            }
            ScheduleChanged(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not complete live Council participant activity {ActivityKey} for run {RunId}.", activityKey, runId);
            throw;
        }
    }

    /// <summary>
    /// Sets status.
    /// </summary>
    public void SetStatus(Guid runId, string statusMessage)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return;

            lock (state.SyncRoot)
            {
                state.StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
                    ? "Council is running."
                    : statusMessage.Trim().Length <= 800
                        ? statusMessage.Trim()
                        : statusMessage.Trim()[..800] + "…";
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(SetStatus)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(SetStatus)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Runs the touch operation.
    /// </summary>
    public void Touch(Guid runId)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return;

            lock (state.SyncRoot)
                state.UpdatedAtUtc = DateTime.UtcNow;
            ScheduleChanged(state);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Touch)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Touch)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Runs the append user message operation.
    /// </summary>
    public void AppendUserMessage(Guid runId, string text)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(text) || !sessions.TryGetValue(runId, out var state))
                return;

            lock (state.SyncRoot)
            {
                state.AdditionalUserMessages.Add(text.Trim());
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(AppendUserMessage)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(AppendUserMessage)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    public void Complete(Guid runId)
    {
    try
    {
            if (!sessions.TryGetValue(runId, out var state))
                return;
            lock (state.SyncRoot)
            {
                state.IsRunning = false;
                state.StatusMessage = "Council completed.";
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
            logger.LogInformation("Completed live Council session {RunId}.", runId);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Complete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Complete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether cel.
    /// </summary>
    public bool Cancel(Guid runId)
    {
    try
    {
            if (!sessions.TryGetValue(runId, out var state))
                return false;
            if (!state.Cancellation.IsCancellationRequested)
                state.Cancellation.Cancel();
            lock (state.SyncRoot)
            {
                state.IsRunning = false;
                state.StatusMessage = "Council cancellation requested.";
                state.UpdatedAtUtc = DateTime.UtcNow;
            }
            ScheduleChanged(state);
            logger.LogInformation("Cancellation was requested for live Council session {RunId}.", runId);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Cancel)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Cancel)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the get operation.
    /// </summary>
    public CouncilLiveSessionSnapshot? Get(Guid runId) {
    try
    {
        return sessions.TryGetValue(runId, out var state) ? CreateSnapshot(state) : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Get)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(Get)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets summary.
    /// </summary>
    public CouncilLiveSessionSummary? GetSummary(Guid runId) {
    try
    {
        return sessions.TryGetValue(runId, out var state) ? CreateSummary(state) : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetSummary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetSummary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets active.
    /// </summary>
    public IReadOnlyList<CouncilLiveSessionSnapshot> GetActive() {
    try
    {
        return sessions.Values
            .Select(CreateSnapshot)
            .Where(snapshot => snapshot.IsRunning)
            .OrderByDescending(snapshot => snapshot.UpdatedAtUtc)
            .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetActive)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetActive)} failed.");
        throw;
    }
}

    /// <summary>
    /// Gets active summaries.
    /// </summary>
    public IReadOnlyList<CouncilLiveSessionSummary> GetActiveSummaries() {
    try
    {
        return sessions.Values
            .Select(CreateSummary)
            .Where(summary => summary.IsRunning)
            .OrderByDescending(summary => summary.UpdatedAtUtc)
            .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetActiveSummaries)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(GetActiveSummaries)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates snapshot.
    /// </summary>
    private CouncilLiveSessionSnapshot CreateSnapshot(CouncilLiveSessionState state)
    {
    try
    {
            lock (state.SyncRoot)
            {
                return new CouncilLiveSessionSnapshot(
                    state.RunId,
                    state.IsRunning,
                    state.StartedAtUtc,
                    state.UpdatedAtUtc,
                    state.CouncilMembers,
                    state.UserMessage,
                    state.AdditionalUserMessages.ToArray(),
                    state.Transcript.ToString(),
                    state.StatusMessage,
                    state.ParticipantActivities.Values
                        .OrderBy(activity => activity.StartedAtUtc)
                        .Select(activity => new CouncilLiveParticipantActivitySnapshot(
                            activity.ActivityKey,
                            activity.ModelName,
                            activity.Phase,
                            activity.Role,
                            activity.RouteLabel,
                            activity.StatusMessage,
                            activity.Content.ToString(),
                            activity.FinalContent,
                            activity.IsRunning,
                            activity.StartedAtUtc,
                            activity.UpdatedAtUtc))
                        .ToArray());
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(CreateSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(CreateSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates summary.
    /// </summary>
    private CouncilLiveSessionSummary CreateSummary(CouncilLiveSessionState state)
    {
    try
    {
            lock (state.SyncRoot)
            {
                return new CouncilLiveSessionSummary(
                    state.RunId,
                    state.IsRunning,
                    state.StartedAtUtc,
                    state.UpdatedAtUtc,
                    state.CouncilMembers,
                    state.StatusMessage);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(CreateSummary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(CreateSummary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the append with block boundary operation.
    /// </summary>
    private void AppendWithBlockBoundary(StringBuilder transcript, string text)
    {
    try
    {
            if (transcript.Length > 0 && transcript[^1] != '\n' && (StartsVisibleBlock(text) || EndsVisibleBlock(transcript)))
                transcript.AppendLine();
            transcript.Append(text);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(AppendWithBlockBoundary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(AppendWithBlockBoundary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the ends visible block operation.
    /// </summary>
    private bool EndsVisibleBlock(StringBuilder transcript)
    {
    try
    {
            var length = Math.Min(transcript.Length, 24);
            if (length == 0) return false;
            var tail = transcript.ToString(transcript.Length - length, length).TrimEnd(' ', '\t', '\r');
            return tail.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)
                || tail.EndsWith("</details>", StringComparison.OrdinalIgnoreCase)
                || tail.EndsWith("</pre>", StringComparison.OrdinalIgnoreCase)
                || tail.EndsWith("</div>", StringComparison.OrdinalIgnoreCase)
                || tail.EndsWith("-->", StringComparison.Ordinal);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(EndsVisibleBlock)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(EndsVisibleBlock)} failed.");
        throw;
    }
}

    /// <summary>
    /// Starts s visible block.
    /// </summary>
    private bool StartsVisibleBlock(string text)
    {
    try
    {
            var trimmed = text.TrimStart(' ', '\t', '\r');
            return trimmed.StartsWith("<p", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("<details", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("_Council", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Council ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("LocalGPT ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Ollama ", StringComparison.OrdinalIgnoreCase);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(StartsVisibleBlock)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilLiveSessionService)}.{nameof(StartsVisibleBlock)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the schedule changed operation.
    /// </summary>
    private void ScheduleChanged(CouncilLiveSessionState state)
    {
        if (Interlocked.Exchange(ref state.NotificationScheduled, 1) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350).ConfigureAwait(false);
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

}
