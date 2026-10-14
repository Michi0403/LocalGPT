using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates AI-reported stale knowledge with durable human review and exact-source refresh.</summary>
public interface IKnowledgeFreshnessReviewService
{
    /// <summary>
    /// Performs report as part of the knowledge freshness review service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The knowledge freshness review result produced by the operation.</returns>
    Task<KnowledgeFreshnessReviewResult> ReportAsync(KnowledgeFreshnessReportRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Refreshes approved source as part of the knowledge freshness review service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The knowledge freshness review result produced by the operation.</returns>
    Task<KnowledgeFreshnessReviewResult> RefreshApprovedSourceAsync(KnowledgeSourceRefreshRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Applies human decision as part of the knowledge freshness review service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="submission">Submission value supplied to the knowledge freshness review operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ApplyHumanDecisionAsync(HumanCollaborationRequest request, HumanDecisionSubmission submission, CancellationToken cancellationToken = default);
}
