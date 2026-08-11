namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council DevExpress function policy.
/// </summary>
public sealed class CouncilDxFunctionPolicy
{
    /// <summary>
    /// Gets or sets maximum calls per step.
    /// </summary>
    public int MaximumCallsPerStep { get; set; }
    /// <summary>
    /// Gets or sets maximum parameter characters.
    /// </summary>
    public int MaximumParameterCharacters { get; set; }
    /// <summary>
    /// Gets or sets maximum result characters.
    /// </summary>
    public int MaximumResultCharacters { get; set; }
    /// <summary>
    /// Gets or sets prompt instruction.
    /// </summary>
    public string PromptInstruction { get; set; } = string.Empty;
}

/// <summary>
/// Represents a council DevExpress function call request.
/// </summary>
public sealed class CouncilDxFunctionCallRequest
{
    /// <summary>
    /// Gets or sets function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public System.Text.Json.JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
