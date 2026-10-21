using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using LocalGPT;
using LocalGPT.Components;
using LocalGPT.Components.Layout;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using DevExpress.Blazor.PivotTable;
using DevExpress.Blazor.PdfViewer;
using DevExpress.Blazor.Reporting.Models;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;
using Markdig;
using System.Dynamic;
using System.Globalization;
using LocalGPT.Components.Shared;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace LocalGPT.Components.Shared;

/// <summary>Preserves the existing console component interaction and state lifecycle in an independently reviewable partial declaration; reusable behavior remains delegated to its existing Services.</summary>
public partial class ChatGameConsole
{
    /// <summary>One display/value pair used by the DevExpress selectors inside the shared ASCII wall.</summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ChatGameConsole"/>.</typeparam>
    /// <param name="Value">Value value supplied to the chat game console operation and used when producing its result.</param>
    /// <param name="Label">Label value supplied to the chat game console operation and used when producing its result.</param>
    private sealed record AsciiSelectOption<T>(T Value, string Label);

    /// <summary>Maintained fullscreen scaling choices preserving the labels of the previous native selector.</summary>
    private readonly IReadOnlyList<AsciiSelectOption<string>> FullscreenScaleOptions =
    [
        new("Fit", "Fit whole frame"),
        new("Width", "Fit width + vertical scroll"),
        new("Native", "Native size + scroll")
    ];

    /// <summary>Presentation choices that share the same deterministic game state and controls.</summary>
    private readonly IReadOnlyList<AsciiSelectOption<string>> GameDisplayOptions =
    [
        new("Ascii", "ASCII screen"),
        new("Pixel", "Pixel screen")
    ];

    /// <summary>Maintained game-control choices preserving the existing Human/Shared/AI semantics.</summary>
    private readonly IReadOnlyList<AsciiSelectOption<CouncilGameControlMode>> ControlModeOptions =
    [
        new(CouncilGameControlMode.Human, "Human"),
        new(CouncilGameControlMode.Shared, "Human + AI"),
        new(CouncilGameControlMode.Ai, "AI hunter")
    ];

    /// <summary>Maintained AI autoplay delays exposed through the DevExpress selector.</summary>
    private readonly IReadOnlyList<AsciiSelectOption<int>> AutoplayDelayOptions =
    [
        new(500, "0.5 s"),
        new(1000, "1 s"),
        new(1500, "1.5 s"),
        new(2500, "2.5 s"),
        new(5000, "5 s")
    ];

    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this chat game console instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="ChatGameConsole"/>.</value>
    [Parameter] public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the Council run that may currently be waiting for one hot-seat human move.</summary>
    /// <value>The active Council run identifier associated with this ASCII surface.</value>
    [Parameter] public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable suggested team key used to identify or correlate this chat game console instance with related application state.
    /// </summary>
    /// <value>The suggested team key value exposed by <see cref="ChatGameConsole"/>.</value>
    [Parameter] public string SuggestedTeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the canonical conversation messages mirrored from the normal DXAiChat surface.
    /// </summary>
    /// <value>The same message history the regular chat renderer displays.</value>
    [Parameter] public IReadOnlyList<BlazorChatMessage> ConversationMessages { get; set; } = [];
    /// <summary>
    /// Gets or sets the server-owned Council participant activity lanes that should be projected into the replayable ASCII conversation.
    /// </summary>
    /// <value>Current participant snapshots for the Council run associated with the canonical chat, when one exists.</value>
    [Parameter] public IReadOnlyList<CouncilLiveParticipantActivitySnapshot> CouncilParticipantActivities { get; set; } = [];
    /// <summary>
    /// Gets or sets the selected provider/model display name used to derive a compact terminal nickname.
    /// </summary>
    /// <value>The current assistant session display name.</value>
    [Parameter] public string AssistantDisplayName { get; set; } = "AI";
    /// <summary>
    /// Gets or sets a value indicating whether contextual ASCII fun is enabled for the current chat.
    /// </summary>
    /// <value><c>true</c> when models may optionally add bounded ASCII art, sequences, or game ideas.</value>
    [Parameter] public bool FunModeEnabled { get; set; }
    /// <summary>
    /// Gets or sets the close requested value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The close requested value exposed by <see cref="ChatGameConsole"/>.</value>
    [Parameter] public EventCallback CloseRequested { get; set; }
    /// <summary>Reports whether this terminal currently owns a running game for its chat/Council context.</summary>
    /// <value>Callback used by the Chat page to make game/view availability visible before the user opens the game plane.</value>
    [Parameter] public EventCallback<bool> GameAvailabilityChanged { get; set; }
    /// <summary>Gets or sets the saved chats exposed to the in-terminal Operator session selector.</summary>
    /// <value>Saved conversation summaries available to the current Chat page.</value>
    [Parameter] public IReadOnlyList<ChatMemoryConversationSummary> SavedConversations { get; set; } = [];
    /// <summary>Requests a normal chat prompt from Operator mode.</summary>
    /// <value>Callback used to submit a normal Chat prompt through the owning page.</value>
    [Parameter] public EventCallback<string> ChatPromptRequested { get; set; }
    /// <summary>Requests a Council prompt from Operator mode.</summary>
    /// <value>Callback used to start a Council prompt through the owning page.</value>
    [Parameter] public EventCallback<string> CouncilPromptRequested { get; set; }
    /// <summary>Requests creation of a fresh chat from Operator mode.</summary>
    /// <value>Callback used to create a new chat through the owning page.</value>
    [Parameter] public EventCallback NewSessionRequested { get; set; }
    /// <summary>Requests opening a saved chat selected by id or title from Operator mode.</summary>
    /// <value>Callback used to open the selected saved chat through the owning page.</value>
    [Parameter] public EventCallback<string> SessionOpenRequested { get; set; }
    /// <summary>Requests stopping the active Council run from Operator mode.</summary>
    /// <value>Callback used to stop the active Council run through the owning page.</value>
    [Parameter] public EventCallback CouncilStopRequested { get; set; }
    /// <summary>Requests skipping the current Council round from Operator mode.</summary>
    /// <value>Callback used to skip the current Council round through the owning page.</value>
    [Parameter] public EventCallback CouncilSkipRequested { get; set; }

