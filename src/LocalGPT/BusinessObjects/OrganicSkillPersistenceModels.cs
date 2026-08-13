using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an organic skill definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicSkillDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this organic skill definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this organic skill definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [Required, MaxLength(200)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the organic skill definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [Required, MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the organic skill definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this organic skill definition instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [MaxLength(240)] public string SourcePeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets the organs JSON value that forms part of the organic skill definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organs JSON value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [Column(TypeName = "TEXT")] public string OrgansJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the capability keys JSON value that forms part of the organic skill definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The capability keys JSON value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [Column(TypeName = "TEXT")] public string CapabilityKeysJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the UI activation keys JSON value that forms part of the organic skill definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The UI activation keys JSON value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    [Column(TypeName = "TEXT")] public string UiActivationKeysJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets a value indicating whether online applies to the organic skill definition state.
    /// </summary>
    /// <value>The is online value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the organic skill definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the organic skill definition state.
    /// </summary>
    /// <value>The is user approved value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public bool IsUserApproved { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this organic skill definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this organic skill definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the project links collection maintained or exposed by this organic skill definition instance for downstream processing.
    /// </summary>
    /// <value>The project links value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public ICollection<ProjectOrganicSkillLink> ProjectLinks { get; set; } = [];
    /// <summary>
    /// Gets or sets the member links collection maintained or exposed by this organic skill definition instance for downstream processing.
    /// </summary>
    /// <value>The member links value exposed by <see cref="OrganicSkillDefinition"/>.</value>
    public ICollection<CouncilMemberOrganicSkillLink> MemberLinks { get; set; } = [];
}

/// <summary>
/// Represents a project organic skill link application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectOrganicSkillLink
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this project organic skill link instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project organic skill link instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the project value that forms part of the project organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>
    /// Gets or sets the stable skill identifier used to identify or correlate this project organic skill link instance with related application state.
    /// </summary>
    /// <value>The skill identifier value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets the skill value that forms part of the project organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The skill value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public OrganicSkillDefinition? Skill { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether required applies to the project organic skill link state.
    /// </summary>
    /// <value>The is required value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public bool IsRequired { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the project organic skill link state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the notes value that forms part of the project organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this project organic skill link state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="ProjectOrganicSkillLink"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a council member organic skill link application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilMemberOrganicSkillLink
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council member organic skill link instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable member key used to identify or correlate this council member organic skill link instance with related application state.
    /// </summary>
    /// <value>The member key value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    [Required, MaxLength(240)] public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable skill identifier used to identify or correlate this council member organic skill link instance with related application state.
    /// </summary>
    /// <value>The skill identifier value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets the skill value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The skill value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public OrganicSkillDefinition? Skill { get; set; }
    /// <summary>
    /// Gets or sets the proficiency value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proficiency value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public int Proficiency { get; set; } = 50;
    /// <summary>
    /// Gets or sets a value indicating whether self revealed applies to the council member organic skill link state.
    /// </summary>
    /// <value>The is self revealed value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public bool IsSelfRevealed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council member organic skill link state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the evidence value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evidence value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    [MaxLength(4000)] public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the DevExpress functions JSON value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress functions JSON value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    [Column(TypeName = "TEXT")] public string DxFunctionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the controller methods JSON value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller methods JSON value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    [Column(TypeName = "TEXT")] public string ControllerMethodsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the organic capabilities JSON value that forms part of the council member organic skill link state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organic capabilities JSON value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    [Column(TypeName = "TEXT")] public string OrganicCapabilitiesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council member organic skill link state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilMemberOrganicSkillLink"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the input contract for save organic skill, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveOrganicSkillRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this save organic skill instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this save organic skill instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the save organic skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the save organic skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this save organic skill instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public string SourcePeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this save organic skill instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets the capability keys collection maintained or exposed by this save organic skill instance for downstream processing.
    /// </summary>
    /// <value>The capability keys value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI activation keys collection maintained or exposed by this save organic skill instance for downstream processing.
    /// </summary>
    /// <value>The UI activation keys value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether online applies to the save organic skill state.
    /// </summary>
    /// <value>The is online value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the save organic skill state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save organic skill state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveOrganicSkillRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for link project organic skill, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LinkProjectOrganicSkillRequest
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this link project organic skill instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable skill identifier used to identify or correlate this link project organic skill instance with related application state.
    /// </summary>
    /// <value>The skill identifier value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether required applies to the link project organic skill state.
    /// </summary>
    /// <value>The is required value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public bool IsRequired { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the link project organic skill state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the notes value that forms part of the link project organic skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the link project organic skill state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="LinkProjectOrganicSkillRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents the input contract for report council member skill, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class ReportCouncilMemberSkillRequest
{
    /// <summary>
    /// Gets or sets the stable member key used to identify or correlate this report council member skill instance with related application state.
    /// </summary>
    /// <value>The member key value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable skill identifier used to identify or correlate this report council member skill instance with related application state.
    /// </summary>
    /// <value>The skill identifier value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public Guid SkillId { get; set; }
    /// <summary>
    /// Gets or sets the proficiency value that forms part of the report council member skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proficiency value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public int Proficiency { get; set; } = 50;
    /// <summary>
    /// Gets or sets a value indicating whether self revealed applies to the report council member skill state.
    /// </summary>
    /// <value>The is self revealed value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public bool IsSelfRevealed { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the report council member skill state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the evidence value that forms part of the report council member skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evidence value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the DevExpress functions collection maintained or exposed by this report council member skill instance for downstream processing.
    /// </summary>
    /// <value>The DevExpress functions value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public List<string> DxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets the controller methods collection maintained or exposed by this report council member skill instance for downstream processing.
    /// </summary>
    /// <value>The controller methods value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public List<string> ControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets the organic capabilities collection maintained or exposed by this report council member skill instance for downstream processing.
    /// </summary>
    /// <value>The organic capabilities value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public List<string> OrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the report council member skill state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="ReportCouncilMemberSkillRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}
