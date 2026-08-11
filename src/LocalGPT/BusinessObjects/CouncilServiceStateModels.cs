using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ambient local gpt context holder.
/// </summary>
internal sealed class AmbientLocalGptContextHolder(AmbientLocalGptContextSnapshot snapshot)
{
    /// <summary>
    /// Gets or sets snapshot.
    /// </summary>
    public AmbientLocalGptContextSnapshot Snapshot { get; } = snapshot;
}

/// <summary>
/// Represents an ambient local gpt context pop scope.
/// </summary>
internal sealed class AmbientLocalGptContextPopScope(
    Action<AmbientLocalGptContextHolder?> restore,
    AmbientLocalGptContextHolder? prior,
    IDisposable? loggingScope) : IDisposable
{
    private Action<AmbientLocalGptContextHolder?>? restoreAction = restore;
    private AmbientLocalGptContextHolder? priorHolder = prior;
    private IDisposable? activeLoggingScope = loggingScope;

    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
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

/// <summary>
/// Represents a council live session state.
/// </summary>
internal sealed class CouncilLiveSessionState : IDisposable
{
    /// <summary>
    /// Runs the council live session state operation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets sync root.
    /// </summary>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets run identifier.
    /// </summary>
    public Guid RunId { get; }
    /// <summary>
    /// Gets or sets council members.
    /// </summary>
    public IReadOnlyList<string> CouncilMembers { get; }
    /// <summary>
    /// Gets or sets user message.
    /// </summary>
    public string UserMessage { get; }
    /// <summary>
    /// Gets or sets additional user messages.
    /// </summary>
    public List<string> AdditionalUserMessages { get; } = [];
    /// <summary>
    /// Gets or sets transcript.
    /// </summary>
    public StringBuilder Transcript { get; }
    /// <summary>
    /// Gets or sets cancellation.
    /// </summary>
    public CancellationTokenSource Cancellation { get; } = new();
    /// <summary>
    /// Gets or sets started at UTC.
    /// </summary>
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets is running.
    /// </summary>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets or sets status message.
    /// </summary>
    public string StatusMessage { get; set; } = "Preparing Council run.";
    /// <summary>
    /// Stores notification scheduled.
    /// </summary>
    public int NotificationScheduled;

    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
    public void Dispose() => Cancellation.Dispose();
}

/// <summary>
/// Represents a council run state.
/// </summary>
internal sealed class CouncilRunState
{
    /// <summary>
    /// Runs the council run state operation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets sync root.
    /// </summary>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets run identifier.
    /// </summary>
    public Guid RunId { get; }
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public long Revision { get; set; } = 1;
    /// <summary>
    /// Gets or sets participants.
    /// </summary>
    public List<string> Participants { get; set; }
    /// <summary>
    /// Gets or sets model routes.
    /// </summary>
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; }
    /// <summary>
    /// Gets or sets resource load percent.
    /// </summary>
    public int ResourceLoadPercent { get; set; }
    /// <summary>
    /// Gets or sets requested max output tokens.
    /// </summary>
    public int RequestedMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets requested max context tokens.
    /// </summary>
    public int RequestedMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets fallback ollama num gpu.
    /// </summary>
    public int? FallbackOllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets allow parallel hardware roads.
    /// </summary>
    public bool AllowParallelHardwareRoads { get; set; }
    /// <summary>
    /// Gets or sets current round.
    /// </summary>
    public int CurrentRound { get; set; } = -1;
    /// <summary>
    /// Gets or sets current phase.
    /// </summary>
    public string CurrentPhase { get; set; } = "Preparing";
    /// <summary>
    /// Gets or sets is round skip requested.
    /// </summary>
    public bool IsRoundSkipRequested { get; set; }
    /// <summary>
    /// Gets or sets is running.
    /// </summary>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets or sets active lane counts.
    /// </summary>
    public Dictionary<string, int> ActiveLaneCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets change signal.
    /// </summary>
    public TaskCompletionSource<bool> ChangeSignal { get; set; }
    /// <summary>
    /// Gets or sets round cancellation.
    /// </summary>
    public CancellationTokenSource RoundCancellation { get; set; } = new();
}

/// <summary>
/// Represents a council model request lease.
/// </summary>
internal sealed class CouncilModelRequestLease(
    CouncilHardwareRoadPlan plan,
    long revision,
    bool isEnabled,
    Action? release) : ICouncilModelRequestLease
{
    private Action? releaseAction = release;

    /// <summary>
    /// Gets or sets plan.
    /// </summary>
    public CouncilHardwareRoadPlan Plan { get; } = plan;
    /// <summary>
    /// Gets or sets revision.
    /// </summary>
    public long Revision { get; } = revision;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref releaseAction, null)?.Invoke();
}

/// <summary>
/// Represents a council run plan candidate.
/// </summary>
internal sealed record CouncilRunPlanCandidate(
    CouncilHardwareRoadPlan Plan,
    long Revision,
    bool IsEnabled);

/// <summary>
/// Represents a council role runtime assignment.
/// </summary>
internal sealed record CouncilRoleRuntimeAssignment(
    string RoleName,
    OrganicCouncilRoleDefinition? Definition,
    IReadOnlyList<string> AiParticipants)
{
    /// <summary>
    /// Gets or sets human participation mode.
    /// </summary>
    public HumanParticipationMode HumanParticipationMode =>
        Definition?.HumanParticipationMode ?? global::LocalGPT.BusinessObjects.HumanParticipationMode.None;

    /// <summary>
    /// Gets or sets ai selection description.
    /// </summary>
    public string AiSelectionDescription => HumanParticipationMode == global::LocalGPT.BusinessObjects.HumanParticipationMode.HumanOnly
        ? "no AI members (human-only role)"
        : Definition is null || Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected
            ? $"all {AiParticipants.Count} selected AI member(s)"
            : Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels
                ? $"{AiParticipants.Count} provider-bound AI member(s)"
                : $"{AiParticipants.Count} deterministic-random AI member(s)";
}

/// <summary>
/// Represents a council participant pairing.
/// </summary>
internal sealed record CouncilParticipantPairing(
    string RoleName,
    string Participant,
    string PairedRoleName,
    string PairedParticipant);

/// <summary>
/// Represents a configured workflow execution state.
/// </summary>
internal sealed record ConfiguredWorkflowExecutionState(
    int Round,
    int ExpandedStepIndex,
    string PreviousStep,
    string FallbackAnswer,
    string FinalAnswer);
