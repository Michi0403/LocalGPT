namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council DevExpress function policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilDxFunctionPolicy
{
    /// <summary>
    /// Gets or sets the maximum calls per step value that forms part of the council DevExpress function policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum calls per step value exposed by <see cref="CouncilDxFunctionPolicy"/>.</value>
    public int MaximumCallsPerStep { get; set; }
    /// <summary>
    /// Gets or sets the maximum parameter characters value that forms part of the council DevExpress function policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum parameter characters value exposed by <see cref="CouncilDxFunctionPolicy"/>.</value>
    public int MaximumParameterCharacters { get; set; }
    /// <summary>
    /// Gets or sets the maximum result characters value that forms part of the council DevExpress function policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum result characters value exposed by <see cref="CouncilDxFunctionPolicy"/>.</value>
    public int MaximumResultCharacters { get; set; }
    /// <summary>
    /// Gets or sets the prompt instruction value that forms part of the council DevExpress function policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt instruction value exposed by <see cref="CouncilDxFunctionPolicy"/>.</value>
    public string PromptInstruction { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for council DevExpress function call, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class CouncilDxFunctionCallRequest
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the council DevExpress function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="CouncilDxFunctionCallRequest"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters value that forms part of the council DevExpress function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="CouncilDxFunctionCallRequest"/>.</value>
    public System.Text.Json.JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets the reason value that forms part of the council DevExpress function call state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="CouncilDxFunctionCallRequest"/>.</value>
    public string Reason { get; set; } = string.Empty;
}
