namespace LocalGPT.BusinessObjects;

public sealed class CouncilCodeGenerationPlanResult
{
    public bool Found { get; set; }
    public CodeGenerationReviewPayload Payload { get; set; } = new();
    public string SourceFormat { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}
