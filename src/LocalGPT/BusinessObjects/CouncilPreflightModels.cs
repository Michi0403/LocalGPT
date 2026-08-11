namespace LocalGPT.BusinessObjects;

/// <summary>Database, function, skill, project and hardware readiness report created before every council run.</summary>
public sealed class CouncilPreflightReport
{
    /// <summary>
    /// Gets or sets checked at UTC.
    /// </summary>
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets regex pattern count.
    /// </summary>
    public int RegexPatternCount { get; set; }
    /// <summary>
    /// Gets or sets knowledge entry count.
    /// </summary>
    public int KnowledgeEntryCount { get; set; }
    /// <summary>
    /// Gets or sets project count.
    /// </summary>
    public int ProjectCount { get; set; }
    /// <summary>
    /// Gets or sets DevExpress function count.
    /// </summary>
    public int DxFunctionCount { get; set; }
    /// <summary>
    /// Gets or sets organic skill count.
    /// </summary>
    public int OrganicSkillCount { get; set; }
    /// <summary>
    /// Gets or sets team key.
    /// </summary>
    public string TeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets team name.
    /// </summary>
    public string TeamName { get; set; } = "Organic Project Team";
    /// <summary>
    /// Gets or sets introduction prompt template.
    /// </summary>
    public string IntroductionPromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets project names.
    /// </summary>
    public List<string> ProjectNames { get; set; } = [];
    /// <summary>
    /// Gets or sets function names.
    /// </summary>
    public List<string> FunctionNames { get; set; } = [];
    /// <summary>
    /// Gets or sets skill keys.
    /// </summary>
    public List<string> SkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets online skill keys.
    /// </summary>
    public List<string> OnlineSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets offline skill keys.
    /// </summary>
    public List<string> OfflineSkillKeys { get; set; } = [];
    /// <summary>Bounded connected 1-Wire capability contracts taught to every Council member before substantive work.</summary>
    public List<string> CapabilityTeachings { get; set; } = [];
    /// <summary>
    /// Gets or sets regex names.
    /// </summary>
    public List<string> RegexNames { get; set; } = [];
    /// <summary>
    /// Gets or sets missing requirements.
    /// </summary>
    public List<string> MissingRequirements { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
    /// <summary>
    /// Gets or sets members.
    /// </summary>
    public List<CouncilMemberReadiness> Members { get; set; } = [];
    /// <summary>
    /// Gets or sets prompt context.
    /// </summary>
    public string PromptContext { get; set; } = string.Empty;
}

/// <summary>Readiness facts for one council member and its selected hardware road.</summary>
public sealed class CouncilMemberReadiness
{
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets lane key.
    /// </summary>
    public string LaneKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets hardware kind.
    /// </summary>
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets hardware index.
    /// </summary>
    public int HardwareIndex { get; set; } = -1;
    /// <summary>
    /// Gets or sets hardware name.
    /// </summary>
    public string HardwareName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets effective load percent.
    /// </summary>
    public int EffectiveLoadPercent { get; set; } = 30;
    /// <summary>
    /// Gets or sets effective max output tokens.
    /// </summary>
    public int EffectiveMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets effective max context tokens.
    /// </summary>
    public int EffectiveMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets ollama num gpu.
    /// </summary>
    public int? OllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets assigned DevExpress functions.
    /// </summary>
    public List<string> AssignedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets assigned organic skills.
    /// </summary>
    public List<string> AssignedOrganicSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets missing capabilities.
    /// </summary>
    public List<string> MissingCapabilities { get; set; } = [];
}

/// <summary>
/// Represents a debug artifact inspection result.
/// </summary>
public sealed class DebugArtifactInspectionResult
{
    /// <summary>
    /// Gets or sets file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets full path.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets size bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets last write UTC.
    /// </summary>
    public DateTime LastWriteUtc { get; set; }
    /// <summary>
    /// Gets or sets documents.
    /// </summary>
    public List<string> Documents { get; set; } = [];
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public List<string> Metadata { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
