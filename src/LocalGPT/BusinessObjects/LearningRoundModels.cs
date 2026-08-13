namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a learning round snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningRoundSnapshot
{
    /// <summary>
    /// Gets or sets the generated at UTC associated with this learning round snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated at UTC value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the conversation count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The conversation count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int ConversationCount { get; set; }
    /// <summary>
    /// Gets or sets the message count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The message count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int MessageCount { get; set; }
    /// <summary>
    /// Gets or sets the log count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The log count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int LogCount { get; set; }
    /// <summary>
    /// Gets or sets the knowledge count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The knowledge count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int KnowledgeCount { get; set; }
    /// <summary>
    /// Gets or sets the regex count that quantifies the associated learning round snapshot data.
    /// </summary>
    /// <value>The regex count value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public int RegexCount { get; set; }
    /// <summary>
    /// Gets or sets the recent conversations collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent conversations value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentConversations { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent messages collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent messages value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentMessages { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent logs collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent logs value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentLogs { get; set; } = [];
    /// <summary>
    /// Gets or sets the recent knowledge collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The recent knowledge value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RecentKnowledge { get; set; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this learning round snapshot instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LearningRoundSnapshot"/>.</value>
    public IReadOnlyList<object> RegexPatterns { get; set; } = [];
}

/// <summary>
/// Represents a learning fact input application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningFactInput
{
    /// <summary>
    /// Gets or sets the topic value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The topic value exposed by <see cref="LearningFactInput"/>.</value>
    public string Topic { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scope value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scope value exposed by <see cref="LearningFactInput"/>.</value>
    public string Scope { get; set; } = "AI Council Learning";
    /// <summary>
    /// Gets or sets the content value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="LearningFactInput"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the helpful sources value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The helpful sources value exposed by <see cref="LearningFactInput"/>.</value>
    public string HelpfulSources { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tags value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The tags value exposed by <see cref="LearningFactInput"/>.</value>
    public string Tags { get; set; } = "learning-round;model-suggested";
    /// <summary>
    /// Gets or sets the confidence value that forms part of the learning fact input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence value exposed by <see cref="LearningFactInput"/>.</value>
    public int Confidence { get; set; }
}

/// <summary>
/// Represents a learning regex input application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LearningRegexInput
{
    /// <summary>
    /// Gets or sets the name value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the pattern value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pattern value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Pattern { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the flags value that forms part of the learning regex input state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The flags value exposed by <see cref="LearningRegexInput"/>.</value>
    public string Flags { get; set; } = "c";
}

/// <summary>
/// Represents the input contract for learning maintenance, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LearningMaintenanceRequest
{
    /// <summary>
    /// Gets or sets the facts collection maintained or exposed by this learning maintenance instance for downstream processing.
    /// </summary>
    /// <value>The facts value exposed by <see cref="LearningMaintenanceRequest"/>.</value>
    public List<LearningFactInput> Facts { get; set; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this learning maintenance instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LearningMaintenanceRequest"/>.</value>
    public List<LearningRegexInput> RegexPatterns { get; set; } = [];
}

/// <summary>
/// Represents the outcome of learning maintenance, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="FactsStored">Facts stored value supplied to the learning maintenance operation and used when producing its result.</param>
/// <param name="RegexPatternsStored">Regex patterns stored value supplied to the learning maintenance operation and used when producing its result.</param>
/// <param name="KnowledgeEntryIds">Guid dependency used by the learning maintenance workflow to provide the corresponding application capability.</param>
/// <param name="RegexNames">String dependency used by the learning maintenance workflow to provide the corresponding application capability.</param>
public sealed record LearningMaintenanceResult(
    int FactsStored,
    int RegexPatternsStored,
    IReadOnlyList<Guid> KnowledgeEntryIds,
    IReadOnlyList<string> RegexNames);
