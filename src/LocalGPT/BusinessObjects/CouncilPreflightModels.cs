namespace LocalGPT.BusinessObjects;

/// <summary>Database, function, skill, project and hardware readiness report created before every council run.</summary>
public sealed class CouncilPreflightReport
{
    /// <summary>
    /// Gets or sets the checked at UTC associated with this council preflight report state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The checked at UTC value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the regex pattern count that quantifies the associated council preflight report data.
    /// </summary>
    /// <value>The regex pattern count value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public int RegexPatternCount { get; set; }
    /// <summary>
    /// Gets or sets the knowledge entry count that quantifies the associated council preflight report data.
    /// </summary>
    /// <value>The knowledge entry count value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public int KnowledgeEntryCount { get; set; }
    /// <summary>
    /// Gets or sets the project count that quantifies the associated council preflight report data.
    /// </summary>
    /// <value>The project count value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public int ProjectCount { get; set; }
    /// <summary>
    /// Gets or sets the DevExpress function count that quantifies the associated council preflight report data.
    /// </summary>
    /// <value>The DevExpress function count value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public int DxFunctionCount { get; set; }
    /// <summary>
    /// Gets or sets the organic skill count that quantifies the associated council preflight report data.
    /// </summary>
    /// <value>The organic skill count value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public int OrganicSkillCount { get; set; }
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this council preflight report instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public string TeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets the team name value that forms part of the council preflight report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The team name value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public string TeamName { get; set; } = "Organic Project Team";
    /// <summary>
    /// Gets or sets the introduction prompt template value that forms part of the council preflight report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The introduction prompt template value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public string IntroductionPromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the project names collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The project names value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> ProjectNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the function names collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The function names value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> FunctionNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the skill keys collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The skill keys value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> SkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the online skill keys collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The online skill keys value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> OnlineSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the offline skill keys collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The offline skill keys value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> OfflineSkillKeys { get; set; } = [];
    /// <summary>Bounded connected 1-Wire capability contracts taught to every Council member before substantive work.</summary>
    /// <value>The capability teachings value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> CapabilityTeachings { get; set; } = [];
    /// <summary>
    /// Gets or sets the regex names collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The regex names value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> RegexNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the missing requirements collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The missing requirements value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> MissingRequirements { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<string> Warnings { get; set; } = [];
    /// <summary>
    /// Gets or sets the members collection maintained or exposed by this council preflight report instance for downstream processing.
    /// </summary>
    /// <value>The members value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public List<CouncilMemberReadiness> Members { get; set; } = [];
    /// <summary>
    /// Gets or sets the prompt context value that forms part of the council preflight report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt context value exposed by <see cref="CouncilPreflightReport"/>.</value>
    public string PromptContext { get; set; } = string.Empty;
}

/// <summary>Readiness facts for one council member and its selected hardware road.</summary>
public sealed class CouncilMemberReadiness
{
    /// <summary>
    /// Gets or sets the model name value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable lane key used to identify or correlate this council member readiness instance with related application state.
    /// </summary>
    /// <value>The lane key value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public string LaneKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hardware kind value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware kind value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets the hardware index value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware index value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public int HardwareIndex { get; set; } = -1;
    /// <summary>
    /// Gets or sets the hardware name value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware name value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public string HardwareName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the effective load percent value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effective load percent value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public int EffectiveLoadPercent { get; set; } = 30;
    /// <summary>
    /// Gets or sets the effective max output tokens value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effective max output tokens value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public int EffectiveMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the effective max context tokens value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The effective max context tokens value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public int EffectiveMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the council member readiness state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets the assigned DevExpress functions collection maintained or exposed by this council member readiness instance for downstream processing.
    /// </summary>
    /// <value>The assigned DevExpress functions value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public List<string> AssignedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets the assigned organic skills collection maintained or exposed by this council member readiness instance for downstream processing.
    /// </summary>
    /// <value>The assigned organic skills value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public List<string> AssignedOrganicSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets the missing capabilities collection maintained or exposed by this council member readiness instance for downstream processing.
    /// </summary>
    /// <value>The missing capabilities value exposed by <see cref="CouncilMemberReadiness"/>.</value>
    public List<string> MissingCapabilities { get; set; } = [];
}

/// <summary>
/// Represents the outcome of debug artifact inspection, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class DebugArtifactInspectionResult
{
    /// <summary>
    /// Gets or sets the file name used by this debug artifact inspection instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file name value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full path used by this debug artifact inspection instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The full path value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the format value that forms part of the debug artifact inspection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size bytes value that forms part of the debug artifact inspection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The size bytes value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public long SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets the last write UTC associated with this debug artifact inspection state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last write UTC value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public DateTime LastWriteUtc { get; set; }
    /// <summary>
    /// Gets or sets the documents collection maintained or exposed by this debug artifact inspection instance for downstream processing.
    /// </summary>
    /// <value>The documents value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public List<string> Documents { get; set; } = [];
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this debug artifact inspection instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public List<string> Metadata { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this debug artifact inspection instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="DebugArtifactInspectionResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}
