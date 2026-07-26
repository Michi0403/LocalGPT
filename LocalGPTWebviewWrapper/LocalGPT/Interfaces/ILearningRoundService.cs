using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ILearningRoundService
{
    Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default);
    Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default);
}
