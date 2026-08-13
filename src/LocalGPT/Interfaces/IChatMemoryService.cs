using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for chat memory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IChatMemoryService
    {
        /// <summary>
        /// Gets the database path used by this chat memory instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="IChatMemoryService"/>.</value>
        string DatabasePath { get; }
        /// <summary>
        /// Retrieves conversations as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="take">Take value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<ChatMemoryConversationSummary>> GetConversationsAsync(int take = 50, CancellationToken cancellationToken = default);
        /// <summary>
        /// Loads conversation as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat memory conversation snapshot produced by the operation.</returns>
        Task<ChatMemoryConversationSnapshot?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Persists conversation as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="messages">Blazor chat message dependency used by the chat memory workflow to provide the corresponding application capability.</param>
        /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The GUID produced by the operation.</returns>
        Task<Guid?> SaveConversationAsync(string providerName, IReadOnlyList<BlazorChatMessage> messages, Guid? conversationId = null, CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieves message feedback as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<ChatMessageFeedbackSnapshot>> GetMessageFeedbackAsync(Guid conversationId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Performs record message feedback as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
        /// <param name="sortOrder">Sort order value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="isPositive">Is positive value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="comment">Comment value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        Task<bool> RecordMessageFeedbackAsync(Guid conversationId, int sortOrder, bool? isPositive, string? comment, CancellationToken cancellationToken = default);
        /// <summary>
        /// Retrieves recent thoughts as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="take">Take value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<ChatMemoryThought>> GetRecentThoughtsAsync(int take = 12, CancellationToken cancellationToken = default);
        /// <summary>
        /// Builds memory briefing as part of the chat memory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversationTake">Conversation take value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="thoughtTake">Thought take value supplied to the chat memory operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> BuildMemoryBriefingAsync(int conversationTake = 5, int thoughtTake = 5, CancellationToken cancellationToken = default);
    }
}
