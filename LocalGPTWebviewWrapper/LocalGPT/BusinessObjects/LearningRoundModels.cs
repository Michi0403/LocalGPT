namespace LocalGPT.BusinessObjects;

public sealed class LearningRoundSnapshot
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int ConversationCount { get; set; }
    public int MessageCount { get; set; }
    public int LogCount { get; set; }
    public int KnowledgeCount { get; set; }
    public int RegexCount { get; set; }
    public IReadOnlyList<object> RecentConversations { get; set; } = [];
    public IReadOnlyList<object> RecentMessages { get; set; } = [];
    public IReadOnlyList<object> RecentLogs { get; set; } = [];
    public IReadOnlyList<object> RecentKnowledge { get; set; } = [];
    public IReadOnlyList<object> RegexPatterns { get; set; } = [];
}

public sealed class LearningFactInput
{
    public string Topic { get; set; } = string.Empty;
    public string Scope { get; set; } = "AI Council Learning";
    public string Content { get; set; } = string.Empty;
    public string HelpfulSources { get; set; } = string.Empty;
    public string Tags { get; set; } = "learning-round;model-suggested";
    public int Confidence { get; set; }
}

public sealed class LearningRegexInput
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Flags { get; set; } = "c";
}

public sealed class LearningMaintenanceRequest
{
    public List<LearningFactInput> Facts { get; set; } = [];
    public List<LearningRegexInput> RegexPatterns { get; set; } = [];
}

public sealed record LearningMaintenanceResult(
    int FactsStored,
    int RegexPatternsStored,
    IReadOnlyList<Guid> KnowledgeEntryIds,
    IReadOnlyList<string> RegexNames);
