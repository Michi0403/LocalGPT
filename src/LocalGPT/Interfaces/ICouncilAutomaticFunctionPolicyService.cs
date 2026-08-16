using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Resolves the persisted user-editable automatic/native function policy for one Council workflow step.</summary>
public interface ICouncilAutomaticFunctionPolicyService
{
    /// <summary>Resolves the effective automatic function policy from the configured team and step.</summary>
    /// <param name="team">Persisted Council team definition.</param>
    /// <param name="step">Persisted workflow step definition.</param>
    /// <param name="suppressAutomaticFunctions">Whether the current control-flow revision temporarily suppresses automatic functions.</param>
    /// <returns>The normalized effective policy consumed by the provider runtime and visible capability briefing.</returns>
    CouncilAutomaticFunctionPolicyResolution Resolve(
        OrganicCouncilTeamDefinition team,
        CouncilWorkflowStepDefinition step,
        bool suppressAutomaticFunctions = false);
}
