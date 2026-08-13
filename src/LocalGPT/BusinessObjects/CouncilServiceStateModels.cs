using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ambient LocalGPT context holder application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="snapshot">Snapshot value supplied to the ambient LocalGPT context holder operation and used when producing its result.</param>
internal sealed class AmbientLocalGptContextHolder(AmbientLocalGptContextSnapshot snapshot)
{
    /// <summary>
    /// Gets the snapshot value that forms part of the ambient LocalGPT context holder state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snapshot value exposed by <see cref="AmbientLocalGptContextHolder"/>.</value>
    public AmbientLocalGptContextSnapshot Snapshot { get; } = snapshot;
}

/// <summary>
/// Represents an ambient LocalGPT context pop scope application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="restore">Restore value supplied to the ambient LocalGPT context pop scope operation and used when producing its result.</param>
/// <param name="prior">Prior value supplied to the ambient LocalGPT context pop scope operation and used when producing its result.</param>
/// <param name="loggingScope">Disposable dependency used by the ambient LocalGPT context pop scope workflow to provide the corresponding application capability.</param>
internal sealed class AmbientLocalGptContextPopScope(
    Action<AmbientLocalGptContextHolder?> restore,
    AmbientLocalGptContextHolder? prior,
    IDisposable? loggingScope) : IDisposable
{
    /// <summary>
    /// Stores the internal restore action state used by <see cref="AmbientLocalGptContextPopScope"/> while executing its surrounding workflow.
    /// </summary>
    private Action<AmbientLocalGptContextHolder?>? restoreAction = restore;
    /// <summary>
    /// Stores the internal prior holder state used by <see cref="AmbientLocalGptContextPopScope"/> while executing its surrounding workflow.
    /// </summary>
    private AmbientLocalGptContextHolder? priorHolder = prior;
    /// <summary>
    /// Stores the disposable dependency used by <see cref="AmbientLocalGptContextPopScope"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private IDisposable? activeLoggingScope = loggingScope;

    /// <summary>
    /// Releases resources owned by <see cref="AmbientLocalGptContextPopScope"/> and leaves the ambient LocalGPT context pop scope workflow in a safely disposed state.
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
/// Represents council live session state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
internal sealed class CouncilLiveSessionState : IDisposable
{
    /// <summary>
    /// Initializes a new <see cref="CouncilLiveSessionState"/> instance and captures the dependencies or initial state required by its council live session workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="councilMembers">String dependency used by the council live session workflow to provide the corresponding application capability.</param>
    /// <param name="userMessage">User message value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="initialTranscript">Initial transcript value supplied to the council live session operation and used when producing its result.</param>
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
    /// Gets the sync root value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets the stable run identifier used to identify or correlate this council live session instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public Guid RunId { get; }
    /// <summary>
    /// Gets the council members collection maintained or exposed by this council live session instance for downstream processing.
    /// </summary>
    /// <value>The council members value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public IReadOnlyList<string> CouncilMembers { get; }
    /// <summary>
    /// Gets the user message value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user message value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public string UserMessage { get; }
    /// <summary>
    /// Gets the additional user messages collection maintained or exposed by this council live session instance for downstream processing.
    /// </summary>
    /// <value>The additional user messages value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public List<string> AdditionalUserMessages { get; } = [];
    /// <summary>
    /// Gets live participant activity states keyed by the run-local participant/phase identity.
    /// </summary>
    /// <value>The participant activities value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public Dictionary<string, CouncilLiveParticipantActivityState> ParticipantActivities { get; } = new(StringComparer.Ordinal);
    /// <summary>
    /// Gets the transcript value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transcript value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public StringBuilder Transcript { get; }
    /// <summary>
    /// Gets the cancellation signal used to stop or abandon work associated with this council live session operation.
    /// </summary>
    /// <value>The cancellation value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public CancellationTokenSource Cancellation { get; } = new();
    /// <summary>
    /// Gets the started at UTC associated with this council live session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council live session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council live session state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets or sets the status message value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status message value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public string StatusMessage { get; set; } = "Preparing Council run.";
    /// <summary>
    /// Stores the internal notification scheduled state used by <see cref="CouncilLiveSessionState"/> while executing its surrounding workflow.
    /// </summary>
    public int NotificationScheduled;

    /// <summary>
    /// Releases resources owned by <see cref="CouncilLiveSessionState"/> and leaves the council live session workflow in a safely disposed state.
    /// </summary>
    /// <returns>The void dispose cancellation produced by the operation.</returns>
    public void Dispose() => Cancellation.Dispose();
}

/// <summary>
/// Holds one live participant stream separately from the ordered Council transcript so parallel AI hosts remain visible without interleaving provider markup.
/// </summary>
/// <param name="activityKey">Activity key value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="modelName">Model name value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="phase">Phase value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="role">Role value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="routeLabel">Route label value supplied to the council live participant activity operation and used when producing its result.</param>
internal sealed class CouncilLiveParticipantActivityState(
    string activityKey,
    string modelName,
    string phase,
    string role,
    string routeLabel)
{
    /// <summary>
    /// Gets the stable activity key used to identify or correlate this council live participant activity instance with related application state.
    /// </summary>
    /// <value>The activity key value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string ActivityKey { get; } = activityKey;
    /// <summary>
    /// Gets the model name value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string ModelName { get; } = modelName;
    /// <summary>
    /// Gets the phase value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The phase value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string Phase { get; } = phase;
    /// <summary>
    /// Gets the role value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string Role { get; } = role;
    /// <summary>
    /// Gets the route label value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route label value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string RouteLabel { get; } = routeLabel;
    /// <summary>
    /// Gets the content value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public StringBuilder Content { get; } = new();
    /// <summary>Gets or sets the authoritative final participant answer once the model request completes.</summary>
    /// <value>The final content value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string FinalContent { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status message value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status message value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string StatusMessage { get; set; } = "Waiting for the model runtime.";
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council live participant activity state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets the started at UTC associated with this council live participant activity state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council live participant activity state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents council run state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
internal sealed class CouncilRunState
{
    /// <summary>
    /// Initializes a new <see cref="CouncilRunState"/> instance and captures the dependencies or initial state required by its council run workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="participants">String dependency used by the council run workflow to provide the corresponding application capability.</param>
    /// <param name="modelRoutes">One wire council model route dependency used by the council run workflow to provide the corresponding application capability.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council run operation and used when producing its result.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council run operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council run operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council run operation and used when producing its result.</param>
    /// <param name="allowParallelHardwareRoads">Value indicating whether allow parallel hardware roads should apply to this operation.</param>
    /// <param name="maxParallelModels">Max parallel models value supplied to the council run operation and used when producing its result.</param>
    /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the council run operation and used when producing its result.</param>
    public CouncilRunState(
        Guid runId,
        IEnumerable<string> participants,
        IEnumerable<OneWireCouncilModelRoute> modelRoutes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads,
        int maxParallelModels,
        int modelTimeoutSeconds)
    {
        RunId = runId;
        Participants = participants.ToList();
        ModelRoutes = modelRoutes.ToList();
        ResourceLoadPercent = Math.Clamp((int)Math.Round(resourceLoadPercent / 5d) * 5, 0, 100);
        RequestedMaxOutputTokens = Math.Max(1, requestedMaxOutputTokens);
        RequestedMaxContextTokens = Math.Max(256, requestedMaxContextTokens);
        FallbackOllamaNumGpu = fallbackOllamaNumGpu;
        AllowParallelHardwareRoads = allowParallelHardwareRoads;
        MaxParallelModels = Math.Max(1, maxParallelModels);
        ModelTimeoutSeconds = Math.Clamp(modelTimeoutSeconds, 30, 1800);
        ChangeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Gets the sync root value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="CouncilRunState"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets the stable run identifier used to identify or correlate this council run instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="CouncilRunState"/>.</value>
    public Guid RunId { get; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="CouncilRunState"/>.</value>
    public long Revision { get; set; } = 1;
    /// <summary>
    /// Gets or sets the participants collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The participants value exposed by <see cref="CouncilRunState"/>.</value>
    public List<string> Participants { get; set; }
    /// <summary>
    /// Gets or sets the model routes collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The model routes value exposed by <see cref="CouncilRunState"/>.</value>
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; }
    /// <summary>
    /// Gets or sets the resource load percent value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The resource load percent value exposed by <see cref="CouncilRunState"/>.</value>
    public int ResourceLoadPercent { get; set; }
    /// <summary>
    /// Gets or sets the requested max output tokens value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested max output tokens value exposed by <see cref="CouncilRunState"/>.</value>
    public int RequestedMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the requested max context tokens value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested max context tokens value exposed by <see cref="CouncilRunState"/>.</value>
    public int RequestedMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the fallback Ollama num GPU value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fallback Ollama num GPU value exposed by <see cref="CouncilRunState"/>.</value>
    public int? FallbackOllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether parallel hardware roads applies to the council run state.
    /// </summary>
    /// <value>The allow parallel hardware roads value exposed by <see cref="CouncilRunState"/>.</value>
    public bool AllowParallelHardwareRoads { get; set; }
    /// <summary>Gets or sets the per-host Council model request concurrency ceiling used by road-parallel scheduling.</summary>
    /// <value>The max parallel models value exposed by <see cref="CouncilRunState"/>.</value>
    public int MaxParallelModels { get; set; }
    /// <summary>Gets or sets the provider-request timeout in seconds for Council members in this run.</summary>
    /// <value>The model timeout seconds value exposed by <see cref="CouncilRunState"/>.</value>
    public int ModelTimeoutSeconds { get; set; }
    /// <summary>
    /// Gets or sets the current round value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current round value exposed by <see cref="CouncilRunState"/>.</value>
    public int CurrentRound { get; set; } = -1;
    /// <summary>
    /// Gets or sets the current phase value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current phase value exposed by <see cref="CouncilRunState"/>.</value>
    public string CurrentPhase { get; set; } = "Preparing";
    /// <summary>
    /// Gets or sets a value indicating whether round skip requested applies to the council run state.
    /// </summary>
    /// <value>The is round skip requested value exposed by <see cref="CouncilRunState"/>.</value>
    public bool IsRoundSkipRequested { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council run state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilRunState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets the active lane counts collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The active lane counts value exposed by <see cref="CouncilRunState"/>.</value>
    public Dictionary<string, int> ActiveLaneCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets the change signal value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The change signal value exposed by <see cref="CouncilRunState"/>.</value>
    public TaskCompletionSource<bool> ChangeSignal { get; set; }
    /// <summary>
    /// Gets or sets the cancellation signal used to stop or abandon work associated with this council run operation.
    /// </summary>
    /// <value>The round cancellation value exposed by <see cref="CouncilRunState"/>.</value>
    public CancellationTokenSource RoundCancellation { get; set; } = new();
}

/// <summary>
/// Represents a council model request lease application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="plan">Plan value supplied to the council model request lease operation and used when producing its result.</param>
/// <param name="revision">Revision value supplied to the council model request lease operation and used when producing its result.</param>
/// <param name="isEnabled">Value indicating whether is enabled should apply to this operation.</param>
/// <param name="release">Release value supplied to the council model request lease operation and used when producing its result.</param>
internal sealed class CouncilModelRequestLease(
    CouncilHardwareRoadPlan plan,
    long revision,
    bool isEnabled,
    Action? release) : ICouncilModelRequestLease
{
    /// <summary>
    /// Stores the internal release action state used by <see cref="CouncilModelRequestLease"/> while executing its surrounding workflow.
    /// </summary>
    private Action? releaseAction = release;

    /// <summary>
    /// Gets the plan value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plan value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public CouncilHardwareRoadPlan Plan { get; } = plan;
    /// <summary>
    /// Gets the revision value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public long Revision { get; } = revision;
    /// <summary>
    /// Gets a value indicating whether enabled applies to the council model request lease state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// Releases resources owned by <see cref="CouncilModelRequestLease"/> and leaves the council model request lease workflow in a safely disposed state.
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref releaseAction, null)?.Invoke();
}

/// <summary>
/// Represents a council run plan candidate application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Plan">Plan value supplied to the council run plan candidate operation and used when producing its result.</param>
/// <param name="Revision">Revision value supplied to the council run plan candidate operation and used when producing its result.</param>
/// <param name="IsEnabled">Value indicating whether enabled should apply to this operation.</param>
internal sealed record CouncilRunPlanCandidate(
    CouncilHardwareRoadPlan Plan,
    long Revision,
    bool IsEnabled);

/// <summary>
/// Represents a council role runtime assignment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RoleName">Role name value supplied to the council role runtime assignment operation and used when producing its result.</param>
/// <param name="Definition">Definition value supplied to the council role runtime assignment operation and used when producing its result.</param>
/// <param name="AiParticipants">String dependency used by the council role runtime assignment workflow to provide the corresponding application capability.</param>
internal sealed record CouncilRoleRuntimeAssignment(
    string RoleName,
    OrganicCouncilRoleDefinition? Definition,
    IReadOnlyList<string> AiParticipants)
{
    /// <summary>
    /// Gets the human participation mode value that forms part of the council role runtime assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human participation mode value exposed by <see cref="CouncilRoleRuntimeAssignment"/>.</value>
    public HumanParticipationMode HumanParticipationMode =>
        Definition?.HumanParticipationMode ?? global::LocalGPT.BusinessObjects.HumanParticipationMode.None;

    /// <summary>
    /// Gets the AI selection description value that forms part of the council role runtime assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The AI selection description value exposed by <see cref="CouncilRoleRuntimeAssignment"/>.</value>
    public string AiSelectionDescription => HumanParticipationMode == global::LocalGPT.BusinessObjects.HumanParticipationMode.HumanOnly
        ? "no AI members (human-only role)"
        : Definition is null || Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected
            ? $"all {AiParticipants.Count} selected AI member(s)"
            : Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels
                ? $"{AiParticipants.Count} provider-bound AI member(s)"
                : $"{AiParticipants.Count} deterministic-random AI member(s)";
}

/// <summary>
/// Represents a council participant pairing application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RoleName">Role name value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="Participant">Participant value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="PairedRoleName">Paired role name value supplied to the council participant pairing operation and used when producing its result.</param>
/// <param name="PairedParticipant">Paired participant value supplied to the council participant pairing operation and used when producing its result.</param>
internal sealed record CouncilParticipantPairing(
    string RoleName,
    string Participant,
    string PairedRoleName,
    string PairedParticipant);

/// <summary>
/// Represents configured workflow execution state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Round">Round value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="ExpandedStepIndex">Expanded step index value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="PreviousStep">Previous step value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="FallbackAnswer">Fallback answer value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="FinalAnswer">Final answer value supplied to the configured workflow execution operation and used when producing its result.</param>
/// <param name="XDirective">X directive value supplied to the configured workflow execution operation and used when producing its result.</param>
internal sealed record ConfiguredWorkflowExecutionState(
    int Round,
    int ExpandedStepIndex,
    string PreviousStep,
    string FallbackAnswer,
    string FinalAnswer,
    CouncilXRoundDirective? XDirective = null);
