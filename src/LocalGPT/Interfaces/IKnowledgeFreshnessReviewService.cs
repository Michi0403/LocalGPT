using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Coordinates AI-reported stale knowledge with durable human review and exact-source refresh.</summary>
public interface IKnowledgeFreshnessReviewService
{
    Task<KnowledgeFreshnessReviewResult> ReportAsync(KnowledgeFreshnessReportRequest request, CancellationToken cancellationToken = default);
    Task<KnowledgeFreshnessReviewResult> RefreshApprovedSourceAsync(KnowledgeSourceRefreshRequest request, CancellationToken cancellationToken = default);
    Task ApplyHumanDecisionAsync(HumanCollaborationRequest request, HumanDecisionSubmission submission, CancellationToken cancellationToken = default);
}
