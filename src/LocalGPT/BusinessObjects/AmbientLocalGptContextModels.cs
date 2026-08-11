namespace LocalGPT.BusinessObjects;



/// <summary>
/// Represents an ambient local gpt context snapshot.
/// </summary>
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
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    /// <summary>
    /// Determines whether trusted human interaction.
    /// </summary>
    public bool IsTrustedHumanInteraction(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        (AuthorityKind == vocabulary.AuthorityHumanInteraction || AuthorityKind == vocabulary.AuthorityHumanApproval);

    /// <summary>
    /// Determines whether human approval.
    /// </summary>
    public bool HasHumanApproval(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        AuthorityKind == vocabulary.AuthorityHumanApproval &&
        ApprovalRequestId is not null;
}
