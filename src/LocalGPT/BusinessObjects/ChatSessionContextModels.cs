namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a chat session context snapshot.
/// </summary>
public sealed record ChatSessionContextSnapshot(
    Guid? ConversationId,
    Guid? ProjectId,
    Guid? ProjectVersionId,
    string ApplicationVersion);

/// <summary>
/// Represents a chat message feedback snapshot.
/// </summary>
public sealed record ChatMessageFeedbackSnapshot(
    long MessageId,
    Guid ConversationId,
    int SortOrder,
    string Role,
    string Preview,
    bool? IsPositive,
    string Comment,
    DateTime? UpdatedAtUtc);
