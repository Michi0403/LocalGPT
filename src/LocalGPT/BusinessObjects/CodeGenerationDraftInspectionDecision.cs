namespace LocalGPT.BusinessObjects;

/// <summary>Independent inspection decision. A model opinion is not a compiler receipt.</summary>
public sealed class CodeGenerationDraftInspectionDecision
{
    /// <summary>Exact candidate hash observed by the reviewer.</summary>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>Whether this candidate passed independent inspection.</summary>
    public bool? Accepted { get; set; }
    /// <summary>Specific defect or reason for the decision.</summary>
    public string Diagnostic { get; set; } = string.Empty;
    /// <summary>Actual reviewing route, supplied by the coordinator rather than model output.</summary>
    public string Reviewer { get; set; } = string.Empty;
    /// <summary>File path associated with the inspected content.</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>Dependency versions associated with the decision.</summary>
    public Dictionary<string, string> DependencyHashes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
