using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class KnowledgeRatingService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<KnowledgeRatingService> logger) : IKnowledgeRatingService
{
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
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<CouncilKnowledgeUserRating>> GetRatingsAsync(Guid knowledgeEntryId, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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
