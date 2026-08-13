using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council DevExpress function orchestrator behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilDxFunctionOrchestrator
{
    /// <summary>
    /// Executes requested calls for <see cref="ICouncilDxFunctionOrchestrator"/>, keeping the operation consistent with the state and invariants of the surrounding council DevExpress function orchestrator workflow.
    /// </summary>
    /// <param name="result">Result value supplied to the council DevExpress function orchestrator operation and used when producing its result.</param>
    /// <param name="sourceStep">Source step value supplied to the council DevExpress function orchestrator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<MultiModelCouncilStep>> ExecuteRequestedCallsAsync(
        MultiModelCouncilResult result,
        MultiModelCouncilStep sourceStep,
        CancellationToken cancellationToken = default);
}
