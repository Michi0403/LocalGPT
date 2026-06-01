using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Data;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    public partial class EfChatMemoryService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        ILogger<EfChatMemoryService> logger) : IChatMemoryService
    {
        public string DatabasePath { get; } = GetDefaultDatabasePath();

        public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await CreateDbContextAsync(cancellationToken);
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ChatMemoryConversationSummary>> GetConversationsAsync(int take = 50, CancellationToken cancellationToken = default)
        {
            await using var db = await CreateDbContextAsync(cancellationToken);
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
                .ToListAsync(cancellationToken);
        }

        public async Task<ChatMemoryConversationSnapshot?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
        {
            await using var db = await CreateDbContextAsync(cancellationToken);
            var conversation = await db.Conversations
                .AsNoTracking()
                .Include(item => item.Messages)
                .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

            if (conversation is null)
                return null;

            var messages = conversation.Messages
                .OrderBy(message => message.SortOrder)
                .Select(ToBlazorChatMessage)
                .ToList();

            return new ChatMemoryConversationSnapshot(
                conversation.Id,
                conversation.Title,
                conversation.ProviderName,
                conversation.CreatedAtUtc,
                conversation.UpdatedAtUtc,
                messages);
        }

        public async Task<Guid?> SaveConversationAsync(
            string providerName,
            IReadOnlyList<BlazorChatMessage> messages,
            Guid? conversationId = null,
            CancellationToken cancellationToken = default)
        {
            var completeMessages = messages
                .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
                .ToList();

            if (completeMessages.Count == 0)
                return conversationId;

            await using var db = await CreateDbContextAsync(cancellationToken);
            var now = DateTime.UtcNow;
            ChatMemoryConversation conversation;

            if (conversationId is Guid id)
            {
                conversation = await db.Conversations
                    .Include(item => item.Messages)
                    .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                    ?? new ChatMemoryConversation { Id = id, CreatedAtUtc = now };
            }
            else
            {
                conversation = new ChatMemoryConversation { CreatedAtUtc = now };
            }

            conversation.Title = BuildTitle(completeMessages);
            conversation.ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Unknown" : providerName.Trim();
            conversation.UpdatedAtUtc = now;
            conversation.Messages.Clear();

            var index = 0;
            foreach (var message in completeMessages)
            {
                conversation.Messages.Add(new ChatMemoryMessage
                {
                    SortOrder = index++,
                    Role = ToRoleName(message.Role),
                    Content = message.Content,
                    Thinking = ExtractThinking(message.Content),
                    CreatedAtUtc = now
                });
            }

            if (db.Entry(conversation).State == EntityState.Detached)
                db.Conversations.Add(conversation);

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved chat memory conversation {ConversationId} with {MessageCount} messages.", conversation.Id, completeMessages.Count);
            return conversation.Id;
        }

        public async Task<IReadOnlyList<ChatMemoryThought>> GetRecentThoughtsAsync(int take = 12, CancellationToken cancellationToken = default)
        {
            await using var db = await CreateDbContextAsync(cancellationToken);
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
                .ToListAsync(cancellationToken);
        }

        public async Task<string> BuildMemoryBriefingAsync(int conversationTake = 5, int thoughtTake = 5, CancellationToken cancellationToken = default)
        {
            var conversations = await GetConversationsAsync(conversationTake, cancellationToken);
            var thoughts = await GetRecentThoughtsAsync(thoughtTake, cancellationToken);

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
                    builder.AppendLine($"- {thought.ConversationTitle}: {TrimForPrompt(thought.Thinking, 500)}");
                }
            }

            return builder.ToString().Trim();
        }

        private async Task<LocalGptMemoryDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return db;
        }

        private static BlazorChatMessage ToBlazorChatMessage(ChatMemoryMessage message)
        {
            return new BlazorChatMessage(new ChatRole(message.Role), message.Content, new List<AIChatUploadFileInfo>());
        }

        private static string BuildTitle(IReadOnlyList<BlazorChatMessage> messages)
        {
            var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.First().Content;
            var title = WhitespacePattern().Replace(StripThinking(firstUserMessage), " ").Trim();

            if (string.IsNullOrWhiteSpace(title))
                return "New conversation";

            return title.Length <= 90 ? title : $"{title[..87].TrimEnd()}...";
        }

        private static string? ExtractThinking(string content)
        {
            var match = ThinkingBlockPattern().Match(content);
            if (!match.Success)
                return null;

            var thinking = WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim();
            return string.IsNullOrWhiteSpace(thinking) ? null : thinking;
        }

        private static string StripThinking(string content)
        {
            return ThinkingBlockPattern().Replace(content, string.Empty);
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(text, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static string ToRoleName(ChatMessageRole role)
        {
            return role switch
            {
                ChatMessageRole.Assistant => "assistant",
                ChatMessageRole.System => "system",
                ChatMessageRole.Error => "error",
                _ => "user"
            };
        }

        public static string GetDefaultDatabasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalGPT",
                "localgpt-memory.db");
        }

        [GeneratedRegex("<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex ThinkingBlockPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();
    }
}
