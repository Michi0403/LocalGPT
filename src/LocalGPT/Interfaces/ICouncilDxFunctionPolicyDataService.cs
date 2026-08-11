using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council DevExpress function policy data service contract.
/// </summary>
public interface ICouncilDxFunctionPolicyDataService
{
    /// <summary>
    /// Gets policy async.
    /// </summary>
    Task<CouncilDxFunctionPolicy> GetPolicyAsync(CancellationToken cancellationToken = default);
}
