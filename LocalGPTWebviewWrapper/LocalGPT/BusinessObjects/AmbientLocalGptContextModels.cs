namespace LocalGPT.BusinessObjects;

public static class AmbientActorKinds
{
    public const string System = "System";
    public const string Human = "Human";
    public const string AiModel = "AiModel";
    public const string Council = "Council";
    public const string ApiClient = "ApiClient";
}

public static class AmbientAuthorityKinds
{
    public const string None = "None";
    public const string HumanInteraction = "HumanInteraction";
    public const string HumanApproval = "HumanApproval";
}

public sealed record AmbientLocalGptContextSnapshot(
    string CorrelationId,
    string ActorKind,
    string ActorDisplayName,
    string AuthorityKind = AmbientAuthorityKinds.None,
    Guid? HumanProfileId = null,
    Guid? CouncilRunId = null,
    int CouncilRound = 0,
    string Phase = "",
    Guid? ApprovalRequestId = null,
    string Source = "")
{
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public bool IsTrustedHumanInteraction =>
        ActorKind == AmbientActorKinds.Human &&
        AuthorityKind is AmbientAuthorityKinds.HumanInteraction or AmbientAuthorityKinds.HumanApproval;
    public bool HasHumanApproval =>
        ActorKind == AmbientActorKinds.Human &&
        AuthorityKind == AmbientAuthorityKinds.HumanApproval &&
        ApprovalRequestId is not null;
}
