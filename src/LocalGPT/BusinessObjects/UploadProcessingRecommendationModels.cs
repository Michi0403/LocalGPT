
namespace LocalGPT.BusinessObjects;

/// <summary>Describes the deterministic processor route available for one quarantined file.</summary>
public sealed class UploadFileProcessingRoute
{
    /// <summary>
    /// Gets or sets the relative path used by this upload file processing route instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="UploadFileProcessingRoute"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the processor value that forms part of the upload file processing route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The processor value exposed by <see cref="UploadFileProcessingRoute"/>.</value>
    public string Processor { get; set; } = "QuarantineOnly";
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this upload file processing route instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="UploadFileProcessingRoute"/>.</value>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether available applies to the upload file processing route state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="UploadFileProcessingRoute"/>.</value>
    public bool IsAvailable { get; set; }
    /// <summary>
    /// Gets or sets the reason value that forms part of the upload file processing route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="UploadFileProcessingRoute"/>.</value>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Request used to build an advisory processing plan for one quarantined upload workspace.</summary>
public sealed class UploadProcessingRecommendationRequest
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="UploadProcessingRecommendationRequest"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user goal value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user goal value exposed by <see cref="UploadProcessingRecommendationRequest"/>.</value>
    public string UserGoal { get; set; } = string.Empty;
}

/// <summary>One user-reviewable recommendation generated from quarantine evidence, approved knowledge and available Council teams.</summary>
public sealed class UploadProcessingRecommendation
{
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the generated at UTC associated with this upload processing recommendation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated at UTC value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the source value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string Source { get; set; } = "DeterministicFallback";
    /// <summary>
    /// Gets or sets the confidence value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public int Confidence { get; set; }
    /// <summary>
    /// Gets or sets the summary value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the recommended action value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommended action value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string RecommendedAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the processing mode value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The processing mode value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string ProcessingMode { get; set; } = "SoloOrTeam";
    /// <summary>
    /// Gets or sets the stable suggested team key used to identify or correlate this upload processing recommendation instance with related application state.
    /// </summary>
    /// <value>The suggested team key value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string SuggestedTeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the suggested team name value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested team name value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string SuggestedTeamName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the suggested configuration value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested configuration value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string SuggestedConfiguration { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the suggested prompt value that forms part of the upload processing recommendation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested prompt value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public string SuggestedPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether requires clarification applies to the upload processing recommendation state.
    /// </summary>
    /// <value>The requires clarification value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public bool RequiresClarification { get; set; }
    /// <summary>
    /// Gets or sets the clarifying questions collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The clarifying questions value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> ClarifyingQuestions { get; set; } = [];
    /// <summary>
    /// Gets or sets the domains collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The domains value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> Domains { get; set; } = [];
    /// <summary>
    /// Gets or sets the project kinds collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The project kinds value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> ProjectKinds { get; set; } = [];
    /// <summary>
    /// Gets or sets the toolchains collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The toolchains value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> Toolchains { get; set; } = [];
    /// <summary>
    /// Gets or sets the repositories collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The repositories value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<RepositoryEvidenceIdentity> Repositories { get; set; } = [];
    /// <summary>
    /// Gets or sets the processing routes collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The processing routes value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<UploadFileProcessingRoute> ProcessingRoutes { get; set; } = [];
    /// <summary>
    /// Gets or sets the evidence collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The evidence value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> Evidence { get; set; } = [];
    /// <summary>
    /// Gets or sets the knowledge references collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The knowledge references value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> KnowledgeReferences { get; set; } = [];
    /// <summary>
    /// Gets or sets the alternative actions collection maintained or exposed by this upload processing recommendation instance for downstream processing.
    /// </summary>
    /// <value>The alternative actions value exposed by <see cref="UploadProcessingRecommendation"/>.</value>
    public List<string> AlternativeActions { get; set; } = [];
}
