using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council code generation plan behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilCodeGenerationPlanService
{
    /// <summary>
    /// Performs parse as part of the council code generation plan service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilAnswer">Council answer value supplied to the council code generation plan operation and used when producing its result.</param>
    /// <returns>The council code generation plan result produced by the operation.</returns>
    CouncilCodeGenerationPlanResult Parse(string councilAnswer);
}
