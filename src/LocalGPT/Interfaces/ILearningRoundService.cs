using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for learning round behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILearningRoundService
{
    /// <summary>
    /// Builds snapshot as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="takePerSource">Take per source value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning round snapshot produced by the operation.</returns>
    Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs maintain as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning maintenance result produced by the operation.</returns>
    Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default);
}
