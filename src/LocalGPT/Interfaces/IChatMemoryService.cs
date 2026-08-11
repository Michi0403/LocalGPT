using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the chat memory service contract.
    /// </summary>
    public interface IChatMemoryService
    {
        string DatabasePath { get; }
        /// <summary>
        /// Gets conversations async.
        /// </summary>
        Task<IReadOnlyList<ChatMemoryConversationSummary>> GetConversationsAsync(int take = 50, CancellationToken cancellationToken = default);
        /// <summary>
        /// Loads conversation async.
        /// </summary>
        Task<ChatMemoryConversationSnapshot?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Saves conversation async.
        /// </summary>
        Task<Guid?> SaveConversationAsync(string providerName, IReadOnlyList<BlazorChatMessage> messages, Guid? conversationId = null, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets message feedback async.
        /// </summary>
        Task<IReadOnlyList<ChatMessageFeedbackSnapshot>> GetMessageFeedbackAsync(Guid conversationId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Runs the record message feedback async operation.
        /// </summary>
        Task<bool> RecordMessageFeedbackAsync(Guid conversationId, int sortOrder, bool? isPositive, string? comment, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets recent thoughts async.
        /// </summary>
        Task<IReadOnlyList<ChatMemoryThought>> GetRecentThoughtsAsync(int take = 12, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds memory briefing async.
        /// </summary>
        Task<string> BuildMemoryBriefingAsync(int conversationTake = 5, int thoughtTake = 5, CancellationToken cancellationToken = default);
    }
}
