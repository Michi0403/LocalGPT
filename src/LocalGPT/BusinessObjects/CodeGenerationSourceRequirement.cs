namespace LocalGPT.BusinessObjects;

/// <summary>Run-bound requirement-to-source evidence; presence is never a compiler or behavioral verification receipt.</summary>
public sealed class CodeGenerationSourceRequirement
{
    /// <summary>Stable goal or human-decision requirement identifier.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Original user goal or exact human request identity.</summary>
    public string Origin { get; set; } = string.Empty;
    /// <summary>Verbatim user choice or concise requirement, without model reinterpretation.</summary>
    public string Requirement { get; set; } = string.Empty;
    /// <summary>Manifest paths declaring the work needed for this obligation.</summary>
    public List<string> ManifestPaths { get; set; } = [];
    /// <summary>Subsection of mapped paths for which actual reviewed candidate source bytes exist.</summary>
    public List<string> SourcePaths { get; set; } = [];
    /// <summary>ManifestMissing, ManifestDeclared, SourcePresent or NeedsSemanticVerification; never BuildPassed.</summary>
    public string EvidenceState { get; set; } = string.Empty;
}
