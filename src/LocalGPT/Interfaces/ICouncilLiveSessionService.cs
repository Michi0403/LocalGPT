using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilLiveSessionService
{
    event Action<Guid>? Changed;

    CancellationToken Begin(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript);
    void Append(Guid runId, string text);
    void SetStatus(Guid runId, string statusMessage);
    void Touch(Guid runId);
    void AppendUserMessage(Guid runId, string text);
    void Complete(Guid runId);
    bool Cancel(Guid runId);
    CouncilLiveSessionSnapshot? Get(Guid runId);
    CouncilLiveSessionSummary? GetSummary(Guid runId);
    IReadOnlyList<CouncilLiveSessionSnapshot> GetActive();
    IReadOnlyList<CouncilLiveSessionSummary> GetActiveSummaries();
}
