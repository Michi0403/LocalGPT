namespace LocalGPT.BusinessObjects;

/// <summary>Request used to build an advisory processing plan for one quarantined upload workspace.</summary>
public sealed class UploadProcessingRecommendationRequest
{
    public string WorkspaceName { get; set; } = string.Empty;
    public string UserGoal { get; set; } = string.Empty;
}

/// <summary>One user-reviewable recommendation generated from quarantine evidence, approved knowledge and available Council teams.</summary>
public sealed class UploadProcessingRecommendation
{
    public string WorkspaceName { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Source { get; set; } = "DeterministicFallback";
    public int Confidence { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string ProcessingMode { get; set; } = "SoloOrTeam";
    public string SuggestedTeamKey { get; set; } = string.Empty;
    public string SuggestedTeamName { get; set; } = string.Empty;
    public string SuggestedConfiguration { get; set; } = string.Empty;
    public string SuggestedPrompt { get; set; } = string.Empty;
    public bool RequiresClarification { get; set; }
    public List<string> ClarifyingQuestions { get; set; } = [];
    public List<string> Domains { get; set; } = [];
    public List<string> ProjectKinds { get; set; } = [];
    public List<string> Toolchains { get; set; } = [];
    public List<string> Evidence { get; set; } = [];
    public List<string> KnowledgeReferences { get; set; } = [];
    public List<string> AlternativeActions { get; set; } = [];
}
