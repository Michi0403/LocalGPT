using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council DevExpress function orchestrator contract.
/// </summary>
public interface ICouncilDxFunctionOrchestrator
{
    /// <summary>
    /// Runs the execute requested calls async operation.
    /// </summary>
    Task<IReadOnlyList<MultiModelCouncilStep>> ExecuteRequestedCallsAsync(
        MultiModelCouncilResult result,
        MultiModelCouncilStep sourceStep,
        CancellationToken cancellationToken = default);
}
