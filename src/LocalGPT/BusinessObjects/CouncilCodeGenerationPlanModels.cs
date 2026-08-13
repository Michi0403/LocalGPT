namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the outcome of council code generation plan, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class CouncilCodeGenerationPlanResult
{
    /// <summary>
    /// Gets or sets a value indicating whether found applies to the council code generation plan state.
    /// </summary>
    /// <value>The found value exposed by <see cref="CouncilCodeGenerationPlanResult"/>.</value>
    public bool Found { get; set; }
    /// <summary>
    /// Gets or sets the payload value that forms part of the council code generation plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload value exposed by <see cref="CouncilCodeGenerationPlanResult"/>.</value>
    public CodeGenerationReviewPayload Payload { get; set; } = new();
    /// <summary>
    /// Gets or sets the source format value that forms part of the council code generation plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source format value exposed by <see cref="CouncilCodeGenerationPlanResult"/>.</value>
    public string SourceFormat { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the warning value that forms part of the council code generation plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The warning value exposed by <see cref="CouncilCodeGenerationPlanResult"/>.</value>
    public string Warning { get; set; } = string.Empty;
}
