namespace LocalGPT.BusinessObjects;



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
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public bool IsTrustedHumanInteraction(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        (AuthorityKind == vocabulary.AuthorityHumanInteraction || AuthorityKind == vocabulary.AuthorityHumanApproval);

    public bool HasHumanApproval(LocalGptVocabularySnapshot vocabulary) =>
        ActorKind == vocabulary.ActorHuman &&
        AuthorityKind == vocabulary.AuthorityHumanApproval &&
        ApprovalRequestId is not null;
}
