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
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalGPT.Components.Pages
{
    public partial class Chat
    {
    private string ActiveChatConfigurationSection { get; set; } = "provider";
    private IReadOnlyList<WorkbenchNavItem> ChatConfigurationSections =>
    [
        new("provider", Localization.Get("Chat.Configuration.Provider", fallback: "Provider"), "Configured AI sessions and per-model properties.", ModelsList.Count.ToString()),
        new("council", Localization.Get("Chat.Configuration.Council", fallback: "AI Council"), "Council members, hosts, hardware roads, presets and team workflow.", CouncilEditorModelNames.Count.ToString()),
        new("memory", Localization.Get("Chat.Configuration.MemoryProjects", fallback: "Memory & projects"), "Saved conversations, project and release context.", SavedConversations.Count.ToString()),
        new("architecture", Localization.Get("Chat.Configuration.Architecture", fallback: "Architecture"), "Optional implementation decisions for the next Council answer.")
    ];

    private Task OnChatConfigurationSectionChangedAsync(string key)
    {
        ActiveChatConfigurationSection = key;
        return Task.CompletedTask;
    }

    /*in razor can render as html..  AllowedFileExtensions="@Catalog.AllowedUploadExtensions"
                                                FileTypeFilter="@Catalog.AllowedUploadMimeTypes"*/
    [Inject]
    IChatClient? ChatClient { get; set; }
    CompositeChatClient? ChatClientProvider => ChatClient as CompositeChatClient;
    DxAIChat? DxAiChat { get; set; }
    List<ChatClientSession> ModelsList => ChatClientProvider?.AvailableChatClients ?? new();
    string SelectedProviderSessionName => ChatClientProvider?.SelectedSession?.Name ?? string.Empty;
    string toastName = "ChatToasts";

    bool ReuseContextWhenSwitching { get; set; } = true;
    bool AutoLoadLatestConversation { get; set; }
    bool UseFreshDiagnosticChat { get; set; }

    string OllamaAccelerationMode { get; set; } = string.Empty;
    int LimitedGpuLayers { get; set; } = 20;
    string OllamaEndpoint { get; set; } = string.Empty;
    bool GenerateCouncilArtifacts { get; set; }
    bool showGameConsole;
    bool IncludeCouncilMemory { get; set; } = true;
    string? DiagnosticRequestedSessionName { get; set; }
    string? DiagnosticOllamaEndpoint { get; set; }
    int CouncilMaxOutputTokens { get; set; }
    int CouncilMaxContextTokens { get; set; }
    int CouncilResourceLoadPercent { get; set; } = 100;
    int CouncilCritiqueRounds { get; set; } = 1;
    bool CouncilAllowParallelHardwareRoads { get; set; } = true;

    /// <summary>
    /// Gets the simple scheduling mode shown to the user for the preparation or currently edited Council run.
    /// </summary>
    string CouncilEditorSchedulingMode => (EditingRunningCouncilConfiguration ? ActiveCouncilAllowParallelHardwareRoads : CouncilAllowParallelHardwareRoads)
        ? "road-parallel"
        : "host-balanced";
    int CouncilEditorMaxParallelModels => EditingRunningCouncilConfiguration
        ? ActiveCouncilMaxParallelModels
        : Math.Max(1, CouncilMaxParallelModels);
    int CouncilEditorModelTimeoutSeconds => EditingRunningCouncilConfiguration
        ? ActiveCouncilModelTimeoutSeconds
        : Math.Clamp(CouncilModelTimeoutSeconds, 30, 1800);

    /// <summary>
    /// Gets a compact explanation of the currently selected Council scheduling policy.
    /// </summary>
    string CouncilSchedulingSummary => CouncilEditorSchedulingMode == "road-parallel"
        ? $"Up to {CouncilEditorMaxParallelModels} request(s) per AI host may run concurrently, still constrained by each model road's lane setting; separate hosts also run independently."
        : "Each AI host stays single-flight while separate machines/providers may run concurrently.";
    int CouncilMaxParallelModels { get; set; } = 1;
    int CouncilModelTimeoutSeconds { get; set; } = 1800;

    string ArchitectureLanguageToolchain { get; set; } = string.Empty;
    string ArchitectureUiStack { get; set; } = string.Empty;
    string ArchitectureSolutionShape { get; set; } = string.Empty;
    string ArchitectureRenderMode { get; set; } = string.Empty;
    string ArchitectureReferenceLook { get; set; } = string.Empty;
    string ArchitecturePollNotes { get; set; } = string.Empty;
    bool AllowCouncilToChooseSandboxDetails { get; set; }
    string ArchitecturePollStatus { get; set; } = string.Empty;
    List<PromptSuggestion> AllPromptSuggestions { get; set; } = new();
    List<PromptSuggestion> PromptSuggestions { get; set; } = new();
    IReadOnlyList<PromptSuggestion> CouncilStarterSuggestions => AllPromptSuggestions
        .Where(item => item.StartsCouncilDirectly && item.IsAvailableForTeam(SelectedCouncilTeamKey))
        .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        .ToList();
    List<ChatMemoryConversationSummary> SavedConversations { get; set; } = new();
    List<ChatMemoryThought> RecentThoughts { get; set; } = new();
    List<MultiModelCouncilModelCandidate> OllamaCandidates { get; set; } = new();
    List<string> SelectedCouncilModelNames { get; set; } = new();
    IReadOnlyList<ProviderModelReference> SelectedCouncilProviderModels => OllamaCandidates
        .Where(candidate => SelectedCouncilModelNames.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
        .Select(candidate => candidate.ToReference())
        .ToList();
    IReadOnlyList<string> UnavailableCouncilSelections => SelectedCouncilModelNames
        .Where(value => new ProviderModelIdentity().LooksProviderQualified(value))
        .Where(value => !OllamaCandidates.Any(candidate => candidate.SelectionKey.Equals(value, StringComparison.OrdinalIgnoreCase)))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    bool IsSelectionEndpointStillConfigured(string selectionKey)
    {
        var identity = new ProviderModelIdentity();
        if (!identity.TryParseSelectionKey(selectionKey, out var reference))
            return false;
        var requestedEndpoint = reference.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
            || reference.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
            ? identity.NormalizeOpenAiCompatibleEndpoint(reference.Endpoint)
            : identity.NormalizeEndpoint(reference.Endpoint);
        return OllamaCandidates.Any(candidate =>
        {
            if (!candidate.IsConfigured || !candidate.ProviderKind.Equals(reference.ProviderKind, StringComparison.OrdinalIgnoreCase))
                return false;
            var candidateEndpoint = candidate.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                || candidate.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                ? identity.NormalizeOpenAiCompatibleEndpoint(candidate.Endpoint)
                : identity.NormalizeEndpoint(candidate.Endpoint);
            return candidateEndpoint.Equals(requestedEndpoint, StringComparison.OrdinalIgnoreCase);
        });
    }

    IReadOnlyList<string> BlockingUnavailableCouncilSelections => UnavailableCouncilSelections
        .Where(value => !IsSelectionEndpointStillConfigured(value))
        .ToList();
    IReadOnlyList<CouncilProviderHostGroup> CouncilProviderHosts => OllamaCandidates
        .GroupBy(candidate => $"{candidate.Provider}|{new ProviderModelIdentity().NormalizeEndpoint(candidate.Endpoint)}", StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            var first = group.First();
            return new CouncilProviderHostGroup(
                group.Key,
                first.Provider,
                new ProviderModelIdentity().GetEndpointLabel(first.Endpoint),
                group.OrderBy(candidate => candidate.ModelName, StringComparer.OrdinalIgnoreCase).ToList());
        })
        .OrderBy(group => group.ProviderName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(group => group.EndpointLabel, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private sealed record CouncilProviderHostGroup(
        string Key,
        string ProviderName,
        string EndpointLabel,
        IReadOnlyList<MultiModelCouncilModelCandidate> Models);

    private string L(string key, string fallback) => Localization.Get(key, fallback: fallback);

    List<string> DiagnosticCouncilModelNames { get; set; } = new();
    List<CouncilModelPreset> ModelPresets { get; set; } = new();
    List<HardwarePerformancePreset> HardwarePerformancePresetItems { get; set; } = new();
    HardwarePerformancePreset? SelectedHardwarePerformancePreset { get; set; }
    string HardwarePerformancePresetName { get; set; } = string.Empty;
    bool isHardwarePerformancePresetBusy;
    string SelectedHardwarePerformancePresetValue => SelectedHardwarePerformancePreset?.Id.ToString() ?? string.Empty;
    List<OneWireCouncilModelRoute> CouncilModelRoutes { get; set; } = [];
    List<OneWireCouncilModelRoute> ActiveCouncilModelRoutes { get; set; } = [];
    List<string> ActiveCouncilConfigurationParticipants { get; set; } = [];
    Guid? ActiveCouncilConfigurationRunId { get; set; }
    long ActiveCouncilConfigurationRevision { get; set; }
    int ActiveCouncilResourceLoadPercent { get; set; } = 100;
    int ActiveCouncilMaxOutputTokens { get; set; }
    int ActiveCouncilMaxContextTokens { get; set; }
    int? ActiveCouncilFallbackOllamaNumGpu { get; set; }
    bool ActiveCouncilAllowParallelHardwareRoads { get; set; } = true;
    int ActiveCouncilMaxParallelModels { get; set; } = 1;
    int ActiveCouncilModelTimeoutSeconds { get; set; } = 1800;
    bool EditingRunningCouncilConfiguration =>
        ActiveCouncilConfigurationRunId is Guid runId &&
        SelectedCouncilRunId == runId &&
        CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    IReadOnlyCollection<string> CouncilEditorModelNames =>
        EditingRunningCouncilConfiguration ? ActiveCouncilConfigurationParticipants : SelectedCouncilModelNames;
    List<OneWireCouncilModelRoute> CouncilEditorRoutes =>
        EditingRunningCouncilConfiguration ? ActiveCouncilModelRoutes : CouncilModelRoutes;
    int CouncilEditorResourceLoadPercent =>
        EditingRunningCouncilConfiguration ? ActiveCouncilResourceLoadPercent : CouncilResourceLoadPercent;
    int CouncilEditorMaxOutputTokens =>
        EditingRunningCouncilConfiguration ? ActiveCouncilMaxOutputTokens : CouncilMaxOutputTokens;
    int CouncilEditorMaxContextTokens =>
        EditingRunningCouncilConfiguration ? ActiveCouncilMaxContextTokens : CouncilMaxContextTokens;
    CouncilModelPreset? SelectedModelPreset { get; set; }
    List<OrganicCouncilTeamDefinition> CouncilTeams { get; set; } = [];
    string SelectedCouncilTeamKey { get; set; } = "general";
    string ModelPresetName { get; set; } = string.Empty;
    bool CreateProjectPerCouncilRun { get; set; } = true;
    bool isModelPresetBusy;
    string SelectedModelPresetValue => SelectedModelPreset?.Id.ToString() ?? string.Empty;
    List<LocalGptProjectSummary> ChatProjects { get; set; } = new();
    LocalGptProjectDetails? SelectedChatProjectDetails { get; set; }
    List<ChatFeedbackTarget> FeedbackTargets { get; set; } = new();
    List<ChatMessageFeedbackSnapshot> SavedFeedback { get; set; } = new();
    ChatMemoryConversationSummary? SelectedConversation { get; set; }
    Guid? ActiveConversationId { get; set; }
    int? SelectedFeedbackSortOrder { get; set; }
    string FeedbackComment { get; set; } = string.Empty;
    string memoryStatus = string.Empty;
    string modelStatus = string.Empty;
    string modelSelectionNotice = string.Empty;
    bool hadUnavailableProviderSelections;
    string feedbackStatus = string.Empty;
    bool isMemoryBusy;
    bool isModelRefreshBusy;
    bool chatConfigurationOpen;
    readonly CancellationTokenSource componentLifetimeCts = new();
    bool autoSaveStarted;
    bool interactiveAttached;
    bool chatControlInitialized;
    bool chatRuntimeStarted;
    bool chatRuntimeActivationScheduled;
    bool isDisposed;
    HumanCollaborationSnapshot collaborationSnapshot = new(new HumanCouncilParticipantProfile(), [], [], []);
    [Parameter]
    [SupplyParameterFromQuery(Name = "rejoinCouncilRunId")]
    public Guid? RequestedRejoinCouncilRunId { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "team")]
    public string? RequestedCouncilTeamKey { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "preset")]
    public string? RequestedModelPresetName { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "starter")]
    public string? RequestedCouncilStarterKey { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "autoStartCouncil")]
    public bool AutoStartCouncilStarter { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "newCouncil")]
    public bool StartNewCouncilChat { get; set; }
    bool directCouncilStarterDispatched;
    bool directCouncilStarterDispatching;
    int directCouncilStarterDispatchAttempts;
    Guid? SelectedCouncilRunId { get; set; }
    Guid? RejoinCouncilRunId { get; set; }
    Guid? AttachedLiveCouncilRunId { get; set; }
    int liveCouncilRefreshScheduled;
    int liveCouncilListRefreshScheduled;
    bool ownsLiveCouncilStream;
    DateTime lastAttachedLiveCouncilUpdatedAtUtc;
    CouncilLiveSessionAttachmentSnapshot? attachedLiveCouncilSnapshot;
    readonly SemaphoreSlim liveCouncilAttachGate = new(1, 1);
    const string LiveCouncilMessageMarkerPrefix = "<!-- localgpt-live-council:";
    string SelectedCouncilRunValue => SelectedCouncilRunId?.ToString() ?? string.Empty;
    /// <summary>
    /// Returns the current independently streamed participant lanes for a live Council run.
    /// </summary>
    IReadOnlyList<CouncilLiveParticipantActivitySnapshot> LiveCouncilParticipantActivities(Guid runId)
    {
        try
        {
            return CouncilLiveSessions.GetParticipantActivities(runId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not read live participant activities for Council run {RunId}.", runId);
            return [];
        }
    }

    private string LiveCouncilTranscript(Guid runId, string fallbackContent)
    {
        var latestTranscript = CouncilLiveSessions.GetTranscript(runId);
        return string.IsNullOrWhiteSpace(latestTranscript) ? fallbackContent : latestTranscript;
    }

    IReadOnlyList<CouncilLiveSessionSummary> RunningLiveCouncilSessions => CouncilLiveSessions.GetActiveSummaries();
    CouncilLiveSessionSummary? ActiveLiveCouncilSession
    {
        get
        {
            Guid?[] candidateRunIds = [AttachedLiveCouncilRunId, RejoinCouncilRunId];
            foreach (var candidateRunId in candidateRunIds)
            {
                if (candidateRunId is Guid runId && CouncilLiveSessions.GetSummary(runId) is { IsRunning: true } snapshot)
                    return snapshot;
            }

            return null;
        }
    }
    bool IsLiveCouncilInteractionAvailable => ActiveLiveCouncilSession?.IsRunning == true;
    bool CanRejoinSelectedCouncilRun =>
        SelectedCouncilRunId is Guid runId && CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    bool IsSelectedCouncilSessionAttached =>
        SelectedCouncilRunId is Guid runId
        && (AttachedLiveCouncilRunId == runId || RejoinCouncilRunId == runId)
        && CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    bool CanJoinSelectedCouncilRun => CanRejoinSelectedCouncilRun && !IsSelectedCouncilSessionAttached;
    Guid? ActiveCouncilInteractionRunId => ActiveCouncilRun?.RunId ?? ActiveLiveCouncilSession?.RunId;
    string ActiveCouncilRunShortId => ActiveCouncilInteractionRunId is Guid runId ? ShortCouncilRunId(runId) : string.Empty;
    string RunningCouncilContribution { get; set; } = string.Empty;
    List<BlazorChatMessage> PendingLiveCouncilUserMessages { get; } = [];
    HumanCouncilRunSnapshot? ActiveCouncilRun
    {
        get
        {
            Guid?[] candidateRunIds = [AttachedLiveCouncilRunId, RejoinCouncilRunId];
            foreach (var candidateRunId in candidateRunIds)
            {
                if (candidateRunId is Guid runId
                    && collaborationSnapshot.ActiveRuns.FirstOrDefault(run => run.RunId == runId) is { } activeRun)
                {
                    return activeRun;
                }
            }

            return null;
        }
    }
    bool autoSaveFailureNotified;
    bool initialModelRefreshStarted;
    bool initialStateInitializationStarted;
    bool initialStateReady;
    bool hasUserSelectedSession;
    string lastSavedSignature = string.Empty;
    DotNetObjectReference<Chat>? chatInteropReference;



    private void ToggleGameConsole() => showGameConsole = !showGameConsole;

    private void CloseGameConsole() => showGameConsole = false;

    protected override Task OnInitializedAsync()
    {
        try
        {
            OllamaAccelerationMode = Catalog.OllamaModeAutoGpu;
            OllamaEndpoint = Catalog.DefaultOllamaEndpoint;
            CouncilMaxOutputTokens = Catalog.DefaultCouncilOutputTokens;
            CouncilMaxContextTokens = Catalog.DefaultCouncilContextTokens;
            if (RequestedRejoinCouncilRunId is Guid requestedRunId)
            {
                SelectedCouncilRunId = requestedRunId;
                RejoinCouncilRunId = requestedRunId;
            }

            HumanCollaboration.Changed += OnHumanCollaborationChanged;
            CouncilLiveSessions.Changed += OnCouncilLiveSessionChanged;
            CouncilRunConfigurations.Changed += OnCouncilRunConfigurationChanged;

            ArchitectureLanguageToolchain = Catalog.ArchitectureLanguageToolchainOptions.FirstOrDefault() ?? string.Empty;
            ArchitectureUiStack = Catalog.ArchitectureUiStackOptions.FirstOrDefault() ?? string.Empty;
            ArchitectureSolutionShape = Catalog.ArchitectureSolutionShapeOptions.FirstOrDefault() ?? string.Empty;
            ArchitectureRenderMode = Catalog.ArchitectureRenderModeOptions.FirstOrDefault() ?? string.Empty;
            ArchitectureReferenceLook = Catalog.ArchitectureReferenceLookOptions.FirstOrDefault() ?? string.Empty;
            AllPromptSuggestions = Catalog.GetSuggestion();
            RefreshPromptSuggestions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in OnInitializedAsync: {Message}", ex);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            initialStateReady = true;
        }

        return Task.CompletedTask;
    }

    private async Task InitializeChatStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RefreshHumanCollaborationAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await LoadDatabaseBackedDefaultsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await LoadPersistentPromptSuggestionsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await LoadCouncilTeamsAsync().ConfigureAwait(false);
            RefreshPromptSuggestions();
            await LoadChatProjectsAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshMemoryAsync().ConfigureAwait(false);
            ApplyDiagnosticQueryOptions(selectSession: false);
            await LoadModelPresetsAsync().ConfigureAwait(false);
            await LoadHardwarePerformancePresetsAsync(cancellationToken).ConfigureAwait(false);
            ApplyDiagnosticQueryOptions(selectSession: true);
            if (UseFreshDiagnosticChat)
            {
                ClearSelectedSessionForFreshStart();
                memoryStatus = "Diagnostic fresh chat: saved memory was not auto-loaded into the selected model.";
            }
            else if (AutoLoadLatestConversation)
            {
                await LoadLatestConversationIntoSessionAsync(loadIntoDxChat: false).ConfigureAwait(false);
            }
            else if (SavedConversations.Count > 0)
            {
                memoryStatus = "Saved memory is available. Click Load latest memory when you want to continue an older chat.";
            }
        }
        catch (OperationCanceledException) when (isDisposed || cancellationToken.IsCancellationRequested)
        {
            Logger.LogDebug("Chat initial state loading was cancelled during component teardown.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chat background initialization failed: {Message}", ex.Message);
            ComponentActivity.RecordFailure(nameof(Chat), nameof(InitializeChatStateAsync), ex);
            Notifier.ShowError(toastName, "Chat state could not be fully initialized. See local application logs for details.", "Chat initialization");
        }
        finally
        {
            initialStateReady = true;
            if (!isDisposed)
            {
                await InvokeAsync(() =>
                {
                    RefreshPromptSuggestions();
                    ScheduleChatRuntimeActivation();
                    StateHasChanged();
                }).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Merges enabled database-owned prompt starters without removing maintained built-in quick prompts.</summary>
    /// <returns>A task that completes after persistent prompt records are merged.</returns>
    private async Task LoadPersistentPromptSuggestionsAsync()
    {
        try
        {
            var records = await FeaturePersistence.GetCouncilPromptStartersAsync(cancellationToken: componentLifetimeCts.Token).ConfigureAwait(false);
            var merged = AllPromptSuggestions.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                IReadOnlyList<string> teamKeys;
                try
                {
                    teamKeys = JsonSerializer.Deserialize<string[]>(record.TeamKeysJson) ?? [];
                }
                catch (JsonException exception)
                {
                    Logger.LogWarning(exception, "Persistent prompt starter {StarterKey} has invalid team-key JSON and was loaded as a generic prompt.", record.Key);
                    teamKeys = [];
                }

                merged[record.Key] = new PromptSuggestion(
                    record.Title,
                    record.Summary,
                    record.PromptMessage,
                    record.Key,
                    teamKeys,
                    record.StartsCouncilDirectly);
            }
            AllPromptSuggestions = merged.Values.ToList();
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Persistent prompt starters were unavailable; maintained built-in quick prompts remain active.");
        }
    }

    private async Task LoadCouncilTeamsAsync()
    {
        var teams = await CouncilTeamConfigurations.GetTeamsAsync(includeDisabled: false).ConfigureAwait(false);
        CouncilTeams = teams.OrderBy(team => team.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        if (CouncilTeams.Count == 0)
            return;
        var requestedTeam = string.IsNullOrWhiteSpace(RequestedCouncilTeamKey) ? null : RequestedCouncilTeamKey.Trim();
        if (requestedTeam is not null && CouncilTeams.Any(team => string.Equals(team.Key, requestedTeam, StringComparison.OrdinalIgnoreCase)))
            SelectedCouncilTeamKey = requestedTeam;
        else if (CouncilTeams.All(team => !string.Equals(team.Key, SelectedCouncilTeamKey, StringComparison.OrdinalIgnoreCase)))
            SelectedCouncilTeamKey = CouncilTeams.FirstOrDefault(team => team.Key == "general")?.Key ?? CouncilTeams[0].Key;
        RefreshPromptSuggestions();
    }

    /// <summary>Applies a user-selected Council team and refreshes its connected pre-prompts.</summary>
    /// <param name="args">Select change event containing the requested team key.</param>
    private void OnCouncilTeamChanged(ChangeEventArgs args)
    {
        var requested = Convert.ToString(args.Value)?.Trim();
        if (!string.IsNullOrWhiteSpace(requested) && CouncilTeams.Any(team => string.Equals(team.Key, requested, StringComparison.OrdinalIgnoreCase)))
            SelectedCouncilTeamKey = requested;
        RefreshPromptSuggestions();
        SavePreparationConfiguration();
    }

    /// <summary>Filters the DevExpress prompt suggestions to generic prompts plus prompts connected to the selected team.</summary>
    private void RefreshPromptSuggestions()
    {
        // Normal quick prompts remain available for every model/session. Team filtering only
        // controls the additional direct Council starters, so selecting a development team
        // never removes the familiar DxAIChat suggestions.
        PromptSuggestions = AllPromptSuggestions
            .Where(item => !item.StartsCouncilDirectly || item.IsAvailableForTeam(SelectedCouncilTeamKey))
            .OrderByDescending(item => item.StartsCouncilDirectly)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task LoadDatabaseBackedDefaultsAsync()
    {
        await MigrateCouncilDefaultsAsync().ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<int>(
            SystemVariables.DefaultMaxOutputTokens,
            value => CouncilMaxOutputTokens = Math.Clamp(
                value,
                Catalog.MinCouncilOutputTokens,
                Catalog.MaxCouncilOutputTokens)).ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<int>(
            SystemVariables.DefaultContextTokens,
            value => CouncilMaxContextTokens = Math.Clamp(
                value,
                Catalog.MinCouncilContextTokens,
                Catalog.MaxCouncilContextTokens)).ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<int>(
            SystemVariables.DefaultHeavyModelGpuLayers,
            value => LimitedGpuLayers = Math.Clamp(value, 1, 99)).ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<int>(
            SystemVariables.DefaultCouncilResourceLoadPercent,
            value => CouncilResourceLoadPercent = Math.Clamp((int)Math.Round(value / 5d) * 5, 0, 100)).ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<int>(
            SystemVariables.DefaultCouncilCritiqueRounds,
            value => CouncilCritiqueRounds = Math.Clamp(value, 0, 3)).ConfigureAwait(false);
        await TryApplyDatabaseVariableAsync<string>(
            SystemVariables.DefaultOllamaEndpoint,
            value => OllamaEndpoint = string.IsNullOrWhiteSpace(value)
                ? Catalog.DefaultOllamaEndpoint
                : value.Trim()).ConfigureAwait(false);
    }

    private async Task MigrateCouncilDefaultsAsync()
    {
        var targetVersion = SystemVariables.CouncilDefaultsVersion.DefaultValue;
        try
        {
            int version;
            try { version = await VariableStore.GetAsync<int>(SystemVariables.CouncilDefaultsVersion.Name).ConfigureAwait(false); }
            catch { version = 0; }
            if (version >= targetVersion) return;

            try
            {
                var previousLoad = await VariableStore.GetAsync<int>(SystemVariables.DefaultCouncilResourceLoadPercent.Name).ConfigureAwait(false);
                if (previousLoad == SystemVariables.LegacyCouncilResourceLoadPercent)
                    await VariableStore.SetAsync(SystemVariables.DefaultCouncilResourceLoadPercent.Name, SystemVariables.DefaultCouncilResourceLoadPercent.DefaultValue).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "The previous Council power default was unavailable during migration.");
            }

            await VariableStore.SetAsync(SystemVariables.CouncilDefaultsVersion.Name, targetVersion).ConfigureAwait(false);
            Logger.LogInformation("Migrated AI Council defaults to version {Version}.", targetVersion);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not migrate AI Council defaults; compiled defaults remain available.");
        }
    }

    private async Task TryApplyDatabaseVariableAsync<T>(SystemVariableDefinition<T> definition, Action<T> apply)
    {
        try
        {
            apply(await VariableStore.GetAsync<T>(definition.Name).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Could not apply database-backed Chat default {definition.Name}; retaining the compiled fallback.");
            ComponentActivity.RecordWarning(nameof(Chat), "ApplyDatabaseDefault", "A database-backed Chat default was unavailable; LocalGPT retained its compiled fallback.");
        }
    }
}
}
