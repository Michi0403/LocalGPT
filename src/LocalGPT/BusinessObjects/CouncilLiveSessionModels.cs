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
    string StatusMessage);

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
