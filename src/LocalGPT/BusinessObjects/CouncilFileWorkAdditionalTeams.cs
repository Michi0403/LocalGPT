namespace LocalGPT.BusinessObjects;

/// <summary>
/// Development teams that may use the coordinated source-file route (plan, file authoring, recovery,
/// build, repair, verification and ZIP) for new projects when the user requested artifacts.
/// Teams not listed keep their own workflow unchanged.
/// </summary>
public sealed class CouncilFileWorkAdditionalTeams
{
    /// <summary>Team routes, each mapping the team's own workflow steps onto the coordinated route.</summary>
    public List<CouncilFileWorkTeamRoute> Teams { get; set; } = [];
}
