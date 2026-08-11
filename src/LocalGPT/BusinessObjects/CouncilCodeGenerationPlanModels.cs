namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council code generation plan result.
/// </summary>
public sealed class CouncilCodeGenerationPlanResult
{
    /// <summary>
    /// Gets or sets found.
    /// </summary>
    public bool Found { get; set; }
    /// <summary>
    /// Gets or sets payload.
    /// </summary>
    public CodeGenerationReviewPayload Payload { get; set; } = new();
    /// <summary>
    /// Gets or sets source format.
    /// </summary>
    public string SourceFormat { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets warning.
    /// </summary>
    public string Warning { get; set; } = string.Empty;
}
