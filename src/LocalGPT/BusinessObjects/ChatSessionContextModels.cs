namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a chat session context snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="ConversationId">Identifier of the conversation to use for this operation.</param>
/// <param name="ProjectId">Identifier of the project to use for this operation.</param>
/// <param name="ProjectVersionId">Identifier of the project version to use for this operation.</param>
/// <param name="ApplicationVersion">Application version value supplied to the chat session context snapshot operation and used when producing its result.</param>
public sealed record ChatSessionContextSnapshot(
    Guid? ConversationId,
    Guid? ProjectId,
    Guid? ProjectVersionId,
    string ApplicationVersion);

/// <summary>
/// Represents a chat message feedback snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="MessageId">Identifier of the message to use for this operation.</param>
/// <param name="ConversationId">Identifier of the conversation to use for this operation.</param>
/// <param name="SortOrder">Sort order value supplied to the chat message feedback snapshot operation and used when producing its result.</param>
/// <param name="Role">Role value supplied to the chat message feedback snapshot operation and used when producing its result.</param>
/// <param name="Preview">Preview value supplied to the chat message feedback snapshot operation and used when producing its result.</param>
/// <param name="IsPositive">Value indicating whether positive should apply to this operation.</param>
/// <param name="Comment">Comment value supplied to the chat message feedback snapshot operation and used when producing its result.</param>
/// <param name="UpdatedAtUtc">Updated at utc value supplied to the chat message feedback snapshot operation and used when producing its result.</param>
public sealed record ChatMessageFeedbackSnapshot(
    long MessageId,
    Guid ConversationId,
    int SortOrder,
    string Role,
    string Preview,
    bool? IsPositive,
    string Comment,
    DateTime? UpdatedAtUtc);
