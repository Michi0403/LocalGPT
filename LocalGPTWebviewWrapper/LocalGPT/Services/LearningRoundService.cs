using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Builds a database-grounded learning-round snapshot and persists model-suggested facts/regexes as
/// untrusted self-maintenance knowledge. It never promotes model output to user-approved authority.
/// </summary>
public sealed class LearningRoundService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ICouncilKnowledgeService knowledgeService,
    IRegexPatternService regexPatternService,
    ILogger<LearningRoundService> logger) : ILearningRoundService
{
    public async Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var take = Math.Clamp(takePerSource, 1, 10_000);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Keep database expressions simple and materialize before truncating/casting to object. This avoids
        // provider-specific translation failures while retaining bounded evidence packages for local models.
        var conversations = await db.Conversations.AsNoTracking()
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(take)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.ProviderName,
                item.ProjectId,
                item.ProjectVersionId,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                MessageCount = item.Messages.Count
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var messages = await db.Messages.AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(take)
            .Select(item => new
            {
                item.ConversationId,
                item.SortOrder,
                item.Role,
                item.Content,
                item.Thinking,
                item.IsPositiveFeedback,
                item.FeedbackComment,
                item.CreatedAtUtc
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var logs = await db.ApplicationLogs.AsNoTracking()
            .OrderByDescending(item => item.TimestampUtc)
            .Take(take)
            .Select(item => new
            {
                item.TimestampUtc,
                item.Level,
                item.Category,
                item.EventId,
                item.EventName,
                item.Message,
                item.Exception
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var knowledge = await db.CouncilKnowledgeEntries.AsNoTracking()
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(take)
            .Select(item => new
            {
                item.Id,
                item.Topic,
                item.Scope,
                item.Content,
                item.Source,
                item.Tags,
                item.Confidence,
                item.VerificationStatus,
                item.ReviewStatus,
                item.IsUserApproved,
                item.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var regexPatterns = await db.RegexPatterns.AsNoTracking()
            .OrderBy(item => item.Name)
            .Take(take)
            .Select(item => new { item.Name, item.Pattern, item.Flags, item.UpdatedOn })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var snapshot = new LearningRoundSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ConversationCount = await db.Conversations.CountAsync(cancellationToken).ConfigureAwait(false),
            MessageCount = await db.Messages.CountAsync(cancellationToken).ConfigureAwait(false),
            LogCount = await db.ApplicationLogs.CountAsync(cancellationToken).ConfigureAwait(false),
            KnowledgeCount = await db.CouncilKnowledgeEntries.CountAsync(cancellationToken).ConfigureAwait(false),
            RegexCount = await db.RegexPatterns.CountAsync(cancellationToken).ConfigureAwait(false),
            RecentConversations = conversations.Cast<object>().ToList(),
            RecentMessages = messages.Select(item => (object)new
            {
                item.ConversationId,
                item.SortOrder,
                item.Role,
                Content = Truncate(item.Content, 8_000),
                Thinking = TruncateNullable(item.Thinking, 4_000),
                item.IsPositiveFeedback,
                item.FeedbackComment,
                item.CreatedAtUtc
            }).ToList(),
            RecentLogs = logs.Select(item => (object)new
            {
                item.TimestampUtc,
                item.Level,
                item.Category,
                item.EventId,
                item.EventName,
                Message = Truncate(item.Message, 4_000),
                Exception = TruncateNullable(item.Exception, 4_000)
            }).ToList(),
            RecentKnowledge = knowledge.Select(item => (object)new
            {
                item.Id,
                item.Topic,
                item.Scope,
                Content = Truncate(item.Content, 8_000),
                item.Source,
                item.Tags,
                item.Confidence,
                item.VerificationStatus,
                item.ReviewStatus,
                item.IsUserApproved,
                item.UpdatedAtUtc
            }).ToList(),
            RegexPatterns = regexPatterns.Cast<object>().ToList()
        };

        logger.LogInformation(
            "Prepared learning-round snapshot with {ConversationCount} conversations, {MessageCount} messages, {LogCount} logs, {KnowledgeCount} knowledge entries and {RegexCount} regex patterns.",
            snapshot.ConversationCount, snapshot.MessageCount, snapshot.LogCount, snapshot.KnowledgeCount, snapshot.RegexCount);
        return snapshot;
    }

    public async Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var knowledgeIds = new List<Guid>();
        var regexNames = new List<string>();

        foreach (var fact in request.Facts.Take(10_000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fact.Topic) || string.IsNullOrWhiteSpace(fact.Content))
                continue;

            var normalizedTopic = fact.Topic.Trim();
            var normalizedContent = fact.Content.Trim();
            var id = CreateDeterministicGuid($"{fact.Scope}|{normalizedTopic}|{normalizedContent}");
            var entry = new CouncilKnowledgeEntry
            {
                Id = id,
                Topic = normalizedTopic,
                Scope = string.IsNullOrWhiteSpace(fact.Scope) ? "AI Council Learning" : fact.Scope.Trim(),
                Content = normalizedContent,
                HelpfulSources = fact.HelpfulSources?.Trim() ?? string.Empty,
                Source = "AI Council learning self-maintenance",
                Tags = string.IsNullOrWhiteSpace(fact.Tags) ? "learning-round;model-suggested" : fact.Tags.Trim(),
                Confidence = Math.Clamp(fact.Confidence, 0, 100),
                VerificationStatus = "ModelSuggested",
                ReviewStatus = "NeedsUserReview",
                IsUserApproved = false,
                IsPinned = false,
                IsArchived = false,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(90)
            };
            await knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            knowledgeIds.Add(entry.Id);
        }

        foreach (var regex in request.RegexPatterns.Take(10_000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(regex.Name) || string.IsNullOrWhiteSpace(regex.Pattern))
                continue;
            var name = regex.Name.Trim();
            await regexPatternService.AddOrUpdateAsync(new RegexPatternDto(name, regex.Pattern, regex.Flags)).ConfigureAwait(false);
            regexNames.Add(name);
        }

        logger.LogInformation(
            "Learning self-maintenance stored {FactCount} model-suggested fact(s) and {RegexCount} regex pattern(s).",
            knowledgeIds.Count, regexNames.Count);
        return new LearningMaintenanceResult(knowledgeIds.Count, regexNames.Count, knowledgeIds, regexNames);
    }

    private static string Truncate(string? value, int maximumCharacters)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= maximumCharacters ? value : value[..maximumCharacters] + "…";
    }

    private static string? TruncateNullable(string? value, int maximumCharacters) =>
        value is null ? null : Truncate(value, maximumCharacters);

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("LocalGPT.Learning:" + value));
        return new Guid(bytes[..16]);
    }
}