    /// <summary>
    /// Stores the internal element identifier state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string elementId = $"localgpt-game-{Guid.NewGuid():N}";
    /// <summary>
    /// Stores the internal snapshot state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private CouncilGameSessionSnapshot? snapshot;
    /// <summary>Approved, persistent Game Studio scene revisions available in the regular Chat console.</summary>
    private IReadOnlyList<ProjectGameRevisionChoice> playableRevisions = [];
    /// <summary>Exact persistent project revision selected in the DevExpress list.</summary>
    private ProjectGameRevisionChoice? selectedPlayableRevision;
    /// <summary>Prevents overlapping revision lookup and launch requests.</summary>
    private bool revisionsLoading;
    /// <summary>Bounded, user-visible status for revision browsing and launch failures.</summary>
    private string revisionsStatus = string.Empty;
    /// <summary>Prevents repeated large authored-scene interop on token-stream component updates.</summary>
    private string lastJsAuthoredSceneSignature = string.Empty;
    /// <summary>
    /// Stores the in-memory console output collection maintained internally by <see cref="ChatGameConsole"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<LocalConsoleOutputEvent> consoleOutput = [];
    /// <summary>
    /// Stores the internal reference state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    /// <summary>Owned browser module loaded after the component's first interactive render and released at disposal.</summary>
    private IJSObjectReference? _gameConsoleModule;
    private DotNetObjectReference<ChatGameConsole>? reference;
    /// <summary>
    /// Stores the internal attached state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private bool attached;
    /// <summary>Remembers the last game-availability state reported to the owning Chat page.</summary>
    private bool? lastReportedGameAvailability;
    /// <summary>
    /// Stores whether asynchronous teardown has already started so duplicate renderer disposal calls remain harmless.
    /// </summary>
    private bool disposed;
    /// <summary>Marks that one or more shared-console events arrived since the previous bounded renderer refresh.</summary>
    private int consoleRenderPending;
    /// <summary>Ensures only one delayed shared-console renderer worker runs for this component.</summary>
    private int consoleRenderWorkerRunning;
    /// <summary>Cancels delayed console renderer work when this component is disposed.</summary>
    private readonly CancellationTokenSource consoleRenderCancellation = new();
    /// <summary>Remembers the last game-input state sent to JavaScript so unrelated renders do not repeat interop calls.</summary>
    private bool? lastJsInputEnabled;
    /// <summary>Remembers the last scale mode sent to JavaScript so unrelated renders do not repeat interop calls.</summary>
    private string? lastJsScaleMode;
    /// <summary>Remembers the last renderer/layout payload presented to JavaScript so token-stream renders do not repeatedly force layout and scrolling.</summary>
    private string lastJsLayoutSignature = string.Empty;
    /// <summary>Remembers which Council run supplied the projected human request so ordinary parent renders do not requery collaboration state.</summary>
    private Guid? lastHumanRequestProjectionRunId;
    /// <summary>
    /// Stores the internal show controls state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private bool showControls = true;
    /// <summary>
    /// Stores the internal input in flight state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private bool inputInFlight;
    /// <summary>Stores the current unresolved HumanOnly hot-seat request for the active Council run.</summary>
    private HumanCollaborationRequest? hotSeatRequest;
    /// <summary>Stores the local-human profile needed to resolve the hot-seat request through the normal collaboration service.</summary>
    private HumanCouncilParticipantProfile? hotSeatProfile;
    /// <summary>Stores optional free-text EMOTE input without changing the authoritative game state.</summary>
    private string hotSeatText = string.Empty;
    /// <summary>Prevents duplicate hot-seat submissions while one decision is being persisted.</summary>
    private bool hotSeatSubmitting;
    /// <summary>Stores the next non-hot-seat human collaboration request associated with the active game Council run.</summary>
    private HumanCollaborationRequest? gameInteractionRequest;
    /// <summary>Stores the response text entered directly inside the ASCII game for the current AI/Council request.</summary>
    private string gameInteractionText = string.Empty;
    /// <summary>Prevents duplicate in-game AI/Council request submissions.</summary>
    private bool gameInteractionSubmitting;
    /// <summary>Stores the text used when starting a fresh themed ASCII corridor scenario.</summary>
    private string scenarioPrompt = string.Empty;
    /// <summary>
    /// Stores the internal fullscreen scale mode state used by <see cref="ChatGameConsole"/> while executing its surrounding workflow.
    /// </summary>
    private string fullscreenScaleMode = "Fit";
    /// <summary>Stores whether the deterministic frame is presented as text cells or a pixel-scaled canvas; mechanics remain shared.</summary>
    private string gameDisplayMode = "Ascii";
    /// <summary>Remembers the last display mode sent to JavaScript so unrelated renders do not repeat presentation work.</summary>
    private string? lastJsDisplayMode;
    /// <summary>Remembers whether Operator was opened from the game plane so closing it returns directly to the active game.</summary>
    private bool operatorReturnsToGame;
    /// <summary>Stores whether the LocalGPT meta/operator layer currently owns the shared ASCII surface.</summary>
    private bool operatorMode;
    /// <summary>Stores whether the shared ASCII surface is rendering the canonical chat transcript instead of a game frame.</summary>
    private bool conversationMode = true;
    /// <summary>Stores the current plain-text rendering of the canonical /chat history.</summary>
    private string asciiConversationText = string.Empty;
    /// <summary>Stores the newest bounded ASCII animation frames extracted from the canonical chat history.</summary>
    private IReadOnlyList<string> asciiSequenceFrames = [];
    /// <summary>Remembers the newest animation payload sent to JavaScript so unrelated renders do not restart playback.</summary>
    private string lastJsSequenceSignature = string.Empty;
    /// <summary>Remembers the newest styled game frame sent to JavaScript so unrelated renders do not rebuild safe color spans.</summary>
    private string lastJsFramePresentationSignature = string.Empty;
    /// <summary>Stores whether an operator line is currently being parsed and queued.</summary>
    private bool operatorSubmitting;
    /// <summary>Retains the unsent command text typed into the Matrix operator prompt until the user submits it.</summary>
    private string operatorInput = string.Empty;
    /// <summary>Stores the bounded status returned by the operator command service.</summary>
    private string operatorStatus = "Operator layer ready. Enter :help for LocalGPT meta commands.";
    /// <summary>Stores the shell selected behind LocalGPT's ASCII wall.</summary>
    private LocalConsoleShellKind operatorShell = LocalConsoleShellKind.Auto;
    /// <summary>Stores the working directory retained by this renderer-owned operator session.</summary>
    private string operatorWorkingDirectory = string.Empty;
    /// <summary>Retains the host-resolved shell choices displayed by the embedded operator selector.</summary>
    private IReadOnlyList<LocalConsoleShellDescriptor> availableShells = [];
    /// <summary>Keeps the DevExpress revision list collapsed until the operator explicitly requests it.</summary>
    private bool showRevisionPicker;
    /// <summary>
    /// Gets a value indicating whether show control guide applies to the chat game console state.
    /// </summary>
    /// <value>The show control guide value exposed by <see cref="ChatGameConsole"/>.</value>
    private bool HasHotSeatRequest => GetHasHotSeatRequestForRender();
    /// <summary>Gets whether an AI/Council human request can be answered directly on the active ASCII game surface.</summary>
    /// <value><c>true</c> when a non-role Human Collaboration request is projected into the game.</value>
    private bool HasGameInteractionRequest => GetHasGameInteractionRequestForRender();
    /// <summary>Gets whether the projected game request requires an explicit approve/decline choice.</summary>
    /// <value><c>true</c> when the current projected request uses the approval contract.</value>
    private bool IsApprovalGameInteraction => GetIsApprovalGameInteractionForRender();
    /// <summary>Gets whether this game surface belongs to the Council hot-seat workflow rather than the standalone deterministic corridor.</summary>
    /// <value><c>true</c> when the current game is attached to the configured Council hot-seat workflow.</value>
    private bool IsCouncilHotSeatGame => GetIsCouncilHotSeatGameForRender();
    /// <summary>Gets whether a completed standalone ASCII corridor can be restarted without replacing the owning chat.</summary>
    /// <value><c>true</c> when the completed corridor is standalone and restartable.</value>
    private bool CanRestartCorridor => GetCanRestartCorridorForRender();
    /// <summary>
    /// Gets the selected fullscreen scale option value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected fullscreen scale option value exposed by <see cref="ChatGameConsole"/>.</value>
    private AsciiSelectOption<string> SelectedFullscreenScaleOption =>
        GetSelectedFullscreenScaleOptionForRender();

