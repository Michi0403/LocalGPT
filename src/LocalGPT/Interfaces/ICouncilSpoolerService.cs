using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council spooler service contract.
/// </summary>
public interface ICouncilSpoolerService
{
    event Action? Changed;
    /// <summary>
    /// Runs the begin operation.
    /// </summary>
    void Begin(MultiModelCouncilResult result);
    /// <summary>
    /// Runs the update operation.
    /// </summary>
    void Update(Guid runId, int round, string phase);
    /// <summary>
    /// Adds step.
    /// </summary>
    void AddStep(Guid runId, MultiModelCouncilStep step);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    void Complete(MultiModelCouncilResult result, bool failed = false);
    /// <summary>
    /// Gets snapshots.
    /// </summary>
    IReadOnlyList<CouncilSpoolerSnapshot> GetSnapshots(bool includeCompleted = true, int take = 30);
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    CouncilSpoolerSnapshot? GetSnapshot(Guid runId);
}
