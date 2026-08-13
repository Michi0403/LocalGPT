using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council DevExpress function policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilDxFunctionPolicyDataService
{
    /// <summary>
    /// Retrieves policy as part of the council DevExpress function policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council DevExpress function policy produced by the operation.</returns>
    Task<CouncilDxFunctionPolicy> GetPolicyAsync(CancellationToken cancellationToken = default);
}
