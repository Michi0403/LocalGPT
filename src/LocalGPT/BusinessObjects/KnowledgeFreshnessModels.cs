namespace LocalGPT.BusinessObjects;

/// <summary>Reports that a persisted knowledge entry appears stale and should be reviewed by the local human.</summary>
public sealed class KnowledgeFreshnessReportRequest
{
    /// <summary>
    /// Gets or sets the stable knowledge identifier used to identify or correlate this knowledge freshness report instance with related application state.
    /// </summary>
    /// <value>The knowledge identifier value exposed by <see cref="KnowledgeFreshnessReportRequest"/>.</value>
    public Guid KnowledgeId { get; set; }
    /// <summary>
    /// Gets or sets the reason value that forms part of the knowledge freshness report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="KnowledgeFreshnessReportRequest"/>.</value>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the detected by value that forms part of the knowledge freshness report state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The detected by value exposed by <see cref="KnowledgeFreshnessReportRequest"/>.</value>
    public string DetectedBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this knowledge freshness report state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="KnowledgeFreshnessReportRequest"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this knowledge freshness report instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="KnowledgeFreshnessReportRequest"/>.</value>
    public Guid? CouncilRunId { get; set; }
}

/// <summary>Requests a refresh of one exact source associated with one persisted knowledge entry.</summary>
public sealed class KnowledgeSourceRefreshRequest
{
    /// <summary>
    /// Gets or sets the stable knowledge identifier used to identify or correlate this knowledge source refresh instance with related application state.
    /// </summary>
    /// <value>The knowledge identifier value exposed by <see cref="KnowledgeSourceRefreshRequest"/>.</value>
    public Guid KnowledgeId { get; set; }
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this knowledge source refresh state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="KnowledgeSourceRefreshRequest"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the knowledge source refresh state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="KnowledgeSourceRefreshRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Describes the durable result of stale-knowledge reporting, review, or source refresh.</summary>
public sealed class KnowledgeFreshnessReviewResult
{
    /// <summary>
    /// Gets or sets the stable knowledge identifier used to identify or correlate this knowledge freshness review instance with related application state.
    /// </summary>
    /// <value>The knowledge identifier value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public Guid KnowledgeId { get; set; }
    /// <summary>
    /// Gets or sets the stable collaboration request identifier used to identify or correlate this knowledge freshness review instance with related application state.
    /// </summary>
    /// <value>The collaboration request identifier value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public Guid? CollaborationRequestId { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the knowledge freshness review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source URL that identifies the network or application endpoint associated with this knowledge freshness review state.
    /// </summary>
    /// <value>The source URL value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the knowledge freshness review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote result value that forms part of the knowledge freshness review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The remote result value exposed by <see cref="KnowledgeFreshnessReviewResult"/>.</value>
    public RemoteKnowledgeImportResult? RemoteResult { get; set; }
}
