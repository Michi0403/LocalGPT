namespace LocalGPT.BusinessObjects;



/// <summary>
/// Represents an ambient LocalGPT context snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="CorrelationId">Identifier of the correlation to use for this operation.</param>
/// <param name="ActorKind">Actor kind value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
/// <param name="ActorDisplayName">Actor display name value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
/// <param name="AuthorityKind">Authority kind value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
/// <param name="HumanProfileId">Identifier of the human profile to use for this operation.</param>
/// <param name="CouncilRunId">Identifier of the council run to use for this operation.</param>
/// <param name="CouncilRound">Council round value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
/// <param name="Phase">Phase value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
/// <param name="ApprovalRequestId">Identifier of the approval request to use for this operation.</param>
/// <param name="Source">Source value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
public sealed record AmbientLocalGptContextSnapshot(
    string CorrelationId,
    string ActorKind,
    string ActorDisplayName,
    string AuthorityKind = "",
    Guid? HumanProfileId = null,
    Guid? CouncilRunId = null,
    int CouncilRound = 0,
    string Phase = "",
    Guid? ApprovalRequestId = null,
    string Source = "")
{
    /// <summary>
    /// Gets or sets the created at UTC associated with this ambient LocalGPT context snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="AmbientLocalGptContextSnapshot"/>.</value>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    /// <summary>
    /// Determines whether trusted human interaction for <see cref="AmbientLocalGptContextSnapshot"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context snapshot workflow.
    /// </summary>
    /// <param name="vocabulary">Vocabulary value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsTrustedHumanInteraction(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        (AuthorityKind == vocabulary.AuthorityHumanInteraction || AuthorityKind == vocabulary.AuthorityHumanApproval);

    /// <summary>
    /// Determines whether human approval for <see cref="AmbientLocalGptContextSnapshot"/>, keeping the operation consistent with the state and invariants of the surrounding ambient LocalGPT context snapshot workflow.
    /// </summary>
    /// <param name="vocabulary">Vocabulary value supplied to the ambient LocalGPT context snapshot operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool HasHumanApproval(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        AuthorityKind == vocabulary.AuthorityHumanApproval &&
        ApprovalRequestId is not null;
}
