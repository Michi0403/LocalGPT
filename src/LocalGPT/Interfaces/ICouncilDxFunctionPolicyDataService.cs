using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilDxFunctionPolicyDataService
{
    Task<CouncilDxFunctionPolicy> GetPolicyAsync(CancellationToken cancellationToken = default);
}
