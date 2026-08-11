namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a learning round snapshot.
/// </summary>
public sealed class LearningRoundSnapshot
{
    /// <summary>
    /// Gets or sets generated at UTC.
    /// </summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets conversation count.
    /// </summary>
    public int ConversationCount { get; set; }
    /// <summary>
    /// Gets or sets message count.
    /// </summary>
    public int MessageCount { get; set; }
    /// <summary>
    /// Gets or sets log count.
    /// </summary>
    public int LogCount { get; set; }
    /// <summary>
    /// Gets or sets knowledge count.
    /// </summary>
    public int KnowledgeCount { get; set; }
    /// <summary>
    /// Gets or sets regex count.
    /// </summary>
    public int RegexCount { get; set; }
    /// <summary>
    /// Gets or sets recent conversations.
    /// </summary>
    public IReadOnlyList<object> RecentConversations { get; set; } = [];
    /// <summary>
    /// Gets or sets recent messages.
    /// </summary>
    public IReadOnlyList<object> RecentMessages { get; set; } = [];
    /// <summary>
    /// Gets or sets recent logs.
    /// </summary>
    public IReadOnlyList<object> RecentLogs { get; set; } = [];
    /// <summary>
    /// Gets or sets recent knowledge.
    /// </summary>
    public IReadOnlyList<object> RecentKnowledge { get; set; } = [];
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public IReadOnlyList<object> RegexPatterns { get; set; } = [];
}

/// <summary>
/// Represents a learning fact input.
/// </summary>
public sealed class LearningFactInput
{
    /// <summary>
    /// Gets or sets topic.
    /// </summary>
    public string Topic { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets scope.
    /// </summary>
    public string Scope { get; set; } = "AI Council Learning";
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets helpful sources.
    /// </summary>
    public string HelpfulSources { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets tags.
    /// </summary>
    public string Tags { get; set; } = "learning-round;model-suggested";
    /// <summary>
    /// Gets or sets confidence.
    /// </summary>
    public int Confidence { get; set; }
}

/// <summary>
/// Represents a learning regex input.
/// </summary>
public sealed class LearningRegexInput
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets pattern.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets flags.
    /// </summary>
    public string Flags { get; set; } = "c";
}

/// <summary>
/// Represents a learning maintenance request.
/// </summary>
public sealed class LearningMaintenanceRequest
{
    /// <summary>
    /// Gets or sets facts.
    /// </summary>
    public List<LearningFactInput> Facts { get; set; } = [];
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public List<LearningRegexInput> RegexPatterns { get; set; } = [];
}

/// <summary>
/// Represents a learning maintenance result.
/// </summary>
public sealed record LearningMaintenanceResult(
    int FactsStored,
    int RegexPatternsStored,
    IReadOnlyList<Guid> KnowledgeEntryIds,
    IReadOnlyList<string> RegexNames);
