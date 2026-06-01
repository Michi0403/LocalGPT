using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IChatMemoryService
    {
        string DatabasePath { get; }
        Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatMemoryConversationSummary>> GetConversationsAsync(int take = 50, CancellationToken cancellationToken = default);
        Task<ChatMemoryConversationSnapshot?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
        Task<Guid?> SaveConversationAsync(string providerName, IReadOnlyList<BlazorChatMessage> messages, Guid? conversationId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatMemoryThought>> GetRecentThoughtsAsync(int take = 12, CancellationToken cancellationToken = default);
        Task<string> BuildMemoryBriefingAsync(int conversationTake = 5, int thoughtTake = 5, CancellationToken cancellationToken = default);
    }
}
