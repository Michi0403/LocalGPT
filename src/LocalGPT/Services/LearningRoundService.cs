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
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the learning round workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the learning round workflow to provide the corresponding application capability.</param>
/// <param name="knowledgeService">Council knowledge service dependency used by the learning round workflow to provide the corresponding application capability.</param>
/// <param name="regexPatternService">Regex pattern service dependency used by the learning round workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="projectWorkspaceSync">Learning project workspace sync service dependency used by the learning round workflow to provide the corresponding application capability.</param>
public sealed class LearningRoundService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ICouncilKnowledgeService knowledgeService,
    IRegexPatternService regexPatternService,
    ILearningProjectWorkspaceSyncService projectWorkspaceSync,
    ILogger<LearningRoundService> logger) : ILearningRoundService
{
    /// <summary>
    /// Builds snapshot as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="takePerSource">Take per source value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning round snapshot produced by the operation.</returns>
    public async Task<LearningRoundSnapshot> BuildSnapshotAsync(int takePerSource = 200, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var take = Math.Clamp(takePerSource, 1, 10_000);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

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

            var projects = await db.LocalGptProjects.AsNoTracking()
                .Where(item => !item.IsArchived)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .Take(take)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.ProjectType,
                    item.CurrentVersion,
                    item.RootPath,
                    item.SolutionPath,
                    item.UpdatedAtUtc,
                    CurrentRevision = item.Revisions.Where(revision => revision.IsCurrent).Select(revision => new
                    {
                        revision.Id,
                        revision.RevisionName,
                        revision.SourceSnapshotHash,
                        revision.SourceRootPath,
                        revision.ProjectStructureJson
                    }).FirstOrDefault(),
                    TrackedFileCount = item.TrackedFiles.Count(file => file.Exists)
                })
                .ToListAsync(cancellationToken).ConfigureAwait(false);


            var snapshot = new LearningRoundSnapshot
            {
                GeneratedAtUtc = DateTime.UtcNow,
                ConversationCount = await db.Conversations.CountAsync(cancellationToken).ConfigureAwait(false),
                MessageCount = await db.Messages.CountAsync(cancellationToken).ConfigureAwait(false),
                LogCount = await db.ApplicationLogs.CountAsync(cancellationToken).ConfigureAwait(false),
                KnowledgeCount = await db.CouncilKnowledgeEntries.CountAsync(cancellationToken).ConfigureAwait(false),
                RegexCount = await db.RegexPatterns.CountAsync(cancellationToken).ConfigureAwait(false),
                ProjectCount = await db.LocalGptProjects.CountAsync(item => !item.IsArchived, cancellationToken).ConfigureAwait(false),
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
                RegexPatterns = regexPatterns.Cast<object>().ToList(),
                Projects = projects.Select(item => (object)new
                {
                    item.Id,
                    item.Name,
                    item.ProjectType,
                    item.CurrentVersion,
                    item.RootPath,
                    item.SolutionPath,
                    item.UpdatedAtUtc,
                    item.TrackedFileCount,
                    CurrentRevision = item.CurrentRevision is null ? null : new
                    {
                        item.CurrentRevision.Id,
                        item.CurrentRevision.RevisionName,
                        item.CurrentRevision.SourceSnapshotHash,
                        item.CurrentRevision.SourceRootPath,
                        ProjectStructureJson = Truncate(item.CurrentRevision.ProjectStructureJson, 12_000)
                    }
                }).ToList()
            };

            logger.LogInformation(
                "Prepared learning-round snapshot with {ConversationCount} conversations, {MessageCount} messages, {LogCount} logs, {KnowledgeCount} knowledge entries, {RegexCount} regex patterns and {ProjectCount} source projects.",
                snapshot.ConversationCount, snapshot.MessageCount, snapshot.LogCount, snapshot.KnowledgeCount, snapshot.RegexCount, snapshot.ProjectCount);
            return snapshot;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(BuildSnapshotAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(BuildSnapshotAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs maintain as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The learning maintenance result produced by the operation.</returns>
    public async Task<LearningMaintenanceResult> MaintainAsync(LearningMaintenanceRequest request, CancellationToken cancellationToken = default)
    {
    try
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

            var synchronizedProjects = request.SynchronizeProjectStructure
                ? await projectWorkspaceSync.SynchronizeAsync(request.WorkspaceName, cancellationToken).ConfigureAwait(false)
                : [];
            var synchronizedWorkspace = synchronizedProjects.FirstOrDefault()?.WorkspaceName ?? request.WorkspaceName?.Trim() ?? string.Empty;

            logger.LogInformation(
                "Learning self-maintenance stored {FactCount} model-suggested fact(s), {RegexCount} regex pattern(s), and synchronized {ProjectCount} source project(s).",
                knowledgeIds.Count, regexNames.Count, synchronizedProjects.Count);
            return new LearningMaintenanceResult(knowledgeIds.Count, regexNames.Count, knowledgeIds, regexNames, synchronizedWorkspace, synchronizedProjects);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(MaintainAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(MaintainAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs truncate as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="maximumCharacters">Maximum characters value supplied to the learning round operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Truncate(string? value, int maximumCharacters)
    {
    try
    {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= maximumCharacters ? value : value[..maximumCharacters] + "…";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(Truncate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(Truncate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs truncate nullable as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the learning round operation and used when producing its result.</param>
    /// <param name="maximumCharacters">Maximum characters value supplied to the learning round operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? TruncateNullable(string? value, int maximumCharacters) {
    try
    {
        return value is null ? null : Truncate(value, maximumCharacters);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(TruncateNullable)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(TruncateNullable)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates deterministic GUID as part of the learning round service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the learning round operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
    private Guid CreateDeterministicGuid(string value)
    {
    try
    {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("LocalGPT.Learning:" + value));
            return new Guid(bytes[..16]);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(CreateDeterministicGuid)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearningRoundService)}.{nameof(CreateDeterministicGuid)} failed.");
        throw;
    }
}
}
