using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Extensions.PlainStatics;
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
    public partial class EfChatMemoryService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<EfChatMemoryService> logger) : IChatMemoryService
    {
        public string DatabasePath => databaseOptions.DatabasePath;

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
                        conversation.Messages.Count))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetConversationsAsync take {take}");
                return new List<ChatMemoryConversationSummary>();
            }
        }

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
                    .Select(filter => DevExpressFunctions.ToBlazorChatMessage(filter,logger))
                    .ToList();
                ArgumentNullException.ThrowIfNull(messages);
                messages = DevExpressFunctions.EnsureVisibleCouncilPrompt(conversation, (List<BlazorChatMessage> )messages, logger) ?? new List<BlazorChatMessage>();
                ArgumentNullException.ThrowIfNull(messages);
                return new ChatMemoryConversationSnapshot(
                    conversation.Id,
                    conversation.Title,
                    conversation.ProviderName,
                    conversation.CreatedAtUtc,
                    conversation.UpdatedAtUtc,
                    messages);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LoadConversationAsync conversationId {conversationId}");
                return null;
            }
        }

        public async Task<Guid?> SaveConversationAsync(
            string providerName,
            IReadOnlyList<BlazorChatMessage> messages,
            Guid? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var completeMessages = messages
                .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
                .ToList();

                if (completeMessages.Count == 0)
                    return conversationId;

                await using var db = await CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(db);
                var now = DateTime.UtcNow;
                ChatMemoryConversation conversation;

                if (conversationId is Guid id)
                {
                    conversation = await db.Conversations
                        .Include(item => item.Messages)
                        .SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false)
                        ?? new ChatMemoryConversation { Id = id, CreatedAtUtc = now };
                }
                else
                {
                    conversation = new ChatMemoryConversation { CreatedAtUtc = now };
                }

                conversation.Title = DevExpressFunctions.BuildTitle(completeMessages, logger);
                conversation.ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Unknown" : providerName.Trim();
                conversation.UpdatedAtUtc = now;
                conversation.Messages.Clear();

                var index = 0;
                foreach (var message in completeMessages)
                {
                    conversation.Messages.Add(new ChatMemoryMessage
                    {
                        SortOrder = index++,
                        Role = DevExpressFunctions.ToRoleName(message.Role, logger),
                        Content = message.Content,
                        Thinking = CouncilChatStringFunctions.ExtractThinking(message.Content,logger),
                        CreatedAtUtc = now
                    });
                }

                if (db.Entry(conversation).State == EntityState.Detached)
                    db.Conversations.Add(conversation);

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Saved chat memory conversation {ConversationId} with {MessageCount} messages.", conversation.Id, completeMessages.Count);
                return conversation.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not save the conversation for provider {ProviderName}; conversation {ConversationId}.", providerName, conversationId);
                return null;
            }
        }

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
                        builder.AppendLine($"- {thought.ConversationTitle}: {CouncilChatStringFunctions.TrimForPrompt(thought.Thinking, 500, logger)}");
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
        private async Task<LocalGptMemoryDbContext?> CreateDbContextAsync(CancellationToken cancellationToken)
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
                return null;
            }
        }
    }
}