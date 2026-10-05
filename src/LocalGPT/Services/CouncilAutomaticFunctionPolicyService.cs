using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Resolves user-owned Council automatic/native function policy without embedding team-specific function lists in runtime orchestration code.</summary>
/// <param name="logger">Logger used for bounded policy diagnostics.</param>
public sealed class CouncilAutomaticFunctionPolicyService(
    ILogger<CouncilAutomaticFunctionPolicyService> logger) : ICouncilAutomaticFunctionPolicyService
{
    /// <summary>Resolves the effective automatic/native function exposure for one configured workflow step from persisted team and step policy.</summary>
    /// <param name="team">User-editable Council team that owns the team-level allow-list.</param>
    /// <param name="step">Workflow step whose configured policy mode is being evaluated.</param>
    /// <param name="suppressAutomaticFunctions">Forces automatic functions off for execution paths that require tool isolation.</param>
    /// <returns>The effective enabled state, optional exact allow-list, and user-visible policy description.</returns>
    public CouncilAutomaticFunctionPolicyResolution Resolve(
        OrganicCouncilTeamDefinition team,
        CouncilWorkflowStepDefinition step,
        bool suppressAutomaticFunctions = false)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(team);
            ArgumentNullException.ThrowIfNull(step);

            if (suppressAutomaticFunctions || !step.CanUseOrganicFunctions)
            {
                return new CouncilAutomaticFunctionPolicyResolution(
                    false,
                    [],
                    suppressAutomaticFunctions
                        ? "disabled for this workflow revision"
                        : "disabled by the workflow step");
            }

            var mode = step.AutomaticFunctionPolicyMode;
            if (mode == CouncilAutomaticFunctionPolicyMode.Legacy)
            {
                mode = step.AllowedAutomaticFunctions is { Count: > 0 }
                    ? CouncilAutomaticFunctionPolicyMode.ExactAllowList
                    : CouncilAutomaticFunctionPolicyMode.AllPolicyApproved;
            }

            if (mode == CouncilAutomaticFunctionPolicyMode.Disabled)
                return new CouncilAutomaticFunctionPolicyResolution(false, [], "disabled by the workflow step");

            var teamList = Normalize(team.AllowedAutomaticFunctions);
            var stepList = Normalize(step.AllowedAutomaticFunctions);
            if (!IsStrictGameRuntimeTeam(team))
            {
                var preferred = mode == CouncilAutomaticFunctionPolicyMode.TeamAllowList
                    ? teamList
                    : mode == CouncilAutomaticFunctionPolicyMode.ExactAllowList
                        ? stepList
                        : [];
                var description = preferred.Count > 0
                    ? $"available from the complete registered policy-approved catalog; configured names are preferred guidance outside game runtimes: {string.Join(", ", preferred)}"
                    : "available from the complete registered policy-approved catalog";
                return new CouncilAutomaticFunctionPolicyResolution(true, null, description);
            }

            return mode switch
            {
                CouncilAutomaticFunctionPolicyMode.TeamAllowList => new(
                    teamList.Count > 0,
                    teamList,
                    teamList.Count > 0
                        ? $"game runtime restricted to the team allow-list: {string.Join(", ", teamList)}"
                        : "game runtime disabled because the selected team allow-list is empty"),
                CouncilAutomaticFunctionPolicyMode.ExactAllowList => new(
                    stepList.Count > 0,
                    stepList,
                    stepList.Count > 0
                        ? $"game runtime restricted to this step's exact allow-list: {string.Join(", ", stepList)}"
                        : "game runtime disabled because this step's exact allow-list is empty"),
                CouncilAutomaticFunctionPolicyMode.AllPolicyApproved => new(
                    true,
                    null,
                    "game runtime explicitly allows the complete registered policy-approved catalog"),
                _ => new(false, [], "disabled because the configured policy mode is unsupported")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving Council automatic-function policy failed for team {TeamKey} and step {StepKey}.", team?.Key, step?.Key);
            throw;
        }
    }

    /// <summary>Determines whether the selected team owns a live game runtime and therefore keeps hard tool allow-list semantics.</summary>
    /// <param name="team">Council team whose role runtime classes are inspected.</param>
    /// <returns><see langword="true"/> when at least one role is bound to a <c>games.*</c> runtime class.</returns>
    private bool IsStrictGameRuntimeTeam(OrganicCouncilTeamDefinition team)
    {
        try
        {
            return team.Roles
                .SelectMany(role => role.RuntimeClassKeys ?? [])
                .Any(runtimeClassKey =>
                    !string.IsNullOrWhiteSpace(runtimeClassKey) &&
                    runtimeClassKey.Trim().StartsWith("games.", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving strict game-runtime tool policy failed for team {TeamKey}.", team.Key);
            throw;
        }
    }

    /// <summary>Normalizes one user-edited function list while preserving canonical registered names.</summary>
    /// <param name="values">Function names stored by the team or workflow-step configuration.</param>
    /// <returns>A trimmed, case-insensitively distinct and deterministically ordered function-name list.</returns>
    private IReadOnlyList<string> Normalize(IEnumerable<string>? values)
    {
        try
        {
            return (values ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a Council automatic-function allow-list failed.");
            throw;
        }
    }
}
