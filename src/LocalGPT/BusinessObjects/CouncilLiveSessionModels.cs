namespace LocalGPT.BusinessObjects;

public sealed record CouncilLiveSessionSnapshot(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string UserMessage,
    IReadOnlyList<string> AdditionalUserMessages,
    string Transcript);

public sealed record CouncilLiveSessionSummary(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers);
