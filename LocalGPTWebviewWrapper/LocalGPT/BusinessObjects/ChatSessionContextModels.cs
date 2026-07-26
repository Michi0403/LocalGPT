namespace LocalGPT.BusinessObjects;

public sealed record ChatSessionContextSnapshot(
    Guid? ConversationId,
    Guid? ProjectId,
    Guid? ProjectVersionId,
    string ApplicationVersion);

public sealed record ChatMessageFeedbackSnapshot(
    long MessageId,
    Guid ConversationId,
    int SortOrder,
    string Role,
    string Preview,
    bool? IsPositive,
    string Comment,
    DateTime? UpdatedAtUtc);
