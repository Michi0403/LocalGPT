namespace LocalGPT.BusinessObjects;

/// <summary>Database, function, skill, project and hardware readiness report created before every council run.</summary>
public sealed class CouncilPreflightReport
{
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public int RegexPatternCount { get; set; }
    public int KnowledgeEntryCount { get; set; }
    public int ProjectCount { get; set; }
    public int DxFunctionCount { get; set; }
    public int OrganicSkillCount { get; set; }
    public string TeamKey { get; set; } = "general";
    public string TeamName { get; set; } = "Organic Project Team";
    public string IntroductionPromptTemplate { get; set; } = string.Empty;
    public List<string> ProjectNames { get; set; } = [];
    public List<string> FunctionNames { get; set; } = [];
    public List<string> SkillKeys { get; set; } = [];
    public List<string> RegexNames { get; set; } = [];
    public List<string> MissingRequirements { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<CouncilMemberReadiness> Members { get; set; } = [];
    public string PromptContext { get; set; } = string.Empty;
}

/// <summary>Readiness facts for one council member and its selected hardware road.</summary>
public sealed class CouncilMemberReadiness
{
    public string ModelName { get; set; } = string.Empty;
    public string LaneKey { get; set; } = string.Empty;
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    public int HardwareIndex { get; set; } = -1;
    public string HardwareName { get; set; } = string.Empty;
    public int EffectiveLoadPercent { get; set; } = 30;
    public int EffectiveMaxOutputTokens { get; set; }
    public int EffectiveMaxContextTokens { get; set; }
    public int? OllamaNumGpu { get; set; }
    public List<string> AssignedDxFunctions { get; set; } = [];
    public List<string> AssignedOrganicSkills { get; set; } = [];
    public List<string> MissingCapabilities { get; set; } = [];
}

public sealed class DebugArtifactInspectionResult
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastWriteUtc { get; set; }
    public List<string> Documents { get; set; } = [];
    public List<string> Metadata { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
