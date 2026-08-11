using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an organic skill definition.
/// </summary>
public sealed class OrganicSkillDefinition
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    [Required, MaxLength(200)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    [Required, MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    [MaxLength(240)] public string SourcePeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets organs JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string OrgansJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets capability keys JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string CapabilityKeysJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets UI activation keys JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string UiActivationKeysJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets is online.
    /// </summary>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets project links.
    /// </summary>
    public ICollection<ProjectOrganicSkillLink> ProjectLinks { get; set; } = [];
    /// <summary>
    /// Gets or sets member links.
    /// </summary>
    public ICollection<CouncilMemberOrganicSkillLink> MemberLinks { get; set; } = [];
}

/// <summary>
/// Represents a project organic skill link.
/// </summary>
public sealed class ProjectOrganicSkillLink
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets skill identifier.
    /// </summary>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets skill.
    /// </summary>
    public OrganicSkillDefinition? Skill { get; set; }
    /// <summary>
    /// Gets or sets is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a council member organic skill link.
/// </summary>
public sealed class CouncilMemberOrganicSkillLink
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets member key.
    /// </summary>
    [Required, MaxLength(240)] public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets skill identifier.
    /// </summary>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets skill.
    /// </summary>
    public OrganicSkillDefinition? Skill { get; set; }
    /// <summary>
    /// Gets or sets proficiency.
    /// </summary>
    public int Proficiency { get; set; } = 50;
    /// <summary>
    /// Gets or sets is self revealed.
    /// </summary>
    public bool IsSelfRevealed { get; set; }
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets evidence.
    /// </summary>
    [MaxLength(4000)] public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets DevExpress functions JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string DxFunctionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets controller methods JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string ControllerMethodsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets organic capabilities JSON.
    /// </summary>
    [Column(TypeName = "TEXT")] public string OrganicCapabilitiesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a save organic skill request.
/// </summary>
public sealed class SaveOrganicSkillRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    public string SourcePeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets organs.
    /// </summary>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets capability keys.
    /// </summary>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets UI activation keys.
    /// </summary>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets is online.
    /// </summary>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a link project organic skill request.
/// </summary>
public sealed class LinkProjectOrganicSkillRequest
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets skill identifier.
    /// </summary>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a report council member skill request.
/// </summary>
public sealed class ReportCouncilMemberSkillRequest
{
    /// <summary>
    /// Gets or sets member key.
    /// </summary>
    public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets skill identifier.
    /// </summary>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets proficiency.
    /// </summary>
    public int Proficiency { get; set; } = 50;
    /// <summary>
    /// Gets or sets is self revealed.
    /// </summary>
    public bool IsSelfRevealed { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets evidence.
    /// </summary>
    public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets DevExpress functions.
    /// </summary>
    public List<string> DxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets controller methods.
    /// </summary>
    public List<string> ControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets organic capabilities.
    /// </summary>
    public List<string> OrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}
