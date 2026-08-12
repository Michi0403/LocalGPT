namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council live session snapshot.
/// </summary>
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
/// Represents a council live session summary.
/// </summary>
public sealed record CouncilLiveSessionSummary(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string StatusMessage);
