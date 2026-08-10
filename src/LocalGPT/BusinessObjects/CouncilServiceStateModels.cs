using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

internal sealed class AmbientLocalGptContextHolder(AmbientLocalGptContextSnapshot snapshot)
{
    public AmbientLocalGptContextSnapshot Snapshot { get; } = snapshot;
}

internal sealed class AmbientLocalGptContextPopScope(
    Action<AmbientLocalGptContextHolder?> restore,
    AmbientLocalGptContextHolder? prior,
    IDisposable? loggingScope) : IDisposable
{
    private Action<AmbientLocalGptContextHolder?>? restoreAction = restore;
    private AmbientLocalGptContextHolder? priorHolder = prior;
    private IDisposable? activeLoggingScope = loggingScope;

    public void Dispose()
    {
        var currentRestore = Interlocked.Exchange(ref restoreAction, null);
        if (currentRestore is null)
            return;

        activeLoggingScope?.Dispose();
        activeLoggingScope = null;
        currentRestore(priorHolder);
        priorHolder = null;
    }
}

internal sealed class CouncilLiveSessionState : IDisposable
{
    public CouncilLiveSessionState(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript)
    {
        RunId = runId;
        CouncilMembers = councilMembers.ToArray();
        UserMessage = userMessage ?? string.Empty;
        Transcript = new StringBuilder(initialTranscript ?? string.Empty);
    }

    public object SyncRoot { get; } = new();
    public Guid RunId { get; }
    public IReadOnlyList<string> CouncilMembers { get; }
    public string UserMessage { get; }
    public List<string> AdditionalUserMessages { get; } = [];
    public StringBuilder Transcript { get; }
    public CancellationTokenSource Cancellation { get; } = new();
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRunning { get; set; } = true;
    public int NotificationScheduled;

    public void Dispose() => Cancellation.Dispose();
}

internal sealed class CouncilRunState
{
    public CouncilRunState(
        Guid runId,
        IEnumerable<string> participants,
        IEnumerable<OneWireCouncilModelRoute> modelRoutes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads)
    {
        RunId = runId;
        Participants = participants.ToList();
        ModelRoutes = modelRoutes.ToList();
        ResourceLoadPercent = Math.Clamp((int)Math.Round(resourceLoadPercent / 5d) * 5, 0, 100);
        RequestedMaxOutputTokens = Math.Max(1, requestedMaxOutputTokens);
        RequestedMaxContextTokens = Math.Max(256, requestedMaxContextTokens);
        FallbackOllamaNumGpu = fallbackOllamaNumGpu;
        AllowParallelHardwareRoads = allowParallelHardwareRoads;
        ChangeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public object SyncRoot { get; } = new();
    public Guid RunId { get; }
    public long Revision { get; set; } = 1;
    public List<string> Participants { get; set; }
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; }
    public int ResourceLoadPercent { get; set; }
    public int RequestedMaxOutputTokens { get; set; }
    public int RequestedMaxContextTokens { get; set; }
    public int? FallbackOllamaNumGpu { get; set; }
    public bool AllowParallelHardwareRoads { get; set; }
    public int CurrentRound { get; set; } = -1;
    public string CurrentPhase { get; set; } = "Preparing";
    public bool IsRoundSkipRequested { get; set; }
    public bool IsRunning { get; set; } = true;
    public Dictionary<string, int> ActiveLaneCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public TaskCompletionSource<bool> ChangeSignal { get; set; }
    public CancellationTokenSource RoundCancellation { get; set; } = new();
}

internal sealed class CouncilModelRequestLease(
    CouncilHardwareRoadPlan plan,
    long revision,
    bool isEnabled,
    Action? release) : ICouncilModelRequestLease
{
    private Action? releaseAction = release;

    public CouncilHardwareRoadPlan Plan { get; } = plan;
    public long Revision { get; } = revision;
    public bool IsEnabled { get; } = isEnabled;

    public void Dispose() => Interlocked.Exchange(ref releaseAction, null)?.Invoke();
}

internal sealed record CouncilRunPlanCandidate(
    CouncilHardwareRoadPlan Plan,
    long Revision,
    bool IsEnabled);

internal sealed record CouncilRoleRuntimeAssignment(
    string RoleName,
    OrganicCouncilRoleDefinition? Definition,
    IReadOnlyList<string> AiParticipants)
{
    public HumanParticipationMode HumanParticipationMode =>
        Definition?.HumanParticipationMode ?? global::LocalGPT.BusinessObjects.HumanParticipationMode.None;

    public string AiSelectionDescription => HumanParticipationMode == global::LocalGPT.BusinessObjects.HumanParticipationMode.HumanOnly
        ? "no AI members (human-only role)"
        : Definition is null || Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected
            ? $"all {AiParticipants.Count} selected AI member(s)"
            : Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels
                ? $"{AiParticipants.Count} provider-bound AI member(s)"
                : $"{AiParticipants.Count} deterministic-random AI member(s)";
}

internal sealed record CouncilParticipantPairing(
    string RoleName,
    string Participant,
    string PairedRoleName,
    string PairedParticipant);

internal sealed record ConfiguredWorkflowExecutionState(
    int Round,
    int ExpandedStepIndex,
    string PreviousStep,
    string FallbackAnswer,
    string FinalAnswer);