    /// <summary>
    /// Gets the selected game display option value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected game display option value exposed by <see cref="ChatGameConsole"/>.</value>
    private AsciiSelectOption<string> SelectedGameDisplayOption =>
        GetSelectedGameDisplayOptionForRender();

    /// <summary>
    /// Gets the selected control mode option value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected control mode option value exposed by <see cref="ChatGameConsole"/>.</value>
    private AsciiSelectOption<CouncilGameControlMode> SelectedControlModeOption =>
        GetSelectedControlModeOptionForRender();

    /// <summary>
    /// Gets the selected autoplay delay option value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected autoplay delay option value exposed by <see cref="ChatGameConsole"/>.</value>
    private AsciiSelectOption<int> SelectedAutoplayDelayOption =>
        GetSelectedAutoplayDelayOptionForRender();

    /// <summary>Gets whether the active game should expose its human control guide.</summary>
    /// <value><c>true</c> when controls or in-game collaboration input are currently relevant.</value>
    private bool CanShowControlGuide => GetCanShowControlGuideForRender();
    /// <summary>Gets whether the active game should currently expose its human control guide.</summary>
    /// <value>The show control guide value exposed by <see cref="ChatGameConsole"/>.</value>
    private bool ShowControlGuide => GetShowControlGuideForRender();
    /// <summary>Gets whether the active deterministic frame is the high-resolution Kernel Creature Tournament surface.</summary>
    /// <value>The is kernel tournament value exposed by <see cref="ChatGameConsole"/>.</value>
    private bool IsKernelTournament => GetIsKernelTournamentForRender();
    /// <summary>
    /// Gets a value indicating whether submit human control applies to the chat game console state.
    /// </summary>
    /// <value>The can submit human control value exposed by <see cref="ChatGameConsole"/>.</value>
    private bool CanSubmitHumanControl => GetCanSubmitHumanControlForRender();
    /// <summary>
    /// Gets the input busy state value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input busy state value exposed by <see cref="ChatGameConsole"/>.</value>
    private string InputBusyState => GetInputBusyStateForRender();
    /// <summary>
    /// Gets the console text value that forms part of the chat game console state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The console text value exposed by <see cref="ChatGameConsole"/>.</value>
    private string ConsoleText => GetConsoleTextForRender();
    /// <summary>Gets the bounded command transcript or the Matrix-style idle frame shown when no operator output exists yet.</summary>
    /// <value>Visible operator transcript text that never opens or embeds an external terminal window.</value>
    private string OperatorConsoleText => GetOperatorConsoleTextForRender();
    /// <summary>Gets the header status that identifies whether the ASCII wall currently belongs to the game or operator control plane.</summary>
    /// <value>A compact status line describing the selected shell, active jobs, or current game turn.</value>
    private string OperatorHeaderText => GetOperatorHeaderTextForRender();
    /// <summary>Gets the concise host-shell token rendered beside the Matrix operator command row.</summary>
    /// <value>The selected shell label, using <c>pwsh</c> for the PowerShell backend.</value>
    private string OperatorShellLabel => GetOperatorShellLabelForRender();
    /// <summary>Gets the ARIA busy-state text for an operator submission that is currently being queued.</summary>
    /// <value><c>true</c> while submission is in progress; otherwise <c>false</c>.</value>
    private string OperatorBusyState => GetOperatorBusyStateForRender();
    /// <summary>Gets the visible count of service-owned shell jobs without disclosing any submitted command text.</summary>
    /// <value>A bounded human-readable active-job count for the operator status strip.</value>
    private string ActiveJobText => GetActiveJobTextForRender();
    /// <summary>Chooses the Matrix-style heading that tells the user which shared ASCII plane currently owns the surface: replayable chat, an interactive game, or operator control.</summary>
    /// <value>A compact Matrix-style title for chat, game, or operator mode.</value>
    private string AsciiSurfaceTitle => GetAsciiSurfaceTitleForRender();
    /// <summary>Gets the status line shown below the current ASCII presentation title.</summary>
    /// <value>A concise explanation of the shared chat/game/operator state.</value>
    private string AsciiSurfaceSubtitle => GetAsciiSurfaceSubtitleForRender();

