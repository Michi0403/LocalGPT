namespace LocalGPT.BusinessObjects;

/// <summary>Reports that a persisted knowledge entry appears stale and should be reviewed by the local human.</summary>
public sealed class KnowledgeFreshnessReportRequest
{
    public Guid KnowledgeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DetectedBy { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public Guid? CouncilRunId { get; set; }
}

/// <summary>Requests a refresh of one exact source associated with one persisted knowledge entry.</summary>
public sealed class KnowledgeSourceRefreshRequest
{
    public Guid KnowledgeId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
}

/// <summary>Describes the durable result of stale-knowledge reporting, review, or source refresh.</summary>
public sealed class KnowledgeFreshnessReviewResult
{
    public Guid KnowledgeId { get; set; }
    public Guid? CollaborationRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RemoteKnowledgeImportResult? RemoteResult { get; set; }
}
