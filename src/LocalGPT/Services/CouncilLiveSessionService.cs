using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates council live session behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the council live session workflow to provide the corresponding application capability.</param>
public sealed class CouncilLiveSessionService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<CouncilLiveSessionService> logger) : ICouncilLiveSessionService
{
    /// <summary>
    /// Gets the max transcript characters value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max transcript characters value exposed by <see cref="CouncilLiveSessionService"/>.</value>
    private int MaxTranscriptCharacters => Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.CouncilLiveMaximumTranscriptCharacters));
    /// <summary>
    /// Gets the transcript trim target characters value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transcript trim target characters value exposed by <see cref="CouncilLiveSessionService"/>.</value>
    private int TranscriptTrimTargetCharacters => Math.Max(1, MaxTranscriptCharacters - Math.Max(1, MaxTranscriptCharacters / 8));
    /// <summary>
    /// Gets the max participant activity characters value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max participant activity characters value exposed by <see cref="CouncilLiveSessionService"/>.</value>
    private int MaxParticipantActivityCharacters => Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.CouncilLiveMaximumParticipantActivityCharacters));
    /// <summary>
    /// Gets the participant activity trim target characters value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The participant activity trim target characters value exposed by <see cref="CouncilLiveSessionService"/>.</value>
    private int ParticipantActivityTrimTargetCharacters => Math.Max(1, MaxParticipantActivityCharacters - Math.Max(1, MaxParticipantActivityCharacters / 8));
    /// <summary>
    /// Gets the live transcript display characters value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The live transcript display characters value exposed by <see cref="CouncilLiveSessionService"/>.</value>
    private int LiveTranscriptDisplayCharacters => Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.CouncilLiveMaximumDisplayCharacters));

    /// <summary>Maximum transient stream projection for a participant that is still actively producing output.</summary>
    private const int RunningParticipantDisplayCharacters = 64_000;
    /// <summary>Completed lanes keep only a small transient audit window because they are rendered on demand after completion.</summary>
    private const int CompletedParticipantDisplayCharacters = 8_000;
    /// <summary>Maximum completed final-answer projection placed on a recurrent live circuit; the authoritative server-owned answer is unchanged.</summary>
    private const int CompletedParticipantFinalDisplayCharacters = 16_000;
    /// <summary>
    /// Stores the in-memory sessions collection maintained internally by <see cref="CouncilLiveSessionService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CouncilLiveSessionState> sessions = new();

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="CouncilLiveSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<Guid>? Changed;

    /// <summary>
    /// Performs begin as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="councilMembers">String dependency used by the council live session workflow to provide the corresponding application capability.</param>
    /// <param name="userMessage">User message value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="initialTranscript">Initial transcript value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>The cancellation token produced by the operation.</returns>
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
    /// Performs append as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
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
                    state.Transcript.Remove(0, state.Transcript.Length - TranscriptTrimTargetCharacters);
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
    /// Performs begin participant activity as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="role">Role value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="routeLabel">Route label value supplied to the council live session operation and used when producing its result.</param>
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
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
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
                if (activity.Content.Length > MaxParticipantActivityCharacters)
                    activity.Content.Remove(0, activity.Content.Length - ParticipantActivityTrimTargetCharacters);
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
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
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
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="finalContent">Final content value supplied to the council live session operation and used when producing its result.</param>
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
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
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
    /// Sets status as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
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
    /// Performs touch as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
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
    /// Performs append user message as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
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
    /// Performs complete as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
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
    /// Determines whether cel as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs get as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council live session snapshot produced by the operation.</returns>
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

    /// <summary>Returns the lightweight browser-attachment state for a Council run without cloning transcript or participant buffers.</summary>
    /// <param name="runId">Identifier of the Council run.</param>
    /// <returns>A lightweight attachment snapshot, or null when the run is unknown.</returns>
    public CouncilLiveSessionAttachmentSnapshot? GetAttachmentSnapshot(Guid runId)
    {
        try
        {
            return sessions.TryGetValue(runId, out var state) ? CreateAttachmentSnapshot(state) : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading live Council attachment state failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>Returns the newest rich participant-lane snapshots without copying the potentially multi-megabyte ordered transcript.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>The newest participant activity snapshots, or an empty collection when the run is unknown.</returns>
    public IReadOnlyList<CouncilLiveParticipantActivitySnapshot> GetParticipantActivities(Guid runId)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return [];
            lock (state.SyncRoot)
            {
                return state.ParticipantActivities.Values
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
                    .ToArray();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading live Council participant activities failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>Returns the newest ordered live transcript without copying all participant-lane stream buffers.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>The current transient transcript, or an empty string when the run is unknown.</returns>
    public string GetTranscript(Guid runId)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return string.Empty;
            lock (state.SyncRoot)
                return state.Transcript.ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading live Council transcript failed for run {RunId}.", runId);
            throw;
        }
    }


    /// <summary>Returns browser-safe participant projections without repeatedly copying and Markdown-rendering every historical transient token.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>Bounded participant activity snapshots suitable for recurrent Blazor rendering.</returns>
    public IReadOnlyList<CouncilLiveParticipantActivitySnapshot> GetParticipantActivitiesForDisplay(Guid runId)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return [];
            lock (state.SyncRoot)
            {
                return state.ParticipantActivities.Values
                    .OrderBy(activity => activity.StartedAtUtc)
                    .Select(activity => new CouncilLiveParticipantActivitySnapshot(
                        activity.ActivityKey,
                        activity.ModelName,
                        activity.Phase,
                        activity.Role,
                        activity.RouteLabel,
                        activity.StatusMessage,
                        WindowForDisplay(
                            activity.Content,
                            activity.IsRunning ? RunningParticipantDisplayCharacters : CompletedParticipantDisplayCharacters,
                            "participant provider stream"),
                        activity.IsRunning
                            ? activity.FinalContent
                            : WindowForDisplay(activity.FinalContent, CompletedParticipantFinalDisplayCharacters, "completed participant answer"),
                        activity.IsRunning,
                        activity.StartedAtUtc,
                        activity.UpdatedAtUtc))
                    .ToArray();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading browser-safe live Council participant activities failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>Returns a bounded live transcript projection for the browser while the full server-owned buffer remains available to persistence/completion code.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>The head/tail transcript projection used only by recurrent live rendering.</returns>
    public string GetTranscriptForDisplay(Guid runId)
    {
        try
        {
            if (!sessions.TryGetValue(runId, out var state))
                return string.Empty;
            lock (state.SyncRoot)
                return WindowForDisplay(state.Transcript, LiveTranscriptDisplayCharacters, "ordered Council transcript");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading browser-safe live Council transcript failed for run {RunId}.", runId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves summary as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council live session summary produced by the operation.</returns>
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
    /// Retrieves active as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Retrieves active summaries as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>Builds a small head/tail projection directly from a server-owned StringBuilder without first materializing its entire large buffer.</summary>
    /// <param name="buffer">Server-owned live text buffer.</param>
    /// <param name="maxCharacters">Maximum browser projection length.</param>
    /// <param name="label">Human-readable stream label used in the omission marker.</param>
    /// <returns>The complete text when small enough, otherwise a head/tail window.</returns>
    private string WindowForDisplay(StringBuilder buffer, int maxCharacters, string label)
    {
        try
        {
            if (buffer.Length <= maxCharacters)
                return buffer.ToString();

            var marker = $"\n\n> _Live view windowed for browser responsiveness: older middle text from the {label} is omitted here. The server-owned run state and authoritative final answers are not deleted._\n\n";
            var available = Math.Max(2, maxCharacters - marker.Length);
            var head = Math.Min(16_000, available / 4);
            var tail = available - head;
            return string.Concat(
                buffer.ToString(0, head),
                marker,
                buffer.ToString(buffer.Length - tail, tail));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the browser-safe Council live text projection failed; transcript content was omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>Builds a bounded head/tail projection from immutable completed text without altering the server-owned authoritative value.</summary>
    /// <param name="text">Completed text to project into the recurrent browser view.</param>
    /// <param name="maxCharacters">Maximum browser projection length.</param>
    /// <param name="label">Human-readable stream label used in the omission marker.</param>
    /// <returns>The complete text when small enough, otherwise a head/tail window.</returns>
    private string WindowForDisplay(string? text, int maxCharacters, string label)
    {
        try
        {
            var value = text ?? string.Empty;
            if (value.Length <= maxCharacters)
                return value;

            var marker = $"\n\n> _Live view windowed for browser responsiveness: older middle text from the {label} is omitted here. The authoritative server-owned answer is not deleted._\n\n";
            var available = Math.Max(2, maxCharacters - marker.Length);
            var head = Math.Min(4_000, available / 3);
            var tail = available - head;
            return string.Concat(value[..head], marker, value[(value.Length - tail)..]);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a browser-safe completed Council text projection failed; content was omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>
    /// Creates snapshot as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>The council live session snapshot produced by the operation.</returns>
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

    /// <summary>Creates the lightweight browser-attachment projection without materializing large transcript or participant buffers.</summary>
    /// <param name="state">Mutable server-owned live-session state.</param>
    /// <returns>The lightweight attachment projection.</returns>
    private CouncilLiveSessionAttachmentSnapshot CreateAttachmentSnapshot(CouncilLiveSessionState state)
    {
        try
        {
            lock (state.SyncRoot)
            {
                return new CouncilLiveSessionAttachmentSnapshot(
                    state.RunId,
                    state.IsRunning,
                    state.StartedAtUtc,
                    state.UpdatedAtUtc,
                    state.CouncilMembers,
                    state.UserMessage,
                    state.AdditionalUserMessages.ToArray(),
                    state.StatusMessage);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a lightweight Council live-session attachment snapshot failed for run {RunId}.", state.RunId);
            throw;
        }
    }

    /// <summary>
    /// Creates summary as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>The council live session summary produced by the operation.</returns>
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
    /// Performs append with block boundary as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="transcript">Transcript value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
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
    /// Performs ends visible block as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="transcript">Transcript value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Starts s visible block as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs schedule changed as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="state">State value supplied to the council live session operation and used when producing its result.</param>
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
