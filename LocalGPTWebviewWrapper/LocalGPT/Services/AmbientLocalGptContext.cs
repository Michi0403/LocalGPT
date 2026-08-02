using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class AmbientLocalGptContext(ILocalGptVocabularyService vocabulary,
    ILogger<AmbientLocalGptContext> logger)
    : IAmbientLocalGptContext, ILocalHumanInteractionContext, IHumanApprovalExecutionContext
{
    private readonly AsyncLocal<AmbientLocalGptContextHolder?> CurrentHolder = new();
    private readonly AmbientLocalGptContextSnapshot Fallback = new(
        "ambient-unset",
        vocabulary.Get().ActorSystem,
        "LocalGPT",
        Source: "AmbientFallback");

    public AmbientLocalGptContextSnapshot Current => CurrentHolder.Value?.Snapshot ?? Fallback;

    private IDisposable Push(AmbientLocalGptContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var prior = CurrentHolder.Value;
        CurrentHolder.Value = new AmbientLocalGptContextHolder(snapshot);
        var loggingScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["LocalGptCorrelationId"] = snapshot.CorrelationId,
            ["LocalGptActorKind"] = snapshot.ActorKind,
            ["LocalGptAuthorityKind"] = snapshot.AuthorityKind,
            ["LocalGptCouncilRunId"] = snapshot.CouncilRunId,
            ["LocalGptCouncilRound"] = snapshot.CouncilRound,
            ["LocalGptCouncilPhase"] = snapshot.Phase,
            ["LocalGptApprovalRequestId"] = snapshot.ApprovalRequestId,
            ["LocalGptContextSource"] = snapshot.Source
        });
        return new AmbientLocalGptContextPopScope(holder => CurrentHolder.Value = holder, prior, loggingScope);
    }

    public IDisposable PushSystem(string source, string? correlationId = null) => Push(new AmbientLocalGptContextSnapshot(
        NormalizeCorrelationId(correlationId),
        vocabulary.Get().ActorSystem,
        "LocalGPT",
        Source: Normalize(source, 160, "System")));

    public IDisposable PushHumanInteraction(
        Guid humanProfileId,
        string displayName,
        string source,
        string? correlationId = null,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "") => Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId),
            vocabulary.Get().ActorHuman,
            Normalize(displayName, 120, "Human User"),
            vocabulary.Get().AuthorityHumanInteraction,
            humanProfileId,
            councilRunId,
            Math.Max(0, councilRound),
            Normalize(phase, 120),
            Source: Normalize(source, 160, "Local UI")));

    public IDisposable PushHumanApproval(
        Guid humanProfileId,
        string displayName,
        Guid approvalRequestId,
        string source,
        string correlationId,
        Guid? councilRunId = null,
        int councilRound = 0,
        string phase = "") => Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId),
            vocabulary.Get().ActorHuman,
            Normalize(displayName, 120, "Human User"),
            vocabulary.Get().AuthorityHumanApproval,
            humanProfileId,
            councilRunId,
            Math.Max(0, councilRound),
            Normalize(phase, 120),
            approvalRequestId,
            Normalize(source, 160, "Human Collaboration Inbox")));

    public IDisposable PushCouncil(
        Guid councilRunId,
        int councilRound,
        string phase,
        string? correlationId = null) => Push(new AmbientLocalGptContextSnapshot(
            NormalizeCorrelationId(correlationId ?? councilRunId.ToString("N")),
            vocabulary.Get().ActorCouncil,
            "AI Council",
            CouncilRunId: councilRunId,
            CouncilRound: Math.Max(0, councilRound),
            Phase: Normalize(phase, 120),
            Source: "MultiModelCouncilService"));

    private string NormalizeCorrelationId(string? value) =>
        Normalize(value, 180, Guid.NewGuid().ToString("N"));

    private string Normalize(string? value, int maxLength, string fallback = "")
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }


}
