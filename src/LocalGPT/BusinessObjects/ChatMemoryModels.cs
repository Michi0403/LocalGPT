using DevExpress.AIIntegration.Blazor.Chat;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a chat memory conversation application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class ChatMemoryConversation
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this chat memory conversation instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets the title value that forms part of the chat memory conversation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The title value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public string Title { get; set; } = "New conversation";
        /// <summary>
        /// Gets or sets the provider name value that forms part of the chat memory conversation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The provider name value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public string ProviderName { get; set; } = "Unknown";
        /// <summary>
        /// Gets or sets the stable project identifier used to identify or correlate this chat memory conversation instance with related application state.
        /// </summary>
        /// <value>The project identifier value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public Guid? ProjectId { get; set; }
        /// <summary>
        /// Gets or sets the stable project version identifier used to identify or correlate this chat memory conversation instance with related application state.
        /// </summary>
        /// <value>The project version identifier value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public Guid? ProjectVersionId { get; set; }
        /// <summary>
        /// Gets or sets the application version value that forms part of the chat memory conversation state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The application version value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public string ApplicationVersion { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the created at UTC associated with this chat memory conversation state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The created at UTC value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the updated at UTC associated with this chat memory conversation state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The updated at UTC value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the messages collection maintained or exposed by this chat memory conversation instance for downstream processing.
        /// </summary>
        /// <value>The messages value exposed by <see cref="ChatMemoryConversation"/>.</value>
        public ICollection<ChatMemoryMessage> Messages { get; set; } = new List<ChatMemoryMessage>();
    }

    /// <summary>
    /// Represents a chat memory message application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class ChatMemoryMessage
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this chat memory message instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets the stable conversation identifier used to identify or correlate this chat memory message instance with related application state.
        /// </summary>
        /// <value>The conversation identifier value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public Guid ConversationId { get; set; }
        /// <summary>
        /// Gets or sets the conversation value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The conversation value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public ChatMemoryConversation Conversation { get; set; } = null!;
        /// <summary>
        /// Gets or sets the sort order value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sort order value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public int SortOrder { get; set; }
        /// <summary>
        /// Gets or sets the role value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The role value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public string Role { get; set; } = "user";
        /// <summary>
        /// Gets or sets the content value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The content value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the thinking value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The thinking value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public string? Thinking { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether positive feedback applies to the chat memory message state.
        /// </summary>
        /// <value>The is positive feedback value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public bool? IsPositiveFeedback { get; set; }
        /// <summary>
        /// Gets or sets the feedback comment value that forms part of the chat memory message state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The feedback comment value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public string FeedbackComment { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the feedback updated at UTC associated with this chat memory message state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The feedback updated at UTC value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public DateTime? FeedbackUpdatedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the created at UTC associated with this chat memory message state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The created at UTC value exposed by <see cref="ChatMemoryMessage"/>.</value>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a chat memory conversation summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Id">Identifier of the resource to use for this operation.</param>
    /// <param name="Title">Title value supplied to the chat memory conversation summary operation and used when producing its result.</param>
    /// <param name="ProviderName">Provider name value supplied to the chat memory conversation summary operation and used when producing its result.</param>
    /// <param name="CreatedAtUtc">Created at utc value supplied to the chat memory conversation summary operation and used when producing its result.</param>
    /// <param name="UpdatedAtUtc">Updated at utc value supplied to the chat memory conversation summary operation and used when producing its result.</param>
    /// <param name="MessageCount">Message count value supplied to the chat memory conversation summary operation and used when producing its result.</param>
    public sealed record ChatMemoryConversationSummary(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        int MessageCount)
    {
        /// <summary>
        /// Gets or sets the stable project identifier used to identify or correlate this chat memory conversation summary instance with related application state.
        /// </summary>
        /// <value>The project identifier value exposed by <see cref="ChatMemoryConversationSummary"/>.</value>
        public Guid? ProjectId { get; init; }
        /// <summary>
        /// Gets or sets the stable project version identifier used to identify or correlate this chat memory conversation summary instance with related application state.
        /// </summary>
        /// <value>The project version identifier value exposed by <see cref="ChatMemoryConversationSummary"/>.</value>
        public Guid? ProjectVersionId { get; init; }
        /// <summary>
        /// Gets or sets the application version value that forms part of the chat memory conversation summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The application version value exposed by <see cref="ChatMemoryConversationSummary"/>.</value>
        public string ApplicationVersion { get; init; } = string.Empty;
        /// <summary>
        /// Gets the display name value that forms part of the chat memory conversation summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The display name value exposed by <see cref="ChatMemoryConversationSummary"/>.</value>
        public string DisplayName => $"{UpdatedAtUtc:g} - {Title}";
    }

    /// <summary>
    /// Represents a chat memory conversation snapshot application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Id">Identifier of the resource to use for this operation.</param>
    /// <param name="Title">Title value supplied to the chat memory conversation snapshot operation and used when producing its result.</param>
    /// <param name="ProviderName">Provider name value supplied to the chat memory conversation snapshot operation and used when producing its result.</param>
    /// <param name="CreatedAtUtc">Created at utc value supplied to the chat memory conversation snapshot operation and used when producing its result.</param>
    /// <param name="UpdatedAtUtc">Updated at utc value supplied to the chat memory conversation snapshot operation and used when producing its result.</param>
    /// <param name="Messages">Messages value supplied to the chat memory conversation snapshot operation and used when producing its result.</param>
    public sealed record ChatMemoryConversationSnapshot(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        List<BlazorChatMessage> Messages)
    {
        /// <summary>
        /// Gets or sets the stable project identifier used to identify or correlate this chat memory conversation snapshot instance with related application state.
        /// </summary>
        /// <value>The project identifier value exposed by <see cref="ChatMemoryConversationSnapshot"/>.</value>
        public Guid? ProjectId { get; init; }
        /// <summary>
        /// Gets or sets the stable project version identifier used to identify or correlate this chat memory conversation snapshot instance with related application state.
        /// </summary>
        /// <value>The project version identifier value exposed by <see cref="ChatMemoryConversationSnapshot"/>.</value>
        public Guid? ProjectVersionId { get; init; }
        /// <summary>
        /// Gets or sets the application version value that forms part of the chat memory conversation snapshot state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The application version value exposed by <see cref="ChatMemoryConversationSnapshot"/>.</value>
        public string ApplicationVersion { get; init; } = string.Empty;
    }

    /// <summary>
    /// Represents a chat memory thought application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="ConversationId">Identifier of the conversation to use for this operation.</param>
    /// <param name="ConversationTitle">Conversation title value supplied to the chat memory thought operation and used when producing its result.</param>
    /// <param name="CreatedAtUtc">Created at utc value supplied to the chat memory thought operation and used when producing its result.</param>
    /// <param name="Thinking">Thinking value supplied to the chat memory thought operation and used when producing its result.</param>
    public sealed record ChatMemoryThought(
        Guid ConversationId,
        string ConversationTitle,
        DateTime CreatedAtUtc,
        string Thinking);
}
