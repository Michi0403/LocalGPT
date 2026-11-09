using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Council;

/// <summary>
/// Mirrors the hard rule of Council role assignment: roles in one distinct-assignment group without fallback must
/// receive unused models, and a paired role reserves a distinct partner for every member. Roles without a group, or
/// with fallback enabled, can reuse models and therefore never block a run.
/// </summary>
/// <param name="logger">Logger used for bounded staffing diagnostics.</param>
public sealed class CouncilTeamStaffingEstimator(ILogger<CouncilTeamStaffingEstimator> logger) : ICouncilTeamStaffingEstimator
{
    /// <inheritdoc />
    public CouncilTeamStaffingEstimate Estimate(OrganicCouncilTeamDefinition team)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(team);
            var aiRoles = team.Roles
                .Where(role => role.HumanParticipationMode != HumanParticipationMode.HumanOnly && !string.IsNullOrWhiteSpace(role.Role))
                .ToList();
            var rolesByName = aiRoles
                .GroupBy(role => role.Role.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var best = new CouncilTeamStaffingEstimate(team.Key, 1, string.Empty, []);
            foreach (var group in aiRoles
                .Where(role => !string.IsNullOrWhiteSpace(role.DistinctAiAssignmentGroup))
                .GroupBy(role => role.DistinctAiAssignmentGroup.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var required = 0;
                var limitingRoles = new List<string>();
                foreach (var role in group)
                {
                    if (role.AllowDistinctAiAssignmentFallback || IsReservedByPartner(role, rolesByName))
                        continue;
                    var multiplier = ReservesPairedPartner(role, rolesByName) ? 2 : 1;
                    required += Math.Max(1, role.MinimumAiParticipants) * multiplier;
                    limitingRoles.Add(role.Role);
                }
                if (required > best.MinimumDistinctModels)
                    best = new CouncilTeamStaffingEstimate(team.Key, required, group.Key, limitingRoles);
            }
            logger.LogDebug("Council team {TeamKey} needs at least {Required} distinct AI model(s) (limiting group {Group}).",
                team.Key, best.MinimumDistinctModels, string.IsNullOrWhiteSpace(best.LimitingGroup) ? "none" : best.LimitingGroup);
            return best;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Estimating the staffing requirement of Council team {TeamKey} failed.", team?.Key);
            throw;
        }
    }

    /// <inheritdoc />
    public bool CanStaff(OrganicCouncilTeamDefinition team, int availableModelCount)
    {
        try
        {
            return Estimate(team).MinimumDistinctModels <= Math.Max(0, availableModelCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking whether Council team {TeamKey} can be staffed failed.", team?.Key);
            throw;
        }
    }

    /// <summary>Determines whether the role reserves a distinct paired partner for each of its members (same rule as role assignment).</summary>
    /// <param name="role">Role definition.</param>
    /// <param name="rolesByName">AI roles of the team by name.</param>
    /// <returns><see langword="true"/> when each member reserves a second distinct model.</returns>
    private bool ReservesPairedPartner(OrganicCouncilRoleDefinition role, IReadOnlyDictionary<string, OrganicCouncilRoleDefinition> rolesByName)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(role.PairedRole) &&
                rolesByName.TryGetValue(role.PairedRole.Trim(), out var partner) &&
                string.Equals(partner.DistinctAiAssignmentGroup?.Trim(), role.DistinctAiAssignmentGroup?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(partner.MatchAiParticipantCountToRole?.Trim(), role.Role.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying the paired-role reservation of role {Role} failed.", role?.Role);
            throw;
        }
    }

    /// <summary>Determines whether the role's members were already reserved by a partner role (avoids double counting).</summary>
    /// <param name="role">Role definition.</param>
    /// <param name="rolesByName">AI roles of the team by name.</param>
    /// <returns><see langword="true"/> when another role of the group reserves this role's models.</returns>
    private bool IsReservedByPartner(OrganicCouncilRoleDefinition role, IReadOnlyDictionary<string, OrganicCouncilRoleDefinition> rolesByName)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(role.MatchAiParticipantCountToRole) &&
                rolesByName.TryGetValue(role.MatchAiParticipantCountToRole.Trim(), out var owner) &&
                string.Equals(owner.PairedRole?.Trim(), role.Role.Trim(), StringComparison.OrdinalIgnoreCase) &&
                ReservesPairedPartner(owner, rolesByName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying the partner reservation of role {Role} failed.", role?.Role);
            throw;
        }
    }
}
