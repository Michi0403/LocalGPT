namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council live session snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="IsRunning">Value indicating whether running should apply to this operation.</param>
/// <param name="StartedAtUtc">Started at utc value supplied to the council live session snapshot operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the council live session snapshot operation and used when producing its result.</param>
/// <param name="CouncilMembers">String dependency used by the council live session snapshot workflow to provide the corresponding application capability.</param>
/// <param name="UserMessage">User message value supplied to the council live session snapshot operation and used when producing its result.</param>
/// <param name="AdditionalUserMessages">String dependency used by the council live session snapshot workflow to provide the corresponding application capability.</param>
/// <param name="Transcript">Transcript value supplied to the council live session snapshot operation and used when producing its result.</param>
/// <param name="StatusMessage">Status message value supplied to the council live session snapshot operation and used when producing its result.</param>
/// <param name="ParticipantActivities">Council live participant activity snapshot dependency used by the council live session snapshot workflow to provide the corresponding application capability.</param>
public sealed record CouncilLiveSessionSnapshot(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string UserMessage,
    IReadOnlyList<string> AdditionalUserMessages,
    string Transcript,
    string StatusMessage,
    IReadOnlyList<CouncilLiveParticipantActivitySnapshot> ParticipantActivities);

/// <summary>
/// Represents the live, independently updating stream for one Council participant while host queues run in parallel.
/// </summary>
/// <param name="ActivityKey">Activity key value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="ModelName">Model name value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="Phase">Phase value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="Role">Role value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="RouteLabel">Route label value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="StatusMessage">Status message value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="Content">Content value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="FinalContent">Final content value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="IsRunning">Value indicating whether running should apply to this operation.</param>
/// <param name="StartedAtUtc">Started at utc value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the council live participant activity snapshot operation and used when producing its result.</param>
public sealed record CouncilLiveParticipantActivitySnapshot(
    string ActivityKey,
    string ModelName,
    string Phase,
    string Role,
    string RouteLabel,
    string StatusMessage,
    string Content,
    string FinalContent,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc);


/// <summary>Lightweight attachment state used when a browser circuit joins an already running Council without copying transcript or participant stream buffers.</summary>
/// <param name="RunId">Identifier of the Council run.</param>
/// <param name="IsRunning">Indicates whether the Council is still executing.</param>
/// <param name="StartedAtUtc">Time at which the Council run started.</param>
/// <param name="UpdatedAtUtc">Time of the newest server-side live-session change.</param>
/// <param name="CouncilMembers">Provider-qualified members assigned to the run.</param>
/// <param name="UserMessage">Original user request that started the run.</param>
/// <param name="AdditionalUserMessages">Human messages queued after the run began.</param>
/// <param name="StatusMessage">Current server-side run status.</param>
public sealed record CouncilLiveSessionAttachmentSnapshot(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string UserMessage,
    IReadOnlyList<string> AdditionalUserMessages,
    string StatusMessage);

/// <summary>
/// Represents a council live session summary application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="IsRunning">Value indicating whether running should apply to this operation.</param>
/// <param name="StartedAtUtc">Started at utc value supplied to the council live session summary operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the council live session summary operation and used when producing its result.</param>
/// <param name="CouncilMembers">String dependency used by the council live session summary workflow to provide the corresponding application capability.</param>
/// <param name="StatusMessage">Status message value supplied to the council live session summary operation and used when producing its result.</param>
public sealed record CouncilLiveSessionSummary(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string StatusMessage);
