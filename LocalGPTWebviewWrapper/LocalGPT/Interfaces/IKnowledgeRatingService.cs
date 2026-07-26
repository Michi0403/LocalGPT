using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IKnowledgeRatingService
{
    Task<CouncilKnowledgeUserRating> SaveRatingAsync(CouncilKnowledgeUserRating rating, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouncilKnowledgeUserRating>> GetRatingsAsync(Guid knowledgeEntryId, CancellationToken cancellationToken = default);
}
