using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilSpoolerService
{
    event Action? Changed;
    void Begin(MultiModelCouncilResult result);
    void Update(Guid runId, int round, string phase);
    void AddStep(Guid runId, MultiModelCouncilStep step);
    void Complete(MultiModelCouncilResult result, bool failed = false);
    IReadOnlyList<CouncilSpoolerSnapshot> GetSnapshots(bool includeCompleted = true, int take = 30);
    CouncilSpoolerSnapshot? GetSnapshot(Guid runId);
}
