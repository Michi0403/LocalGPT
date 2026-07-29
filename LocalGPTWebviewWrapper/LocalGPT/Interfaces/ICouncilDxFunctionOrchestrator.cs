using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilDxFunctionOrchestrator
{
    Task<IReadOnlyList<MultiModelCouncilStep>> ExecuteRequestedCallsAsync(
        MultiModelCouncilResult result,
        MultiModelCouncilStep sourceStep,
        CancellationToken cancellationToken = default);
}