    /// <summary>Gets a compact live workflow hint while the tournament engine is waiting for AI-authored lineup/bracket evidence.</summary>
    /// <value>The game preparation status value exposed by <see cref="ChatGameConsole"/>.</value>
    private string GamePreparationStatus
    {
        get
        {
            if (snapshot is null || !string.Equals(snapshot.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase) || snapshot.Turn > 0)
                return string.Empty;
            if (CouncilParticipantActivities.Count == 0)
                return "Tournament engine ready · waiting for Council lineup activity…";
            var running = CouncilParticipantActivities.Count(activity => activity.IsRunning);
            var finished = CouncilParticipantActivities.Count(activity => !activity.IsRunning && !string.IsNullOrWhiteSpace(activity.FinalContent));
            var current = CouncilParticipantActivities.FirstOrDefault(activity => activity.IsRunning);
            var currentLabel = current is null ? string.Empty : $" · {current.Role}: {current.StatusMessage}";
            return $"Tournament preparation · {finished} completed · {running} active{currentLabel}";
        }
    }

    /// <summary>Reports game availability only when the state actually changes.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ReportGameAvailabilityAsync()
    {
        try
        {
        var available = snapshot is not null && string.Equals(snapshot.Status, "Running", StringComparison.OrdinalIgnoreCase);
        if (lastReportedGameAvailability == available)
            return;
        lastReportedGameAvailability = available;
        if (GameAvailabilityChanged.HasDelegate)
            await GameAvailabilityChanged.InvokeAsync(available).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ReportGameAvailabilityAsync failed.");
            throw;
        }
    }

    /// <summary>Resolves the running game owned by this chat conversation or active Council run without cross-attaching an unrelated session.</summary>
    /// <returns>The correlated running game, or <c>null</c> when this surface has no owned game.</returns>
    private async Task<CouncilGameSessionSnapshot?> ResolveActiveGameAsync()
    {
        try
        {
        if (ConversationId is Guid conversationId)
        {
            var conversationGame = await Games.GetActiveAsync(conversationId).ConfigureAwait(true);
            if (conversationGame is not null)
                return conversationGame;
        }

        if (CouncilRunId is Guid councilRunId)
        {
            var councilGame = string.Equals(SuggestedTeamKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase)
                ? await Games.GetActiveForCouncilRunAsync(councilRunId, "kernel-creature-tournament").ConfigureAwait(true)
                : await Games.GetActiveForCouncilRunAsync(councilRunId).ConfigureAwait(true);
            if (councilGame is not null)
                return councilGame;
        }

        return ConversationId is null && CouncilRunId is null
            ? await Games.GetActiveAsync(null).ConfigureAwait(true)
            : null;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ResolveActiveGameAsync failed.");
            throw;
        }
    }

