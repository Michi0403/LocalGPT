namespace LocalGPT.BusinessObjects;

/// <summary>Durable ownership and receipt for one candidate source-file authoring attempt.</summary>
public sealed class CodeGenerationDraftAssignment
{
    /// <summary>Identifies this exact attempt; replacements receive a new identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identifies the draft review that owns the candidate bytes.</summary>
    public Guid DraftId { get; set; }
    /// <summary>Canonical relative file path reserved for this attempt.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Provider-qualified model route selected for this attempt.</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>One-based attempt number for this path in the current draft.</summary>
    public int Attempt { get; set; }
    /// <summary>Hash of accepted source bytes; empty until completion is committed.</summary>
    public string Sha256 { get; set; } = string.Empty;
    /// <summary>Failure or cancellation evidence, not a model-wide competence score.</summary>
    public string Diagnostic { get; set; } = string.Empty;
    /// <summary>Actual isolated execution receipt used to import this candidate, when guest authoring was selected.</summary>
    public Guid? GuestRunId { get; set; }
    /// <summary>Hash of the exact executed guest script, if any.</summary>
    public string GuestSourceSha256 { get; set; } = string.Empty;
    /// <summary>Hashes of the direct dependency versions supplied to this author.</summary>
    public Dictionary<string, string> DependencyHashes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
