namespace LocalGPT.BusinessObjects;

/// <summary>Maps one development team's workflow steps onto the coordinated source-file route.</summary>
public sealed class CouncilFileWorkTeamRoute
{
    /// <summary>Council team key.</summary>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>Team step that produces the source manifest.</summary>
    public string PlanStepKey { get; set; } = string.Empty;
    /// <summary>Team step whose role authors the assigned files.</summary>
    public string GenerationStepKey { get; set; } = string.Empty;
    /// <summary>Team step whose role reviews generated files.</summary>
    public string CurationStepKey { get; set; } = string.Empty;
    /// <summary>Team step whose role owns build and artifact verification.</summary>
    public string VerificationStepKey { get; set; } = string.Empty;
    /// <summary>Team step whose function permission authorizes the coordinator's source-workspace calls.</summary>
    public string FunctionPermissionStepKey { get; set; } = string.Empty;
}
