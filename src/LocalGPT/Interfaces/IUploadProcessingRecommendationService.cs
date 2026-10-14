using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Builds evidence-bearing AI recommendations for quarantined files without bypassing the ingestion approval gate.</summary>
public interface IUploadProcessingRecommendationService
{
    /// <summary>Builds or refreshes a processing recommendation for one quarantined workspace.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The upload processing recommendation produced by the operation.</returns>
    Task<UploadProcessingRecommendation> RecommendAsync(UploadProcessingRecommendationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Loads the most recently persisted recommendation for one workspace when available.</summary>
    /// <param name="workspaceName">Workspace name value supplied to the upload processing recommendation operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The upload processing recommendation produced by the operation.</returns>
    Task<UploadProcessingRecommendation?> GetAsync(string workspaceName, CancellationToken cancellationToken = default);
}
