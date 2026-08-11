using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.VisualBasic;
using System.Data;
using System.Net;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Provides ef chat memory service operations.
    /// </summary>
    public partial class EfChatMemoryService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<EfChatMemoryService> logger,
        CouncilTextService councilText,
        IChatMemoryMessageMapper messageMapper,
        IChatSessionContext sessionContext) : IChatMemoryService
    {
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly SemaphoreSlim saveGate = new(1, 1);

        /// <summary>
        /// Gets or sets database path.
        /// </summary>
        public string DatabasePath => databaseOptions.DatabasePath;

        /// <summary>
        /// Gets conversations async.
        /// </summary>
        public async Task<IReadOnlyList<ChatMemoryConversationSummary>> GetConversationsAsync(int take = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                return await db.Conversations
                    .AsNoTracking()
                    .OrderByDescending(conversation => conversation.UpdatedAtUtc)
                    .Take(Math.Clamp(take, 1, 200))
                    .Select(conversation => new ChatMemoryConversationSummary(
                        conversation.Id,
                        conversation.Title,
                        conversation.ProviderName,
                        conversation.CreatedAtUtc,
                        conversation.UpdatedAtUtc,
                        conversation.Messages.Count)
                    {
                        ProjectId = conversation.ProjectId,
                        ProjectVersionId = conversation.ProjectVersionId,
                        ApplicationVersion = conversation.ApplicationVersion
                    })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetConversationsAsync take {take}");
                return new List<ChatMemoryConversationSummary>();
            }
        }

        /// <summary>
        /// Loads conversation async.
        /// </summary>
        public async Task<ChatMemoryConversationSnapshot?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var conversation = await db.Conversations
                    .AsNoTracking()
                    .Include(item => item.Messages)
                    .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken).ConfigureAwait(false);

                if (conversation is null)
                    return null;

                var messages = conversation.Messages
                    .OrderBy(message => message.SortOrder)
                    .Select(filter => messageMapper.ToBlazorChatMessage(filter))
                    .ToList();
                ArgumentNullException.ThrowIfNull(messages);
                messages = messageMapper.EnsureVisibleCouncilPrompt(conversation, messages) ?? new List<BlazorChatMessage>();
                ArgumentNullException.ThrowIfNull(messages);
                return new ChatMemoryConversationSnapshot(
                    conversation.Id,
                    conversation.Title,
                    conversation.ProviderName,
                    conversation.CreatedAtUtc,
                    conversation.UpdatedAtUtc,
                    messages)
                {
                    ProjectId = conversation.ProjectId,
                    ProjectVersionId = conversation.ProjectVersionId,
                    ApplicationVersion = conversation.ApplicationVersion
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LoadConversationAsync conversationId {conversationId}");
                return null;
            }
        }

        /// <summary>
        /// Saves conversation async.
        /// </summary>
        public async Task<Guid?> SaveConversationAsync(
            string providerName,
            IReadOnlyList<BlazorChatMessage> messages,
            Guid? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            await saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var completeMessages = messages
                .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
                .ToList();

                if (completeMessages.Count == 0)
                    return conversationId;

                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                var now = DateTime.UtcNow;
                ChatMemoryConversation conversation;

                var isNewConversation = false;
                if (conversationId is Guid id)
                {
                    conversation = await db.Conversations
                        .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                        .ConfigureAwait(false)
                        ?? new ChatMemoryConversation { Id = id, CreatedAtUtc = now };
                    isNewConversation = db.Entry(conversation).State == EntityState.Detached;
                }
                else
                {
                    conversation = new ChatMemoryConversation { CreatedAtUtc = now };
                    isNewConversation = true;
                }

                conversation.Title = messageMapper.BuildTitle(completeMessages);
                conversation.ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Unknown" : providerName.Trim();
                conversation.ProjectId = sessionContext.ProjectId;
                conversation.ProjectVersionId = sessionContext.ProjectVersionId;
                conversation.ApplicationVersion = sessionContext.ApplicationVersion;
                conversation.UpdatedAtUtc = now;

                // Do not clear a tracked required child collection. With DeleteBehavior.Restrict that severs
                // required relationships before EF can mark the old rows for deletion and causes conceptual-null
                // failures during long Council autosaves. Read feedback without tracking, delete the old rows in
                // one database operation, then insert the replacement snapshot with explicit foreign keys.
                var previousFeedback = await db.Messages
                    .AsNoTracking()
                    .Where(message => message.ConversationId == conversation.Id)
                    .ToDictionaryAsync(
                        message => (message.SortOrder, message.Role, message.Content),
                        message => new
                        {
                            message.IsPositiveFeedback,
                            message.FeedbackComment,
                            message.FeedbackUpdatedAtUtc
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!isNewConversation)
                {
                    await db.Messages
                        .Where(message => message.ConversationId == conversation.Id)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    db.Conversations.Add(conversation);
                }

                var index = 0;
                foreach (var message in completeMessages)
                {
                    var sortOrder = index++;
                    var role = messageMapper.ToRoleName(message.Role);
                    previousFeedback.TryGetValue((sortOrder, role, message.Content), out var feedback);
                    db.Messages.Add(new ChatMemoryMessage
                    {
                        ConversationId = conversation.Id,
                        SortOrder = sortOrder,
                        Role = role,
                        Content = message.Content,
                        Thinking = councilText.ExtractThinking(message.Content, logger),
                        IsPositiveFeedback = feedback?.IsPositiveFeedback,
                        FeedbackComment = feedback?.FeedbackComment ?? string.Empty,
                        FeedbackUpdatedAtUtc = feedback?.FeedbackUpdatedAtUtc,
                        CreatedAtUtc = now
                    });
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                sessionContext.SetConversation(conversation.Id);
                logger.LogInformation("Saved chat memory conversation {ConversationId} with {MessageCount} messages.", conversation.Id, completeMessages.Count);
                return conversation.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not save the conversation for provider {ProviderName}; conversation {ConversationId}.", providerName, conversationId);
                return null;
            }
            finally
            {
                saveGate.Release();
            }
        }


        /// <summary>
        /// Gets message feedback async.
        /// </summary>
        public async Task<IReadOnlyList<ChatMessageFeedbackSnapshot>> GetMessageFeedbackAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                var values = await db.Messages
                    .AsNoTracking()
                    .Where(message => message.ConversationId == conversationId && message.Role == "assistant")
                    .OrderBy(message => message.SortOrder)
                    .Select(message => new
                    {
                        message.Id,
                        message.ConversationId,
                        message.SortOrder,
                        message.Role,
                        message.Content,
                        message.IsPositiveFeedback,
                        message.FeedbackComment,
                        message.FeedbackUpdatedAtUtc
                    })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return values.Select(message => new ChatMessageFeedbackSnapshot(
                    message.Id,
                    message.ConversationId,
                    message.SortOrder,
                    message.Role,
                    councilText.TrimForDisplay(message.Content, 180, logger),
                    message.IsPositiveFeedback,
                    message.FeedbackComment,
                    message.FeedbackUpdatedAtUtc)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not load feedback metadata for conversation {ConversationId}.", conversationId);
                return [];
            }
        }

        /// <summary>
        /// Runs the record message feedback async operation.
        /// </summary>
        public async Task<bool> RecordMessageFeedbackAsync(
            Guid conversationId,
            int sortOrder,
            bool? isPositive,
            string? comment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                var message = await db.Messages.SingleOrDefaultAsync(
                    item => item.ConversationId == conversationId && item.SortOrder == sortOrder,
                    cancellationToken).ConfigureAwait(false);
                if (message is null || !string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    return false;

                message.IsPositiveFeedback = isPositive;
                message.FeedbackComment = string.IsNullOrWhiteSpace(comment)
                    ? string.Empty
                    : comment.Trim()[..Math.Min(comment.Trim().Length, 4000)];
                message.FeedbackUpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation(
                    "Recorded chat feedback for conversation {ConversationId}, message order {SortOrder}; positive={IsPositive}.",
                    conversationId,
                    sortOrder,
                    isPositive);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not record feedback for conversation {ConversationId}, message order {SortOrder}.", conversationId, sortOrder);
                return false;
            }
        }

        /// <summary>
        /// Gets recent thoughts async.
        /// </summary>
        public async Task<IReadOnlyList<ChatMemoryThought>> GetRecentThoughtsAsync(int take = 12, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                return await db.Messages
                    .AsNoTracking()
                    .Where(message => message.Thinking != null && message.Thinking != string.Empty)
                    .OrderByDescending(message => message.CreatedAtUtc)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(message => new ChatMemoryThought(
                        message.ConversationId,
                        message.Conversation.Title,
                        message.CreatedAtUtc,
                        message.Thinking!))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetRecentThoughtsAsync take {take}");
                return new List<ChatMemoryThought>();
            }
        }

        /// <summary>
        /// Builds memory briefing async.
        /// </summary>
        public async Task<string> BuildMemoryBriefingAsync(int conversationTake = 5, int thoughtTake = 5, CancellationToken cancellationToken = default)
        {
            try
            {
                var conversations = await GetConversationsAsync(conversationTake, cancellationToken).ConfigureAwait(false);
                var thoughts = await GetRecentThoughtsAsync(thoughtTake, cancellationToken).ConfigureAwait(false);

                if (conversations.Count == 0 && thoughts.Count == 0)
                    return string.Empty;

                var builder = new StringBuilder();
                if (conversations.Count > 0)
                {
                    builder.AppendLine("Recent conversations:");
                    foreach (var conversation in conversations)
                    {
                        builder.AppendLine($"- {conversation.Title} ({conversation.ProviderName}, {conversation.MessageCount} messages, updated {conversation.UpdatedAtUtc:u})");
                    }
                }

                if (thoughts.Count > 0)
                {
                    builder.AppendLine("Former model thoughts:");
                    foreach (var thought in thoughts)
                    {
                        builder.AppendLine($"- {thought.ConversationTitle}: {councilText.TrimForPrompt(thought.Thinking, 500, logger)}");
                    }
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,$"Error in BuildMemoryBriefingAsync conversationTake {conversationTake} thoughtTake {thoughtTake}");
                return string.Empty;
            }
            
        }
        //Todo get rid of it centralize
        /// <summary>
        /// Creates db context async.
        /// </summary>
        private async Task<LocalGptMemoryDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
        {
            try
            {
                await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
                return await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the LocalGPT database context.");
                throw new InvalidOperationException("LocalGPT could not initialize its database context.", ex);
            }
        }
    }
}