using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Builds evidence-bearing AI recommendations for quarantined files without bypassing the ingestion approval gate.</summary>
public interface IUploadProcessingRecommendationService
{
    /// <summary>Builds or refreshes a processing recommendation for one quarantined workspace.</summary>
    Task<UploadProcessingRecommendation> RecommendAsync(UploadProcessingRecommendationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Loads the most recently persisted recommendation for one workspace when available.</summary>
    Task<UploadProcessingRecommendation?> GetAsync(string workspaceName, CancellationToken cancellationToken = default);
}
