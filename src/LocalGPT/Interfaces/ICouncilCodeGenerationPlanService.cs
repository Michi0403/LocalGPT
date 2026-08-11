using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council code generation plan service contract.
/// </summary>
public interface ICouncilCodeGenerationPlanService
{
    /// <summary>
    /// Runs the parse operation.
    /// </summary>
    CouncilCodeGenerationPlanResult Parse(string councilAnswer);
}
