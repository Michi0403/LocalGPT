using DevExpress.AIIntegration.Blazor.Chat;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a chat memory conversation.
    /// </summary>
    public class ChatMemoryConversation
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets title.
        /// </summary>
        public string Title { get; set; } = "New conversation";
        /// <summary>
        /// Gets or sets provider name.
        /// </summary>
        public string ProviderName { get; set; } = "Unknown";
        /// <summary>
        /// Gets or sets project identifier.
        /// </summary>
        public Guid? ProjectId { get; set; }
        /// <summary>
        /// Gets or sets project version identifier.
        /// </summary>
        public Guid? ProjectVersionId { get; set; }
        /// <summary>
        /// Gets or sets application version.
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets created at UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets updated at UTC.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets messages.
        /// </summary>
        public ICollection<ChatMemoryMessage> Messages { get; set; } = new List<ChatMemoryMessage>();
    }

    /// <summary>
    /// Represents a chat memory message.
    /// </summary>
    public class ChatMemoryMessage
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets conversation identifier.
        /// </summary>
        public Guid ConversationId { get; set; }
        /// <summary>
        /// Gets or sets conversation.
        /// </summary>
        public ChatMemoryConversation Conversation { get; set; } = null!;
        /// <summary>
        /// Gets or sets sort order.
        /// </summary>
        public int SortOrder { get; set; }
        /// <summary>
        /// Gets or sets role.
        /// </summary>
        public string Role { get; set; } = "user";
        /// <summary>
        /// Gets or sets content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets thinking.
        /// </summary>
        public string? Thinking { get; set; }
        /// <summary>
        /// Gets or sets is positive feedback.
        /// </summary>
        public bool? IsPositiveFeedback { get; set; }
        /// <summary>
        /// Gets or sets feedback comment.
        /// </summary>
        public string FeedbackComment { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets feedback updated at UTC.
        /// </summary>
        public DateTime? FeedbackUpdatedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets created at UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a chat memory conversation summary.
    /// </summary>
    public sealed record ChatMemoryConversationSummary(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        int MessageCount)
    {
        /// <summary>
        /// Gets or sets project identifier.
        /// </summary>
        public Guid? ProjectId { get; init; }
        /// <summary>
        /// Gets or sets project version identifier.
        /// </summary>
        public Guid? ProjectVersionId { get; init; }
        /// <summary>
        /// Gets or sets application version.
        /// </summary>
        public string ApplicationVersion { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets display name.
        /// </summary>
        public string DisplayName => $"{UpdatedAtUtc:g} - {Title}";
    }

    /// <summary>
    /// Represents a chat memory conversation snapshot.
    /// </summary>
    public sealed record ChatMemoryConversationSnapshot(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        List<BlazorChatMessage> Messages)
    {
        /// <summary>
        /// Gets or sets project identifier.
        /// </summary>
        public Guid? ProjectId { get; init; }
        /// <summary>
        /// Gets or sets project version identifier.
        /// </summary>
        public Guid? ProjectVersionId { get; init; }
        /// <summary>
        /// Gets or sets application version.
        /// </summary>
        public string ApplicationVersion { get; init; } = string.Empty;
    }

    /// <summary>
    /// Represents a chat memory thought.
    /// </summary>
    public sealed record ChatMemoryThought(
        Guid ConversationId,
        string ConversationTitle,
        DateTime CreatedAtUtc,
        string Thinking);
}
