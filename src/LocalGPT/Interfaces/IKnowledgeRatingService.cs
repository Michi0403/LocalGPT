using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for knowledge rating behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IKnowledgeRatingService
{
    /// <summary>
    /// Persists rating as part of the knowledge rating service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="rating">Rating value supplied to the knowledge rating operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council knowledge user rating produced by the operation.</returns>
    Task<CouncilKnowledgeUserRating> SaveRatingAsync(CouncilKnowledgeUserRating rating, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves ratings as part of the knowledge rating service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="knowledgeEntryId">Identifier of the knowledge entry to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<CouncilKnowledgeUserRating>> GetRatingsAsync(Guid knowledgeEntryId, CancellationToken cancellationToken = default);
}
