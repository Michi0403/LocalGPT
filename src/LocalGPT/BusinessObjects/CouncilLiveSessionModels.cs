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
