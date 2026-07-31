namespace LocalGPT.BusinessObjects;

public sealed record CouncilLiveSessionSnapshot(
    Guid RunId,
    bool IsRunning,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> CouncilMembers,
    string Transcript);
