using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council live session behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilLiveSessionService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ICouncilLiveSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action<Guid>? Changed;

    /// <summary>
    /// Performs begin as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="councilMembers">String dependency used by the council live session workflow to provide the corresponding application capability.</param>
    /// <param name="userMessage">User message value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="initialTranscript">Initial transcript value supplied to the council live session operation and used when producing its result.</param>
    /// <returns>The cancellation token produced by the operation.</returns>
    CancellationToken Begin(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript);
    /// <summary>
    /// Performs append as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
    void Append(Guid runId, string text);
    /// <summary>
    /// Starts or refreshes a live participant activity stream without changing ordered transcript presentation.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="role">Role value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="routeLabel">Route label value supplied to the council live session operation and used when producing its result.</param>
    void BeginParticipantActivity(Guid runId, string activityKey, string modelName, string phase, string role, string routeLabel);
    /// <summary>
    /// Appends provider thinking, status markup, function-call notices, or response text to one live participant stream.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
    void AppendParticipantActivity(Guid runId, string activityKey, string text);
    /// <summary>
    /// Sets participant activity status as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
    void SetParticipantActivityStatus(Guid runId, string activityKey, string statusMessage);
    /// <summary>
    /// Stores the authoritative final answer for one participant independently from its incremental provider stream.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="finalContent">Final content value supplied to the council live session operation and used when producing its result.</param>
    void SetParticipantActivityResult(Guid runId, string activityKey, string finalContent);
    /// <summary>
    /// Marks one live participant stream complete while retaining it until the overall Council run finishes.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="activityKey">Activity key value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
    void CompleteParticipantActivity(Guid runId, string activityKey, string statusMessage);
    /// <summary>
    /// Sets status as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="statusMessage">Status message value supplied to the council live session operation and used when producing its result.</param>
    void SetStatus(Guid runId, string statusMessage);
    /// <summary>
    /// Performs touch as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    void Touch(Guid runId);
    /// <summary>
    /// Performs append user message as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="text">Text value supplied to the council live session operation and used when producing its result.</param>
    void AppendUserMessage(Guid runId, string text);
    /// <summary>
    /// Performs complete as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    void Complete(Guid runId);
    /// <summary>
    /// Determines whether cel as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Cancel(Guid runId);
    /// <summary>
    /// Performs get as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council live session snapshot produced by the operation.</returns>
    CouncilLiveSessionSnapshot? Get(Guid runId);
    /// <summary>Returns only the current rich participant lanes without copying the potentially multi-megabyte ordered transcript.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>The newest participant activity snapshots, or an empty collection when the run is unknown.</returns>
    IReadOnlyList<CouncilLiveParticipantActivitySnapshot> GetParticipantActivities(Guid runId);
    /// <summary>Returns only the current ordered live transcript without copying all participant-lane stream buffers.</summary>
    /// <param name="runId">Identifier of the live Council run.</param>
    /// <returns>The newest transient transcript or an empty string when the run is unknown.</returns>
    string GetTranscript(Guid runId);
    /// <summary>
    /// Retrieves summary as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council live session summary produced by the operation.</returns>
    CouncilLiveSessionSummary? GetSummary(Guid runId);
    /// <summary>
    /// Retrieves active as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilLiveSessionSnapshot> GetActive();
    /// <summary>
    /// Retrieves active summaries as part of the council live session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilLiveSessionSummary> GetActiveSummaries();
}
