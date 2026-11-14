using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Resolves the persisted user-editable automatic/native function policy for one Council workflow step.</summary>
public interface ICouncilAutomaticFunctionPolicyService
{
    /// <summary>Resolves the effective automatic function policy from the configured team and step.</summary>
    /// <param name="team">Persisted Council team definition.</param>
    /// <param name="step">Persisted workflow step definition.</param>
    /// <param name="suppressAutomaticFunctions">Whether the current control-flow revision temporarily suppresses automatic functions.</param>
    /// <param name="requestedCapabilities">Organic/1-Wire capabilities requested for this run; their serving functions are attached natively in non-strict steps.</param>
    /// <returns>The normalized effective policy consumed by the provider runtime and visible capability briefing.</returns>
    CouncilAutomaticFunctionPolicyResolution Resolve(
        OrganicCouncilTeamDefinition team,
        CouncilWorkflowStepDefinition step,
        bool suppressAutomaticFunctions = false,
        IReadOnlyCollection<string>? requestedCapabilities = null);

    /// <summary>Resolves the native tool exposure for a plain chat session that has no Council team or workflow step.</summary>
    /// <returns>The compact base set plus broker entrypoints, or an unrestricted resolution when the runtime mode is AllPolicyApproved.</returns>
    CouncilAutomaticFunctionPolicyResolution ResolveSessionFunctions();
}
