using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the knowledge rating service contract.
/// </summary>
public interface IKnowledgeRatingService
{
    /// <summary>
    /// Saves rating async.
    /// </summary>
    Task<CouncilKnowledgeUserRating> SaveRatingAsync(CouncilKnowledgeUserRating rating, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets ratings async.
    /// </summary>
    Task<IReadOnlyList<CouncilKnowledgeUserRating>> GetRatingsAsync(Guid knowledgeEntryId, CancellationToken cancellationToken = default);
}
