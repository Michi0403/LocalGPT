namespace LocalGPT.BusinessObjects;

public sealed class CouncilDxFunctionPolicy
{
    public int MaximumCallsPerStep { get; set; }
    public int MaximumParameterCharacters { get; set; }
    public int MaximumResultCharacters { get; set; }
    public string PromptInstruction { get; set; } = string.Empty;
}

public sealed class CouncilDxFunctionCallRequest
{
    public string FunctionName { get; set; } = string.Empty;
    public System.Text.Json.JsonElement Parameters { get; set; }
    public string Reason { get; set; } = string.Empty;
}