    /// <summary>Determines whether a game snapshot belongs to the conversation or Council run projected by this terminal.</summary>
    /// <param name="candidate">Candidate game snapshot.</param>
    /// <returns><c>true</c> when the candidate belongs to the current terminal context.</returns>
    private bool MatchesCurrentGameContext(CouncilGameSessionSnapshot candidate)
    {
        try
        {
        if (ConversationId is Guid conversationId && candidate.ConversationId == conversationId)
            return true;
        if (CouncilRunId is Guid councilRunId && candidate.CouncilRunId == councilRunId)
            return true;
        return ConversationId is null && CouncilRunId is null;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.MatchesCurrentGameContext failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the initialized async lifecycle or event notification for <see cref="ChatGameConsole"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnInitializedAsync()
    {
        try
        {
        Games.Changed += OnGameChanged;
        HumanCollaboration.Changed += OnHumanCollaborationChanged;
        Console.Changed += OnConsoleChanged;
        consoleOutput = Console.GetRecentOutput(160);
        availableShells = Operator.GetAvailableShells();
        if (availableShells.Count > 0 && !availableShells.Any(item => item.Shell == operatorShell))
            operatorShell = availableShells[0].Shell;
        snapshot = await ResolveActiveGameAsync().ConfigureAwait(true);
        await RefreshPlayableRevisionsAsync().ConfigureAwait(true);
        await ReportGameAvailabilityAsync().ConfigureAwait(true);
        await RefreshHotSeatRequestAsync().ConfigureAwait(true);
        lastHumanRequestProjectionRunId = CouncilRunId;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.OnInitializedAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the parameters set async lifecycle or event notification for <see cref="ChatGameConsole"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnParametersSetAsync()
    {
        try
        {
        if (snapshot is null || !MatchesCurrentGameContext(snapshot))
            snapshot = await ResolveActiveGameAsync().ConfigureAwait(true);
        await ReportGameAvailabilityAsync().ConfigureAwait(true);

        // Game/operator presentation does not need to rebuild the complete chat transcript for every streamed token.
        // The projection is refreshed on demand when the user switches back to Chat mode.
        if (conversationMode && !operatorMode)
            RefreshConversationProjection();

        if (lastHumanRequestProjectionRunId != CouncilRunId)
        {
            lastHumanRequestProjectionRunId = CouncilRunId;
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
        }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.OnParametersSetAsync failed.");
            throw;
        }
    }

    /// <summary>Rebuilds the canonical ASCII conversation projection only when the Chat plane is actually visible.</summary>
    private void RefreshConversationProjection()
    {
        try
        {
        asciiConversationText = AsciiText.BuildConversationTranscript(ConversationMessages, CouncilParticipantActivities, AssistantDisplayName);
        asciiSequenceFrames = AsciiText.ExtractSequenceFrames(ConversationMessages, CouncilParticipantActivities);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.RefreshConversationProjection failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the after render async lifecycle or event notification for <see cref="ChatGameConsole"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed)
            return;

        try
        {
            if (!attached)
            {
                _gameConsoleModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Shared/ChatGameConsole.razor.js").ConfigureAwait(true) /* interactive, component-owned module */;
                reference ??= DotNetObjectReference.Create(this);
                await _gameConsoleModule!.InvokeVoidAsync("attach", elementId, reference).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                attached = true;
            }

            var inputEnabled = CanSubmitHumanControl && !operatorMode && !conversationMode;
            if (lastJsInputEnabled != inputEnabled)
            {
                await _gameConsoleModule!.InvokeVoidAsync("setEnabled", elementId, inputEnabled).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                lastJsInputEnabled = inputEnabled;
            }
            if (!string.Equals(lastJsScaleMode, fullscreenScaleMode, StringComparison.Ordinal))
            {
                await _gameConsoleModule!.InvokeVoidAsync("setScaleMode", elementId, fullscreenScaleMode).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                lastJsScaleMode = fullscreenScaleMode;
            }
            if (!string.Equals(lastJsDisplayMode, gameDisplayMode, StringComparison.Ordinal))
            {
                await _gameConsoleModule!.InvokeVoidAsync("setDisplayMode", elementId, gameDisplayMode).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                lastJsDisplayMode = gameDisplayMode;
            }

            if (!operatorMode && !conversationMode && snapshot is not null)
            {
                var authoredSceneSignature = snapshot.Scene is null || snapshot.SceneClock is null
                    ? string.Empty : $"{snapshot.Id:N}:{snapshot.SourceRevisionId:N}:{snapshot.SceneClock.Tick}";
                if (!string.Equals(lastJsAuthoredSceneSignature, authoredSceneSignature, StringComparison.Ordinal))
                {
                    await _gameConsoleModule!.InvokeVoidAsync("setAuthoredScene", elementId,
                        snapshot.Scene, snapshot.SceneClock).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                    lastJsAuthoredSceneSignature = authoredSceneSignature;
                }
                var framePresentationSignature = AsciiText.BuildFramePresentationSignature(snapshot);
                if (!string.Equals(lastJsFramePresentationSignature, framePresentationSignature, StringComparison.Ordinal))
                {
                    await _gameConsoleModule!.InvokeVoidAsync("setFramePresentation",
                        elementId,
                        snapshot.FrameText,
                        snapshot.AsciiColorMode.ToString(),
                        snapshot.DefaultForegroundColor,
                        snapshot.DefaultBackgroundColor,
                        snapshot.FrameStyleRuns).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                    lastJsFramePresentationSignature = framePresentationSignature;
                }
            }
            else
            {
                lastJsFramePresentationSignature = string.Empty;
                lastJsAuthoredSceneSignature = string.Empty;
            }

            IReadOnlyList<string> activeSequenceFrames = !operatorMode && conversationMode
                ? asciiSequenceFrames
                : !operatorMode && !conversationMode && snapshot is not null
                    ? snapshot.AnimationFrames
                    : Array.Empty<string>();
            var sequenceTarget = conversationMode ? "conversation" : "game";
            var sequenceDelay = !conversationMode && snapshot is not null ? snapshot.AnimationDelayMilliseconds : 650;
            var sequenceSubtitle = !conversationMode && snapshot is not null ? snapshot.AnimationSubtitle : string.Empty;
            var sequenceSubtitleHold = !conversationMode && snapshot is not null ? snapshot.AnimationSubtitleHoldMilliseconds : 1500;
            var sequenceSignature = activeSequenceFrames.Count > 1
                ? $"{sequenceTarget}:{sequenceDelay}:{sequenceSubtitleHold}:{sequenceSubtitle}:{AsciiText.BuildSequenceSignature(activeSequenceFrames)}:{(snapshot is null ? string.Empty : AsciiText.BuildAnimationPresentationSignature(snapshot))}"
                : string.Empty;
            if (!string.Equals(lastJsSequenceSignature, sequenceSignature, StringComparison.Ordinal))
            {
                await _gameConsoleModule!.InvokeVoidAsync("setSequence",
                    elementId,
                    activeSequenceFrames,
                    sequenceDelay,
                    sequenceTarget,
                    sequenceSubtitle,
                    sequenceSubtitleHold,
                    snapshot?.AsciiColorMode.ToString() ?? CouncilAsciiColorMode.TerminalDefault.ToString(),
                    snapshot?.DefaultForegroundColor ?? 46,
                    snapshot?.DefaultBackgroundColor ?? 0,
                    snapshot?.AnimationFrameStyleRuns ?? [],
                    snapshot?.AnimationSubtitleStyle).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                lastJsSequenceSignature = sequenceSignature;
            }

            var conversationSignature = conversationMode
                ? AsciiText.BuildSequenceSignature([asciiConversationText])
                : string.Empty;
            var layoutSignature = AsciiText.BuildConsoleLayoutSignature(
                operatorMode ? "operator" : conversationMode ? "conversation" : "game",
                $"{fullscreenScaleMode}:{gameDisplayMode}",
                lastJsFramePresentationSignature,
                lastJsSequenceSignature,
                conversationSignature,
                consoleOutput.Count,
                gameInteractionRequest?.Id,
                hotSeatRequest?.Id);
            if (firstRender || !string.Equals(lastJsLayoutSignature, layoutSignature, StringComparison.Ordinal))
            {
                await _gameConsoleModule!.InvokeVoidAsync("refreshLayout", elementId).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                if (operatorMode || conversationMode)
                    await _gameConsoleModule!.InvokeVoidAsync("followTail", elementId).ConfigureAwait(true) /* renderer-affine lifecycle continuation */;
                lastJsLayoutSignature = layoutSignature;
            }
        }
        catch (TaskCanceledException exception)
        {
            ResetJavaScriptAttachmentState();
            Logger.LogDebug(exception, "ASCII game-console JS interop was canceled while the browser circuit was changing state.");
        }
        catch (JSDisconnectedException exception)
        {
            ResetJavaScriptAttachmentState();
            Logger.LogDebug(exception, "ASCII game-console JS interop stopped because the browser circuit disconnected.");
        }
        catch (ObjectDisposedException exception)
        {
            ResetJavaScriptAttachmentState();
            Logger.LogDebug(exception, "ASCII game-console JS interop stopped because browser resources were disposed.");
        }
        catch (Exception exception)
        {
            ResetJavaScriptAttachmentState();
            Logger.LogWarning(exception, "ASCII game-console browser synchronization failed; the next render will retry the bounded attachment state.");
        }
    }

    /// <summary>Clears browser-side presentation caches after a transient circuit/interop failure so a later render can safely reattach.</summary>
    private void ResetJavaScriptAttachmentState()
    {
        try
        {
        attached = false;
        lastJsInputEnabled = null;
        lastJsScaleMode = null;
        lastJsDisplayMode = null;
        lastJsFramePresentationSignature = string.Empty;
        lastJsSequenceSignature = string.Empty;
        lastJsLayoutSignature = string.Empty;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ResetJavaScriptAttachmentState failed.");
            throw;
        }
    }

    /// <summary>Shows the canonical replayable chat transcript on the shared ASCII surface.</summary>
    /// <returns>A task that completes after the renderer switches back to chat presentation.</returns>
    private Task ShowConversationModeAsync()
    {
        try
        {
        operatorMode = false;
        operatorReturnsToGame = false;
        conversationMode = true;
        RefreshConversationProjection();
        lastJsLayoutSignature = string.Empty;
        return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ShowConversationModeAsync failed.");
            throw;
        }
    }

    /// <summary>Shows the active ASCII game without discarding the underlying chat transcript.</summary>
    /// <returns>A task that completes after the renderer switches to the game presentation.</returns>
    private Task ShowGameModeAsync()
    {
        try
        {
        operatorMode = false;
        operatorReturnsToGame = false;
        conversationMode = false;
        return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ShowGameModeAsync failed.");
            throw;
        }
    }

    /// <summary>Toggles the embedded operator layer while keeping game and running-process state alive behind the shared ASCII wall.</summary>
    /// <returns>A task that completes after the renderer has switched modes.</returns>
    private async Task ToggleOperatorModeAsync()
    {
        try
        {
            var enablingOperator = !operatorMode;
            if (enablingOperator)
                operatorReturnsToGame = !conversationMode && snapshot is not null;
            operatorMode = enablingOperator;
            if (operatorMode)
            {
                availableShells = Operator.GetAvailableShells();
                Console.PublishOperatorMessage($"Operator layer enabled with {OperatorShellLabel}. :help lists meta commands.");
            }
            else if (operatorReturnsToGame && snapshot is not null)
            {
                conversationMode = false;
                operatorReturnsToGame = false;
            }
            operatorStatus = operatorMode
                ? "Operator layer enabled. Game input is paused while this command row owns the ASCII wall."
                : "Operator layer hidden. Running jobs continue in the background.";
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Toggling the LocalGPT ASCII operator layer failed.");
        }
    }

    /// <summary>Updates the renderer-owned shell selection from one installed backend descriptor.</summary>
    /// <param name="shell">Stable installed shell kind selected by the operator.</param>
    private void ChangeOperatorShell(LocalConsoleShellKind shell)
    {
        try
        {
            if (availableShells.Any(item => item.Shell == shell))
            {
                operatorShell = shell;
                operatorStatus = $"Selected {OperatorShellLabel}. Ordinary lines run behind the ASCII wall.";
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Changing the LocalGPT ASCII operator shell failed.");
        }
    }

    /// <summary>Submits one human-entered operator line without ever opening an external terminal window.</summary>
    /// <returns>A task that completes after LocalGPT has parsed the line or queued its shell job.</returns>
    private async Task SubmitOperatorLineAsync()
    {
        if (operatorSubmitting || string.IsNullOrWhiteSpace(operatorInput))
            return;

        operatorSubmitting = true;
        var submitted = operatorInput;
        try
        {
            var result = await Operator.SubmitAsync(new LocalConsoleOperatorRequest
            {
                Input = submitted,
                Shell = operatorShell,
                WorkingDirectory = operatorWorkingDirectory,
                UserConfirmed = true
            }).ConfigureAwait(true);

            if (result.ApplicationAction != LocalConsoleOperatorApplicationAction.None)
                await DispatchApplicationOperatorActionAsync(result).ConfigureAwait(true);

            await InvokeAsync(() =>
            {
                operatorShell = result.SelectedShell;
                operatorWorkingDirectory = result.WorkingDirectory;
                operatorStatus = result.Message;
                if (result.Accepted)
                    operatorInput = string.Empty;
                consoleOutput = Console.GetRecentOutput(240);
                StateHasChanged();
            }).ConfigureAwait(true);
        }
        catch (OperationCanceledException exception)
        {
            Logger.LogDebug(exception, "Submitting the LocalGPT ASCII operator line was cancelled.");
            operatorStatus = "Operator request cancelled.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Submitting the LocalGPT ASCII operator line failed; command text was omitted from diagnostics.");
            operatorStatus = "Operator request failed. Review LocalGPT diagnostics.";
        }
        finally
        {
            operatorSubmitting = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

    /// <summary>Dispatches one typed LocalGPT application action already parsed by the service-owned ASCII operator vocabulary.</summary>
    /// <param name="result">Typed operator result containing the application action and bounded argument.</param>
    /// <returns>A task that completes when the requested application action has been handed to its owning UI/service boundary.</returns>
    private async Task DispatchApplicationOperatorActionAsync(LocalConsoleOperatorResult result)
    {
        try
        {
            switch (result.ApplicationAction)
            {
                case LocalConsoleOperatorApplicationAction.ChatPrompt:
                    await InvokeAsync(() => ChatPromptRequested.InvokeAsync(result.ApplicationArgument)).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.CouncilPrompt:
                    await InvokeAsync(() => CouncilPromptRequested.InvokeAsync(result.ApplicationArgument)).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.CouncilStatus:
                    var running = CouncilParticipantActivities.Count(item => item.IsRunning);
                    Console.PublishOperatorMessage(CouncilRunId is Guid runId
                        ? $"Council {runId:N}: {running}/{CouncilParticipantActivities.Count} participant lane(s) currently running."
                        : "No Council run is attached to the current ASCII surface.");
                    break;
                case LocalConsoleOperatorApplicationAction.CouncilStop:
                    await InvokeAsync(() => CouncilStopRequested.InvokeAsync()).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.CouncilSkip:
                    await InvokeAsync(() => CouncilSkipRequested.InvokeAsync()).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.SessionList:
                    Console.PublishOperatorMessage(Operator.FormatSavedConversations(SavedConversations));
                    break;
                case LocalConsoleOperatorApplicationAction.SessionNew:
                    await InvokeAsync(() => NewSessionRequested.InvokeAsync()).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.SessionOpen:
                    await InvokeAsync(() => SessionOpenRequested.InvokeAsync(result.ApplicationArgument)).ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.GameStartCorridor:
                    await StartDoomAsync().ConfigureAwait(true);
                    operatorMode = false;
                    conversationMode = false;
                    break;
                case LocalConsoleOperatorApplicationAction.GameStartDragon:
                    await StartDragonAsync().ConfigureAwait(true);
                    operatorMode = false;
                    conversationMode = false;
                    break;
                case LocalConsoleOperatorApplicationAction.GameStartProject:
                    snapshot = await GameProjects.LaunchBySelectorAsync(result.ApplicationArgument, new LaunchProjectGameRequest
                    {
                        ConversationId = ConversationId,
                        CouncilRunId = CouncilRunId,
                        StartedBy = "ASCII Operator"
                    }).ConfigureAwait(true);
                    showControls = true;
                    operatorMode = false;
                    conversationMode = false;
                    await RefreshHotSeatRequestAsync().ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.GameStatus:
                    Console.PublishOperatorMessage(snapshot is null
                        ? "No ASCII game is active."
                        : $"Game {snapshot.DisplayName}: status={snapshot.Status}; turn={snapshot.Turn}; owner={snapshot.CurrentTurnOwner}; control={snapshot.ControlMode}.");
                    break;
                case LocalConsoleOperatorApplicationAction.GameEnd:
                    if (snapshot is not null)
                        snapshot = await Games.EndAsync(snapshot.Id, "ASCII Operator").ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.ModeChat:
                    operatorMode = false;
                    conversationMode = true;
                    break;
                case LocalConsoleOperatorApplicationAction.ModeGame:
                    operatorMode = false;
                    conversationMode = snapshot is null;
                    break;
                case LocalConsoleOperatorApplicationAction.ModeOperator:
                    operatorMode = true;
                    break;
                case LocalConsoleOperatorApplicationAction.Fullscreen:
                    fullscreenScaleMode = result.ApplicationArgument;
                    await FullscreenAsync().ConfigureAwait(true);
                    break;
                case LocalConsoleOperatorApplicationAction.None:
                default:
                    break;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Dispatching a parsed LocalGPT ASCII operator application action failed.");
            throw;
        }
    }

    /// <summary>Expands the revision editor only on demand, preserving the compact console command bar.</summary>
    private void ToggleRevisionPicker()
    {
        try
        {
            showRevisionPicker = !showRevisionPicker;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Toggling the Game Studio revision picker failed.");
            throw;
        }
    }

    /// <summary>Reloads persistent approved game revisions through the project domain service.</summary>
    /// <returns>The completed revision-list lookup.</returns>
    private async Task RefreshPlayableRevisionsAsync()
    {
        if (revisionsLoading)
            return;
        revisionsLoading = true;
        try
        {
            var previous = selectedPlayableRevision?.RevisionId;
            playableRevisions = await GameProjects.ListPlayableRevisionsAsync().ConfigureAwait(true);
            selectedPlayableRevision = playableRevisions.FirstOrDefault(item => item.RevisionId == previous)
                ?? playableRevisions.FirstOrDefault();
            revisionsStatus = playableRevisions.Count == 0
                ? "No completed playable Game Studio revisions yet."
                : $"{playableRevisions.Count} approved revisions ready to play.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Refreshing approved Game Studio revisions in Chat failed.");
            revisionsStatus = "Could not load Game Studio revisions; retry Refresh.";
        }
        finally
        {
            revisionsLoading = false;
        }
    }

    /// <summary>Selects a durable revision without starting or mutating any game.</summary>
    /// <param name="choice">Project/revision identity selected in DevExpress ListBox.</param>
    private void SelectPlayableRevision(ProjectGameRevisionChoice choice)
    {
        try
        {
            selectedPlayableRevision = choice;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting an approved Game Studio revision failed.");
            throw;
        }
    }

    /// <summary>Starts an explicitly selected approved revision through the shared session service.</summary>
    /// <returns>The completed launch and chat display handoff.</returns>
    private async Task StartSelectedRevisionAsync()
    {
        if (revisionsLoading || selectedPlayableRevision is null)
            return;
        revisionsLoading = true;
        try
        {
            var chosen = selectedPlayableRevision;
            var started = await GameProjects.LaunchRevisionAsync(chosen.ProjectId, chosen.RevisionId,
                new LaunchProjectGameRequest
                {
                    ConversationId = ConversationId,
                    CouncilRunId = CouncilRunId,
                    StartedBy = "Human User"
                }).ConfigureAwait(true);
            snapshot = started;
            revisionsStatus = $"Playing {chosen.DisplayName}.";
            showControls = true;
            operatorMode = false;
            conversationMode = false;
            await ReportGameAvailabilityAsync().ConfigureAwait(true);
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Starting the selected Game Studio revision in Chat failed.");
            revisionsStatus = "The selected revision could not be started. Check game diagnostics.";
        }
        finally
        {
            revisionsLoading = false;
        }
    }

    /// <summary>
    /// Starts doom for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartDoomAsync()
    {
        try
        {
            await StartAsync("ascii-doom", "ascii-doom-council-adventure").ConfigureAwait(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.StartDoomAsync failed.");
            throw;
        }
    }
    /// <summary>
    /// Starts dragon for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartDragonAsync()
    {
        try
        {
            await StartAsync("green-dragon", "green-dragon-runtime-story").ConfigureAwait(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.StartDragonAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs start for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the chat game console operation and used when producing its result.</param>
    /// <param name="teamKey">Team key value supplied to the chat game console operation and used when producing its result.</param>
    /// <param name="mapSeed">Optional deterministic map seed used when replaying a corridor.</param>
    /// <param name="scenario">Optional bounded scenario prompt supplied to the game runtime.</param>
    /// <param name="campaignRuntimeClassKey">Optional copied campaign runtime-class key.</param>
    /// <param name="startingLevel">Optional 1-based level override.</param>
    /// <param name="autoAdvanceLevels">Optional level auto-advance override.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartAsync(
        string gameKey,
        string teamKey,
        int? mapSeed = null,
        string scenario = "",
        string campaignRuntimeClassKey = "",
        int? startingLevel = null,
        bool? autoAdvanceLevels = null)
    {
        try
        {
            snapshot = await Games.StartAsync(new StartCouncilGameRequest
            {
                GameKey = gameKey,
                TeamKey = string.IsNullOrWhiteSpace(SuggestedTeamKey) ? teamKey : SuggestedTeamKey,
                ConversationId = ConversationId,
                CouncilRunId = CouncilRunId,
                ControlMode = CouncilGameControlMode.Shared,
                MapSeed = mapSeed,
                ScenarioPrompt = scenario,
                CampaignRuntimeClassKey = campaignRuntimeClassKey,
                StartingLevel = startingLevel,
                AutoAdvanceLevels = autoAdvanceLevels,
                StartedBy = "Human User"
            }).ConfigureAwait(true);
            showControls = true;
            operatorMode = false;
            conversationMode = false;
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not start the in-chat game console.");
        }
    }

    /// <summary>Restarts the completed ASCII corridor with the exact same deterministic map seed.</summary>
    /// <returns>A task that completes when the replay session has started.</returns>
    private Task RestartSameMapAsync()
    {
        try
        {
            return snapshot is null
        ? Task.CompletedTask
        : StartAsync(
            "ascii-doom",
            snapshot.TeamKey,
            snapshot.CampaignSeed > 0 ? snapshot.CampaignSeed : snapshot.MapSeed,
            snapshot.ScenarioPrompt,
            snapshot.CampaignRuntimeClassKey,
            snapshot.CurrentLevel,
            snapshot.AutoAdvanceLevels);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.RestartSameMapAsync failed.");
            throw;
        }
    }

    /// <summary>Starts a newly seeded corridor while preserving the current chat and selected team.</summary>
    /// <returns>A task that completes when the newly seeded corridor has started.</returns>
    private Task StartFreshMapAsync()
    {
        try
        {
            return StartAsync(
        "ascii-doom",
        snapshot?.TeamKey ?? "ascii-doom-council-adventure",
        campaignRuntimeClassKey: snapshot?.CampaignRuntimeClassKey ?? string.Empty,
        startingLevel: snapshot?.CurrentLevel,
        autoAdvanceLevels: snapshot?.AutoAdvanceLevels);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.StartFreshMapAsync failed.");
            throw;
        }
    }

}
