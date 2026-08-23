using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates persisted semantic relationships between Council knowledge entries and reusable regex patterns.
/// </summary>
/// <param name="dbContextFactory">Factory used to create short-lived LocalGPT database contexts.</param>
/// <param name="databaseInitializer">Database initialization dependency that guarantees migrations are available before access.</param>
/// <param name="regexCompiler">Regex compilation service used to compile bounded recognition tests through the maintained regex policy.</param>
/// <param name="logger">Logger used to record bounded relationship diagnostics without logging knowledge or regex content.</param>
public sealed class KnowledgeRegexLinkService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IRegexCompilationService regexCompiler,
    ILogger<KnowledgeRegexLinkService> logger) : IKnowledgeRegexLinkService
{
    /// <summary>Loads the enabled and disabled regex semantics assigned to one knowledge note in deterministic presentation order.</summary>
    /// <param name="knowledgeEntryId">Identifier of the Council knowledge note whose regex semantics are requested.</param>
    /// <param name="cancellationToken">Cancellation token that stops database access when the caller no longer needs the result.</param>
    /// <returns>The persisted relationships with their regex navigation populated for display and recognition testing.</returns>
    public async Task<IReadOnlyList<CouncilKnowledgeRegexPatternLink>> GetForKnowledgeAsync(
        Guid knowledgeEntryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (knowledgeEntryId == Guid.Empty)
                return [];

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

            return await db.CouncilKnowledgeRegexPatternLinks
                .AsNoTracking()
                .Include(link => link.RegexPattern)
                .Where(link => link.KnowledgeEntryId == knowledgeEntryId)
                .OrderByDescending(link => link.IsEnabled)
                .ThenBy(link => link.LinkPurpose)
                .ThenBy(link => link.RegexPattern!.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Loading knowledge-to-regex relationships was canceled for {KnowledgeEntryId}.", knowledgeEntryId);
            else
                logger.LogError(exception, "Loading knowledge-to-regex relationships failed for {KnowledgeEntryId}.", knowledgeEntryId);
            throw;
        }
    }

    /// <summary>Evaluates caller-supplied transient text against enabled regex semantics linked to one knowledge note without storing the text.</summary>
    /// <param name="knowledgeEntryId">Identifier of the Council knowledge note that defines the recognition context.</param>
    /// <param name="input">Transient text to test against enabled linked regex patterns.</param>
    /// <param name="cancellationToken">Cancellation token that stops relationship loading or matching when requested.</param>
    /// <returns>The semantic relationships whose maintained regex patterns matched the transient input.</returns>
    public async Task<IReadOnlyList<KnowledgeRegexRecognitionMatch>> TestRecognitionAsync(
        Guid knowledgeEntryId,
        string input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (knowledgeEntryId == Guid.Empty || string.IsNullOrWhiteSpace(input))
                return [];

            var links = await GetForKnowledgeAsync(knowledgeEntryId, cancellationToken).ConfigureAwait(false);
            if (links.Count == 0)
                return [];

            var matches = new List<KnowledgeRegexRecognitionMatch>();
            foreach (var link in links.Where(item => item.IsEnabled).Take(64))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (link.RegexPattern is null || string.IsNullOrWhiteSpace(link.RegexPattern.Pattern))
                    continue;

                try
                {
                    var regex = regexCompiler.Compile(
                        link.RegexPattern.Pattern,
                        link.RegexPattern.Flags,
                        TimeSpan.FromMilliseconds(350),
                        nameof(KnowledgeRegexLinkService));
                    if (!regex.IsMatch(input))
                        continue;

                    matches.Add(new KnowledgeRegexRecognitionMatch(
                        link.RegexPatternId,
                        link.RegexPattern.Name,
                        link.LinkPurpose,
                        link.Meaning));
                }
                catch (RegexMatchTimeoutException exception)
                {
                    logger.LogWarning(
                        exception,
                        "Knowledge recognition pattern {RegexPatternId} timed out and was skipped for {KnowledgeEntryId}.",
                        link.RegexPatternId,
                        knowledgeEntryId);
                }
                catch (ArgumentException exception)
                {
                    logger.LogWarning(
                        exception,
                        "Knowledge recognition pattern {RegexPatternId} is invalid and was skipped for {KnowledgeEntryId}.",
                        link.RegexPatternId,
                        knowledgeEntryId);
                }
            }

            return matches;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Testing knowledge recognition relationships was canceled for {KnowledgeEntryId}.", knowledgeEntryId);
            else
                logger.LogError(exception, "Testing knowledge recognition relationships failed for {KnowledgeEntryId}; input text was omitted from logs.", knowledgeEntryId);
            throw;
        }
    }

    /// <summary>Creates or updates one human-confirmed semantic relationship after validating both persisted endpoints.</summary>
    /// <param name="request">Confirmed relationship values, including semantic purpose, meaning, and enabled state.</param>
    /// <param name="cancellationToken">Cancellation token that stops validation or persistence when requested.</param>
    /// <returns>The persisted relationship with its regex navigation loaded for immediate UI reuse.</returns>
    public async Task<CouncilKnowledgeRegexPatternLink> SaveAsync(
        SaveKnowledgeRegexPatternLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Saving a knowledge-to-regex relationship requires explicit user confirmation.");
            if (request.KnowledgeEntryId == Guid.Empty)
                throw new ArgumentException("A knowledge entry is required.", nameof(request));
            if (request.RegexPatternId <= 0)
                throw new ArgumentException("A regex pattern is required.", nameof(request));

            var purpose = string.IsNullOrWhiteSpace(request.LinkPurpose)
                ? "Classification"
                : request.LinkPurpose.Trim();
            if (purpose.Length > 96)
                purpose = purpose[..96];
            var meaning = request.Meaning?.Trim() ?? string.Empty;
            if (meaning.Length > 1000)
                meaning = meaning[..1000];

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

            if (!await db.CouncilKnowledgeEntries.AnyAsync(
                    entry => entry.Id == request.KnowledgeEntryId,
                    cancellationToken).ConfigureAwait(false))
            {
                throw new KeyNotFoundException($"Knowledge entry {request.KnowledgeEntryId} was not found.");
            }

            if (!await db.RegexPatterns.AnyAsync(
                    pattern => pattern.Id == request.RegexPatternId,
                    cancellationToken).ConfigureAwait(false))
            {
                throw new KeyNotFoundException($"Regex pattern {request.RegexPatternId} was not found.");
            }

            var link = await db.CouncilKnowledgeRegexPatternLinks.SingleOrDefaultAsync(
                    item => item.KnowledgeEntryId == request.KnowledgeEntryId
                        && item.RegexPatternId == request.RegexPatternId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (link is null)
            {
                link = new CouncilKnowledgeRegexPatternLink
                {
                    KnowledgeEntryId = request.KnowledgeEntryId,
                    RegexPatternId = request.RegexPatternId
                };
                db.CouncilKnowledgeRegexPatternLinks.Add(link);
            }

            link.LinkPurpose = purpose;
            link.Meaning = meaning;
            link.LinkedAtUtc = DateTime.UtcNow;
            link.LinkedByHuman = true;
            link.IsEnabled = request.IsEnabled;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await db.Entry(link).Reference(item => item.RegexPattern).LoadAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Saved knowledge-to-regex relationship {KnowledgeEntryId}/{RegexPatternId} with purpose {LinkPurpose}.",
                link.KnowledgeEntryId,
                link.RegexPatternId,
                link.LinkPurpose);
            return link;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving a knowledge-to-regex relationship was canceled.");
            else
                logger.LogError(exception, "Saving a knowledge-to-regex relationship failed; knowledge and regex content were omitted from logs.");
            throw;
        }
    }

    /// <summary>Removes only the caller-confirmed semantic link while preserving both the knowledge note and regex pattern.</summary>
    /// <param name="knowledgeEntryId">Identifier of the linked Council knowledge note.</param>
    /// <param name="regexPatternId">Identifier of the linked reusable regex pattern.</param>
    /// <param name="userConfirmed">Indicates that the user explicitly requested the consequential unlink operation.</param>
    /// <param name="cancellationToken">Cancellation token that stops lookup or persistence when requested.</param>
    /// <returns>A task that completes after the relationship is removed or confirmed absent.</returns>
    public async Task DeleteAsync(
        Guid knowledgeEntryId,
        int regexPatternId,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Removing a knowledge-to-regex relationship requires explicit user confirmation.");
            if (knowledgeEntryId == Guid.Empty || regexPatternId <= 0)
                return;

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);

            var link = await db.CouncilKnowledgeRegexPatternLinks.SingleOrDefaultAsync(
                    item => item.KnowledgeEntryId == knowledgeEntryId && item.RegexPatternId == regexPatternId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (link is null)
                return;

            db.CouncilKnowledgeRegexPatternLinks.Remove(link);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Removed knowledge-to-regex relationship {KnowledgeEntryId}/{RegexPatternId}.",
                knowledgeEntryId,
                regexPatternId);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Removing a knowledge-to-regex relationship was canceled.");
            else
                logger.LogError(exception, "Removing a knowledge-to-regex relationship failed.");
            throw;
        }
    }
}
