using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilLiveSessionService
{
    event Action<Guid>? Changed;

    CancellationToken Begin(Guid runId, IReadOnlyList<string> councilMembers, string initialTranscript);
    void Append(Guid runId, string text);
    void Complete(Guid runId);
    bool Cancel(Guid runId);
    CouncilLiveSessionSnapshot? Get(Guid runId);
    IReadOnlyList<CouncilLiveSessionSnapshot> GetActive();
}
