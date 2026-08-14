using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates knowledge rating behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the knowledge rating workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the knowledge rating workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class KnowledgeRatingService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<KnowledgeRatingService> logger) : IKnowledgeRatingService
{
    /// <summary>
    /// Persists rating as part of the knowledge rating service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="rating">Rating value supplied to the knowledge rating operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council knowledge user rating produced by the operation.</returns>
    public async Task<CouncilKnowledgeUserRating> SaveRatingAsync(CouncilKnowledgeUserRating rating, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before rating or approving knowledge.");
            rating.Rating = Math.Clamp(rating.Rating, 0, 100);
            rating.AccuracyStatus = string.IsNullOrWhiteSpace(rating.AccuracyStatus) ? "Unrated" : rating.AccuracyStatus.Trim();
            rating.Notes = rating.Notes?.Trim() ?? string.Empty;
            rating.RatedBy = string.IsNullOrWhiteSpace(rating.RatedBy) ? "Human User" : rating.RatedBy.Trim();
            rating.UpdatedAtUtc = DateTime.UtcNow;

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var knowledge = await db.CouncilKnowledgeEntries.SingleOrDefaultAsync(item => item.Id == rating.KnowledgeEntryId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Knowledge entry {rating.KnowledgeEntryId} was not found.");
            if (rating.Id == Guid.Empty)
                rating.Id = Guid.NewGuid();
            if (rating.CreatedAtUtc == default)
                rating.CreatedAtUtc = DateTime.UtcNow;
            db.CouncilKnowledgeUserRatings.Update(rating);
            knowledge.IsUserApproved = rating.ApprovedForCouncilUse;
            knowledge.ReviewStatus = rating.ApprovedForCouncilUse ? "Current" : "NeedsUserReview";
            knowledge.Confidence = rating.Rating;
            knowledge.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved human rating {RatingId} for knowledge {KnowledgeEntryId}; notes omitted from logs.", rating.Id, rating.KnowledgeEntryId);
            return rating;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(KnowledgeRatingService)}.{nameof(SaveRatingAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(KnowledgeRatingService)}.{nameof(SaveRatingAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves ratings as part of the knowledge rating service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<CouncilKnowledgeUserRating>> GetRatingsAsync(Guid knowledgeEntryId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.CouncilKnowledgeUserRatings.AsNoTracking()
                .Where(item => item.KnowledgeEntryId == knowledgeEntryId)
                .OrderByDescending(item => item.UpdatedAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(KnowledgeRatingService)}.{nameof(GetRatingsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(KnowledgeRatingService)}.{nameof(GetRatingsAsync)} failed.");
        throw;
    }
}
}
