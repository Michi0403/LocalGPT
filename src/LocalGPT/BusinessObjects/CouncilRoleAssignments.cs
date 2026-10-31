namespace LocalGPT.BusinessObjects;

/// <summary>Queryable team role index migrated from the authoritative RolesJson configurations.</summary>
public sealed class CouncilRoleIndex
{
    public string TeamKey { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Responsibility { get; set; } = string.Empty;
    public CouncilRoleAiSelectionMode SelectionMode { get; set; }
}

/// <summary>Exact provider-qualified model membership of a Council team role.</summary>
public sealed class CouncilRoleModelBinding
{
    public string TeamKey { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ModelKey { get; set; } = string.Empty;
}

public sealed record CouncilRoleAssignmentChoice(string TeamKey, string TeamName, string RoleName,
    CouncilRoleAiSelectionMode SelectionMode, bool Assigned, bool IsSystemSeed, bool IsEnabled);

public sealed class CouncilRoleAssignmentRequest
{
    public string TeamKey { get; set; } = string.Empty;
    public string ModelKey { get; set; } = string.Empty;
    public List<string> RoleNames { get; set; } = [];
    public bool RestrictSelectedRolesToAssignedPool { get; set; }
    public bool UserConfirmed { get; set; }
}
