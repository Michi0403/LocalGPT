using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council spooler behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilSpoolerService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ICouncilSpoolerService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Performs begin as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the council spooler operation and used when producing its result.</param>
    void Begin(MultiModelCouncilResult result);
    /// <summary>
    /// Performs update as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council spooler operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council spooler operation and used when producing its result.</param>
    void Update(Guid runId, int round, string phase);
    /// <summary>
    /// Adds step as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="step">Step value supplied to the council spooler operation and used when producing its result.</param>
    void AddStep(Guid runId, MultiModelCouncilStep step);
    /// <summary>
    /// Performs complete as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the council spooler operation and used when producing its result.</param>
    /// <param name="failed">Value indicating whether failed should apply to this operation.</param>
    void Complete(MultiModelCouncilResult result, bool failed = false);
    /// <summary>
    /// Retrieves snapshots as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeCompleted">Value indicating whether include completed should apply to this operation.</param>
    /// <param name="take">Take value supplied to the council spooler operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilSpoolerSnapshot> GetSnapshots(bool includeCompleted = true, int take = 30);
    /// <summary>
    /// Retrieves snapshot as part of the council spooler service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council spooler snapshot produced by the operation.</returns>
    CouncilSpoolerSnapshot? GetSnapshot(Guid runId);
}
