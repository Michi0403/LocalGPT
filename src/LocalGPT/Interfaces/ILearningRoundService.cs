using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the learning round service contract.
/// </summary>
public interface ILearningRoundService
{
    /// <summary>
    /// Builds snapshot async.
    /// </summary>
    Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the maintain async operation.
    /// </summary>
    Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default);
}
