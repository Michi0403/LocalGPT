using DevExpress.AIIntegration.Blazor.Chat;

namespace LocalGPT.BusinessObjects
{
    public class ChatMemoryConversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "New conversation";
        public string ProviderName { get; set; } = "Unknown";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public ICollection<ChatMemoryMessage> Messages { get; set; } = new List<ChatMemoryMessage>();
    }

    public class ChatMemoryMessage
    {
        public long Id { get; set; }
        public Guid ConversationId { get; set; }
        public ChatMemoryConversation Conversation { get; set; } = null!;
        public int SortOrder { get; set; }
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public string? Thinking { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed record ChatMemoryConversationSummary(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        int MessageCount)
    {
        public string DisplayName => $"{UpdatedAtUtc:g} - {Title}";
    }

    public sealed record ChatMemoryConversationSnapshot(
        Guid Id,
        string Title,
        string ProviderName,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        List<BlazorChatMessage> Messages);

    public sealed record ChatMemoryThought(
        Guid ConversationId,
        string ConversationTitle,
        DateTime CreatedAtUtc,
        string Thinking);
}
