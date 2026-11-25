using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Resolves which Council team uses the coordinated source-file route and with which of its own steps.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>
    /// Returns the coordinated file-work policy for a team. The Program Compiler team and unlisted teams get the
    /// stored policy unchanged; a team listed in <c>CouncilFileWorkAdditionalTeamsJson</c> gets a copy whose
    /// step keys are that team's own steps.
    /// </summary>
    /// <param name="policy">Stored <c>CouncilFileWorkRuntimeParametersJson</c> value, read by the caller.</param>
    /// <param name="teamKey">Council team key of the current run.</param>
    /// <param name="additionalTeam">Set when the team is a listed development team rather than the policy's own team.</param>
    /// <returns>The policy to use for this team.</returns>
    private CouncilFileWorkRuntimeParameters ResolveFileWorkPolicy(CouncilFileWorkRuntimeParameters policy, string? teamKey, out bool additionalTeam)
    {
        try
        {
            additionalTeam = false;
            if (!policy.Enabled || string.IsNullOrWhiteSpace(teamKey) ||
                string.Equals(teamKey, policy.TeamKey, StringComparison.OrdinalIgnoreCase))
                return policy;
            CouncilFileWorkTeamRoute? route;
            try
            {
                route = runtimePolicy.GetJson<CouncilFileWorkAdditionalTeams>("CouncilFileWorkAdditionalTeamsJson").Teams?
                    .FirstOrDefault(item => string.Equals(item.TeamKey, teamKey, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A missing or unreadable list keeps every development team on its own workflow.
                logger.LogWarning(exception, "CouncilFileWorkAdditionalTeamsJson could not be read; team {TeamKey} keeps its own workflow.", teamKey);
                return policy;
            }
            if (route is null || string.IsNullOrWhiteSpace(route.PlanStepKey) || string.IsNullOrWhiteSpace(route.GenerationStepKey) ||
                string.IsNullOrWhiteSpace(route.VerificationStepKey))
                return policy;
            // Copy so the shared runtime value is never changed for other teams or runs.
            var copy = JsonSerializer.Deserialize<CouncilFileWorkRuntimeParameters>(JsonSerializer.Serialize(policy))!;
            copy.TeamKey = route.TeamKey;
            copy.PlanStepKey = route.PlanStepKey;
            copy.GenerationStepKey = route.GenerationStepKey;
            copy.CurationStepKey = string.IsNullOrWhiteSpace(route.CurationStepKey) ? route.VerificationStepKey : route.CurationStepKey;
            copy.VerificationStepKey = route.VerificationStepKey;
            copy.FunctionPermissionStepKey = route.FunctionPermissionStepKey ?? string.Empty;
            copy.PreparationStepKeys = [];
            additionalTeam = true;
            return copy;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to resolve the coordinated file-work policy for team {TeamKey}.", teamKey);
            throw;
        }
    }

    /// <summary>
    /// Checks, without side effects, whether a listed development team may take the coordinated route for this run.
    /// A team that may not keeps running its own workflow instead of ending with a blocked-route notice.
    /// </summary>
    /// <param name="request">Current Council request.</param>
    /// <param name="team">Configured team.</param>
    /// <param name="steps">Enabled workflow steps of the team.</param>
    /// <param name="policy">Team-resolved file-work policy.</param>
    /// <returns><see langword="true"/> when artifacts are requested for a new project and the team's roles and function permissions allow the route.</returns>
    private bool CanUseAdditionalTeamFileWork(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team,
        IReadOnlyList<CouncilWorkflowStepDefinition> steps, CouncilFileWorkRuntimeParameters policy)
    {
        try
        {
            if (!request.GenerateImplementationArtifact || request.AttachedWorkspaceNames.Count > 0 ||
                (request.ProjectId is not null && !request.CreateProjectForRun))
                return false;
            var routeKeys = new[] { policy.PlanStepKey, policy.GenerationStepKey, policy.CurationStepKey, policy.VerificationStepKey };
            var routeRoles = steps.Where(step => routeKeys.Contains(step.Key, StringComparer.OrdinalIgnoreCase)).Select(step => step.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (team.Roles.Any(role => routeRoles.Contains(role.Role) &&
                    role.HumanParticipationMode is HumanParticipationMode.Required or HumanParticipationMode.HumanOnly))
                return false;
            var permissionStep = steps.FirstOrDefault(step => string.Equals(step.Key,
                string.IsNullOrWhiteSpace(policy.FunctionPermissionStepKey) ? policy.VerificationStepKey : policy.FunctionPermissionStepKey,
                StringComparison.OrdinalIgnoreCase));
            if (permissionStep is null) return false;
            var permission = councilAutomaticFunctionPolicy.Resolve(team, permissionStep, false, request.RequestedOrganicCapabilities);
            return permission.Enabled && (!permission.IsStrict || permission.AutomaticFunctionAllowList is not { } allowed ||
                (allowed.Contains("codegen.review.create") && allowed.Contains("codegen.review.execute") &&
                 allowed.Contains("council.development.artifact.verify")));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to check the coordinated file-work route for team {TeamKey}.", team.Key);
            throw;
        }
    }
}
