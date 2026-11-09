namespace LocalGPT.BusinessObjects;

/// <summary>
/// Conservative lower bound of distinct AI models a saved Council team needs before its role assignment can start.
/// The Council's own role assignment stays authoritative; this estimate only prevents selecting a team that is
/// certain to fail with the models available to a run.
/// </summary>
/// <param name="TeamKey">Stable key of the estimated team.</param>
/// <param name="MinimumDistinctModels">Distinct AI models required by the most demanding distinct-assignment group (at least 1).</param>
/// <param name="LimitingGroup">Distinct-assignment group that sets the requirement; empty when no group constrains the team.</param>
/// <param name="LimitingRoles">Roles of the limiting group that reserve distinct models without fallback.</param>
public sealed record CouncilTeamStaffingEstimate(
    string TeamKey,
    int MinimumDistinctModels,
    string LimitingGroup,
    IReadOnlyList<string> LimitingRoles);
