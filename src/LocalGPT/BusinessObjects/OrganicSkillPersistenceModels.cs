using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

public sealed class OrganicSkillDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(240)] public string SourcePeerId { get; set; } = "localgpt";
    [Column(TypeName = "TEXT")] public string OrgansJson { get; set; } = "[]";
    [Column(TypeName = "TEXT")] public string CapabilityKeysJson { get; set; } = "[]";
    [Column(TypeName = "TEXT")] public string UiActivationKeysJson { get; set; } = "[]";
    public bool IsOnline { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsUserApproved { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ProjectOrganicSkillLink> ProjectLinks { get; set; } = [];
    public ICollection<CouncilMemberOrganicSkillLink> MemberLinks { get; set; } = [];
}

public sealed class ProjectOrganicSkillLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public LocalGptProject? Project { get; set; }
    public Guid SkillId { get; set; }
    public OrganicSkillDefinition? Skill { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CouncilMemberOrganicSkillLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(240)] public string MemberKey { get; set; } = string.Empty;
    public Guid SkillId { get; set; }
    public OrganicSkillDefinition? Skill { get; set; }
    public int Proficiency { get; set; } = 50;
    public bool IsSelfRevealed { get; set; }
    public bool IsEnabled { get; set; } = true;
    [MaxLength(4000)] public string Evidence { get; set; } = string.Empty;
    [Column(TypeName = "TEXT")] public string DxFunctionsJson { get; set; } = "[]";
    [Column(TypeName = "TEXT")] public string ControllerMethodsJson { get; set; } = "[]";
    [Column(TypeName = "TEXT")] public string OrganicCapabilitiesJson { get; set; } = "[]";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SaveOrganicSkillRequest
{
    public Guid? Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourcePeerId { get; set; } = "localgpt";
    public List<string> Organs { get; set; } = [];
    public List<string> CapabilityKeys { get; set; } = [];
    public List<string> UiActivationKeys { get; set; } = [];
    public bool IsOnline { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool UserConfirmed { get; set; }
}

public sealed class LinkProjectOrganicSkillRequest
{
    public Guid ProjectId { get; set; }
    public Guid SkillId { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
}

public sealed class ReportCouncilMemberSkillRequest
{
    public string MemberKey { get; set; } = string.Empty;
    public Guid SkillId { get; set; }
    public int Proficiency { get; set; } = 50;
    public bool IsSelfRevealed { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public string Evidence { get; set; } = string.Empty;
    public List<string> DxFunctions { get; set; } = [];
    public List<string> ControllerMethods { get; set; } = [];
    public List<string> OrganicCapabilities { get; set; } = [];
    public bool UserConfirmed { get; set; }
}
