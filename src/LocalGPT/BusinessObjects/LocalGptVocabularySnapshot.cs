namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a LocalGPT vocabulary snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptVocabularySnapshot
{
    /// <summary>
    /// Gets or sets the council spooler running value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler running value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerRunning { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council spooler completed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler completed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council spooler failed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler failed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request feedback value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request feedback value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestFeedback { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request guidance value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request guidance value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestGuidance { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status pending value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status pending value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status approved value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status approved value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusApproved { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status declined value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status declined value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status answered value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status answered value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusAnswered { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status consumed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status consumed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusConsumed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status expired value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status expired value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusExpired { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution queued value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution queued value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionQueued { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution injected value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution injected value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionInjected { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution evaluated value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution evaluated value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionEvaluated { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict pending value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict pending value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict supported value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict supported value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictSupported { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict needs correction value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict needs correction value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictNeedsCorrection { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict mixed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict mixed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictMixed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict not reviewed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict not reviewed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictNotReviewed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred pending approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred pending approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredPendingApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred executing value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred executing value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredExecuting { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred completed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred completed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred failed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred failed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred declined value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred declined value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred completed elsewhere value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred completed elsewhere value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredCompletedElsewhere { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor system value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor system value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorSystem { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor human value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor human value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorHuman { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor AI model value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor AI model value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorAiModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor council value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor council value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorCouncil { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor API client value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor API client value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorApiClient { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority none value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority none value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityNone { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority human interaction value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority human interaction value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityHumanInteraction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority human approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority human approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityHumanApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the catalog DevExpress function value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The catalog DevExpress function value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CatalogDxFunction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the catalog public service method value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The catalog public service method value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CatalogPublicServiceMethod { get; set; } = string.Empty;
}
