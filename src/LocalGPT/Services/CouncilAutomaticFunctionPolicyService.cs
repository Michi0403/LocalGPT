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
            var strictGameRuntime = IsStrictGameRuntimeTeam(team);
            var strictTeam = mode == CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList ||
                             (strictGameRuntime && mode == CouncilAutomaticFunctionPolicyMode.TeamAllowList);
            var strictStep = mode == CouncilAutomaticFunctionPolicyMode.StrictStepAllowList ||
                             (strictGameRuntime && mode == CouncilAutomaticFunctionPolicyMode.ExactAllowList);

            // Strictness is an explicit workflow choice for ordinary teams. Retain the
            // existing hard boundary for saved games.* runtime Team/Exact modes.
            // No broker is attached to an exclusive list: it would defeat that boundary.
            if (strictTeam || strictStep)
            {
                // Broker entrypoints cannot be present in an exclusive allow-list:
                // exposing dxai.tools.invoke would indirectly grant access to the
                // entire catalog and negate the saved strict policy.
                var allowed = (strictTeam ? teamList : stepList)
                    .Where(name => !IsBrokerTool(name))
                    .ToList();
                var scope = strictTeam ? "team" : "step";
                return new CouncilAutomaticFunctionPolicyResolution(
                    allowed.Count > 0,
                    allowed,
                    allowed.Count > 0
                        ? $"strict {scope} function allow-list: {string.Join(", ", allowed)}"
                        : $"strict {scope} function allow-list is empty; native tools are disabled",
                    IsStrict: true);
            }

            // In normal software, research and learning workflows the saved names are
            // *preferences*, never an execution boundary. Promote them as native Ollama
            // metadata while the normal broker exposes every other registered,
            // policy-approved DXFunction on demand without sending thousands of schemas.
            IReadOnlyList<string> preferred = mode switch
            {
                CouncilAutomaticFunctionPolicyMode.TeamAllowList => teamList,
                CouncilAutomaticFunctionPolicyMode.ExactAllowList => stepList,
                CouncilAutomaticFunctionPolicyMode.AllPolicyApproved => Normalize(stepList.Concat(teamList)),
                _ => []
            };
            if (preferred.Count == 0)
            {
                return new CouncilAutomaticFunctionPolicyResolution(
                    true,
                    null,
                    "all registered policy-approved automatic DXFunctions available as native tools; no preference list");
            }

            var exposed = preferred
                .Concat(["dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new CouncilAutomaticFunctionPolicyResolution(
                true,
                exposed,
                $"preferred native tools: {string.Join(", ", preferred)}; full policy-approved catalog remains available through discovery/preview/invoke");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving Council automatic-function policy failed for team {TeamKey} and step {StepKey}.", team?.Key, step?.Key);
            throw;
        }
    }

    /// <summary>Checks whether a function name is one of the unrestricted DXFunction broker entrypoints.</summary>
    /// <param name="name">Registered function name to inspect.</param>
    /// <returns>Whether that name permits catalog-wide discovery or invocation.</returns>
    private bool IsBrokerTool(string name)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return name.Equals("dxai.tools.discover", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("dxai.tools.preview", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("dxai.tools.invoke", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying Council DXFunction broker entrypoint {FunctionName} failed.", name);
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
