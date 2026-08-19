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
    /// <summary>
    /// Renders the chat Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
    /// </summary>
    public partial class Chat
    {
    /// <summary>
    /// Gets or sets the active chat configuration section value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active chat configuration section value exposed by <see cref="Chat"/>.</value>
    private string ActiveChatConfigurationSection { get; set; } = "provider";
    /// <summary>
    /// Gets the chat configuration sections collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The chat configuration sections value exposed by <see cref="Chat"/>.</value>
    private IReadOnlyList<WorkbenchNavItem> ChatConfigurationSections =>
    [
        new("provider", Localization.Get("Chat.Configuration.Provider", fallback: "Provider"), "Configured AI sessions and per-model properties.", ModelsList.Count.ToString()),
        new("council", Localization.Get("Chat.Configuration.Council", fallback: "AI Council"), "Council members, hosts, hardware roads, presets and team workflow.", CouncilEditorModelNames.Count.ToString()),
        new("memory", Localization.Get("Chat.Configuration.MemoryProjects", fallback: "Memory & projects"), "Saved conversations, project and release context.", SavedConversations.Count.ToString()),
        new("architecture", Localization.Get("Chat.Configuration.Architecture", fallback: "Architecture"), "Optional implementation decisions for the next Council answer.")
    ];

    /// <summary>
    /// Handles the chat configuration section changed async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task OnChatConfigurationSectionChangedAsync(string key)
    {
        ActiveChatConfigurationSection = key;
        return Task.CompletedTask;
    }

    /*in razor can render as html..  AllowedFileExtensions="@Catalog.AllowedUploadExtensions"
                                                FileTypeFilter="@Catalog.AllowedUploadMimeTypes"*/
    /// <summary>
    /// Gets or sets the chat client value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat client value exposed by <see cref="Chat"/>.</value>
    [Inject]
    IChatClient? ChatClient { get; set; }
    /// <summary>
    /// Gets the chat client provider value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat client provider value exposed by <see cref="Chat"/>.</value>
    CompositeChatClient? ChatClientProvider => ChatClient as CompositeChatClient;
    /// <summary>
    /// Gets or sets the DevExpress AI chat value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The DevExpress AI chat value exposed by <see cref="Chat"/>.</value>
    DxAIChat? DxAiChat { get; set; }
    /// <summary>
    /// Gets the models list collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The models list value exposed by <see cref="Chat"/>.</value>
    List<ChatClientSession> ModelsList => ChatClientProvider?.AvailableChatClients ?? new();
    /// <summary>
    /// Gets the selected provider session name value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected provider session name value exposed by <see cref="Chat"/>.</value>
    string SelectedProviderSessionName => ChatClientProvider?.SelectedSession?.Name ?? string.Empty;
    /// <summary>
    /// Stores the internal toast name state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string toastName = "ChatToasts";

    /// <summary>
    /// Gets or sets a value indicating whether reuse context when switching applies to the chat state.
    /// </summary>
    /// <value>The reuse context when switching value exposed by <see cref="Chat"/>.</value>
    bool ReuseContextWhenSwitching { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether auto load latest conversation applies to the chat state.
    /// </summary>
    /// <value>The auto load latest conversation value exposed by <see cref="Chat"/>.</value>
    bool AutoLoadLatestConversation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether fresh diagnostic chat applies to the chat state.
    /// </summary>
    /// <value>The use fresh diagnostic chat value exposed by <see cref="Chat"/>.</value>
    bool UseFreshDiagnosticChat { get; set; }

    /// <summary>
    /// Gets or sets the Ollama acceleration mode value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama acceleration mode value exposed by <see cref="Chat"/>.</value>
    string OllamaAccelerationMode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the limited GPU layers value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The limited GPU layers value exposed by <see cref="Chat"/>.</value>
    int LimitedGpuLayers { get; set; } = 20;
    /// <summary>
    /// Gets or sets the Ollama endpoint that identifies the network or application endpoint associated with this chat state.
    /// </summary>
    /// <value>The Ollama endpoint value exposed by <see cref="Chat"/>.</value>
    string OllamaEndpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether generate council artifacts applies to the chat state.
    /// </summary>
    /// <value>The generate council artifacts value exposed by <see cref="Chat"/>.</value>
    bool GenerateCouncilArtifacts { get; set; }
    /// <summary>
    /// Stores the internal show game console state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool showGameConsole;
    /// <summary>
    /// Gets or sets a value indicating whether council memory applies to the chat state.
    /// </summary>
    /// <value>The include council memory value exposed by <see cref="Chat"/>.</value>
    bool IncludeCouncilMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets the diagnostic requested session name value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The diagnostic requested session name value exposed by <see cref="Chat"/>.</value>
    string? DiagnosticRequestedSessionName { get; set; }
    /// <summary>
    /// Gets or sets the diagnostic Ollama endpoint that identifies the network or application endpoint associated with this chat state.
    /// </summary>
    /// <value>The diagnostic Ollama endpoint value exposed by <see cref="Chat"/>.</value>
    string? DiagnosticOllamaEndpoint { get; set; }
    /// <summary>
    /// Gets or sets the council max output tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council max output tokens value exposed by <see cref="Chat"/>.</value>
    int CouncilMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the council max context tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council max context tokens value exposed by <see cref="Chat"/>.</value>
    int CouncilMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the council resource load percent value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council resource load percent value exposed by <see cref="Chat"/>.</value>
    int CouncilResourceLoadPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets the council critique rounds value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council critique rounds value exposed by <see cref="Chat"/>.</value>
    int CouncilCritiqueRounds { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether council allow parallel hardware roads applies to the chat state.
    /// </summary>
    /// <value>The council allow parallel hardware roads value exposed by <see cref="Chat"/>.</value>
    bool CouncilAllowParallelHardwareRoads { get; set; } = true;

    /// <summary>
    /// Gets the simple scheduling mode shown to the user for the preparation or currently edited Council run.
    /// </summary>
    /// <value>The council editor scheduling mode value exposed by <see cref="Chat"/>.</value>
    string CouncilEditorSchedulingMode => (EditingRunningCouncilConfiguration ? ActiveCouncilAllowParallelHardwareRoads : CouncilAllowParallelHardwareRoads)
        ? "road-parallel"
        : "host-balanced";
    /// <summary>
    /// Gets the council editor max parallel models value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council editor max parallel models value exposed by <see cref="Chat"/>.</value>
    int CouncilEditorMaxParallelModels => EditingRunningCouncilConfiguration
        ? ActiveCouncilMaxParallelModels
        : Math.Max(1, CouncilMaxParallelModels);
    /// <summary>
    /// Gets the council editor model timeout seconds value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council editor model timeout seconds value exposed by <see cref="Chat"/>.</value>
    int CouncilEditorModelTimeoutSeconds => EditingRunningCouncilConfiguration
        ? ActiveCouncilModelTimeoutSeconds
        : Math.Clamp(CouncilModelTimeoutSeconds, 30, 1800);

    /// <summary>
    /// Gets a compact explanation of the currently selected Council scheduling policy.
    /// </summary>
    /// <value>The council scheduling summary value exposed by <see cref="Chat"/>.</value>
    string CouncilSchedulingSummary => CouncilEditorSchedulingMode == "road-parallel"
        ? $"Up to {CouncilEditorMaxParallelModels} request(s) per AI host may run concurrently, still constrained by each model road's lane setting; separate hosts also run independently."
        : "Each AI host stays single-flight while separate machines/providers may run concurrently.";
    /// <summary>
    /// Gets or sets the council max parallel models value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council max parallel models value exposed by <see cref="Chat"/>.</value>
    int CouncilMaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets the council model timeout seconds value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council model timeout seconds value exposed by <see cref="Chat"/>.</value>
    int CouncilModelTimeoutSeconds { get; set; } = 1800;

    /// <summary>
    /// Gets or sets the architecture language toolchain value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture language toolchain value exposed by <see cref="Chat"/>.</value>
    string ArchitectureLanguageToolchain { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture UI stack value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture UI stack value exposed by <see cref="Chat"/>.</value>
    string ArchitectureUiStack { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture solution shape value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture solution shape value exposed by <see cref="Chat"/>.</value>
    string ArchitectureSolutionShape { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture render mode value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture render mode value exposed by <see cref="Chat"/>.</value>
    string ArchitectureRenderMode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture reference look value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture reference look value exposed by <see cref="Chat"/>.</value>
    string ArchitectureReferenceLook { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the architecture poll notes value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture poll notes value exposed by <see cref="Chat"/>.</value>
    string ArchitecturePollNotes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether council to choose sandbox details applies to the chat state.
    /// </summary>
    /// <value>The allow council to choose sandbox details value exposed by <see cref="Chat"/>.</value>
    bool AllowCouncilToChooseSandboxDetails { get; set; }
    /// <summary>
    /// Gets or sets the architecture poll status value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture poll status value exposed by <see cref="Chat"/>.</value>
    string ArchitecturePollStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the all prompt suggestions collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The all prompt suggestions value exposed by <see cref="Chat"/>.</value>
    List<PromptSuggestion> AllPromptSuggestions { get; set; } = new();
    /// <summary>
    /// Gets or sets the prompt suggestions collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The prompt suggestions value exposed by <see cref="Chat"/>.</value>
    List<PromptSuggestion> PromptSuggestions { get; set; } = new();
    /// <summary>
    /// Gets the council starter suggestions collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council starter suggestions value exposed by <see cref="Chat"/>.</value>
    IReadOnlyList<PromptSuggestion> CouncilStarterSuggestions => AllPromptSuggestions
        .Where(item => item.StartsCouncilDirectly && item.IsAvailableForTeam(SelectedCouncilTeamKey))
        .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        .ToList();
    /// <summary>
    /// Gets or sets the saved conversations collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The saved conversations value exposed by <see cref="Chat"/>.</value>
    List<ChatMemoryConversationSummary> SavedConversations { get; set; } = new();
    /// <summary>
    /// Gets or sets the recent thoughts collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The recent thoughts value exposed by <see cref="Chat"/>.</value>
    List<ChatMemoryThought> RecentThoughts { get; set; } = new();
    /// <summary>
    /// Gets or sets the Ollama candidates collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The Ollama candidates value exposed by <see cref="Chat"/>.</value>
    List<MultiModelCouncilModelCandidate> OllamaCandidates { get; set; } = new();
    /// <summary>
    /// Gets or sets the selected council model names collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The selected council model names value exposed by <see cref="Chat"/>.</value>
    List<string> SelectedCouncilModelNames { get; set; } = new();
    /// <summary>
    /// Gets the selected council provider models collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The selected council provider models value exposed by <see cref="Chat"/>.</value>
    IReadOnlyList<ProviderModelReference> SelectedCouncilProviderModels => OllamaCandidates
        .Where(candidate => SelectedCouncilModelNames.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
        .Select(candidate => candidate.ToReference())
        .ToList();
    /// <summary>
    /// Gets the unavailable council selections collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The unavailable council selections value exposed by <see cref="Chat"/>.</value>
    IReadOnlyList<string> UnavailableCouncilSelections => SelectedCouncilModelNames
        .Where(value => new ProviderModelIdentity().LooksProviderQualified(value))
        .Where(value => !OllamaCandidates.Any(candidate => candidate.SelectionKey.Equals(value, StringComparison.OrdinalIgnoreCase)))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    /// <summary>
    /// Determines whether selection endpoint still configured for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="selectionKey">Selection key value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Gets the blocking unavailable council selections collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The blocking unavailable council selections value exposed by <see cref="Chat"/>.</value>
    IReadOnlyList<string> BlockingUnavailableCouncilSelections => UnavailableCouncilSelections
        .Where(value => !IsSelectionEndpointStillConfigured(value))
        .ToList();
    /// <summary>
    /// Gets the council provider hosts collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council provider hosts value exposed by <see cref="Chat"/>.</value>
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

    /// <summary>
    /// Represents a council provider host group helper type nested within <see cref="Chat"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Key">Key value supplied to the chat operation and used when producing its result.</param>
    /// <param name="ProviderName">Provider name value supplied to the chat operation and used when producing its result.</param>
    /// <param name="EndpointLabel">Endpoint label value supplied to the chat operation and used when producing its result.</param>
    /// <param name="Models">Multi model council model candidate dependency used by the chat workflow to provide the corresponding application capability.</param>
    private sealed record CouncilProviderHostGroup(
        string Key,
        string ProviderName,
        string EndpointLabel,
        IReadOnlyList<MultiModelCouncilModelCandidate> Models);

    /// <summary>
    /// Performs l for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="key">Key value supplied to the chat operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string L(string key, string fallback) => Localization.Get(key, fallback: fallback);

    /// <summary>
    /// Gets or sets the diagnostic council model names collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The diagnostic council model names value exposed by <see cref="Chat"/>.</value>
    List<string> DiagnosticCouncilModelNames { get; set; } = new();
    /// <summary>
    /// Gets or sets the model presets collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The model presets value exposed by <see cref="Chat"/>.</value>
    List<CouncilModelPreset> ModelPresets { get; set; } = new();
    /// <summary>
    /// Gets or sets the hardware performance preset items collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The hardware performance preset items value exposed by <see cref="Chat"/>.</value>
    List<HardwarePerformancePreset> HardwarePerformancePresetItems { get; set; } = new();
    /// <summary>
    /// Gets or sets the selected hardware performance preset value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected hardware performance preset value exposed by <see cref="Chat"/>.</value>
    HardwarePerformancePreset? SelectedHardwarePerformancePreset { get; set; }
    /// <summary>
    /// Gets or sets the hardware performance preset name value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware performance preset name value exposed by <see cref="Chat"/>.</value>
    string HardwarePerformancePresetName { get; set; } = string.Empty;
    /// <summary>
    /// Stores the internal is hardware performance preset busy state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool isHardwarePerformancePresetBusy;
    /// <summary>
    /// Gets the selected hardware performance preset value value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected hardware performance preset value value exposed by <see cref="Chat"/>.</value>
    string SelectedHardwarePerformancePresetValue => SelectedHardwarePerformancePreset?.Id.ToString() ?? string.Empty;
    /// <summary>
    /// Gets or sets the council model routes collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council model routes value exposed by <see cref="Chat"/>.</value>
    List<OneWireCouncilModelRoute> CouncilModelRoutes { get; set; } = [];
    /// <summary>
    /// Gets or sets the active council model routes collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The active council model routes value exposed by <see cref="Chat"/>.</value>
    List<OneWireCouncilModelRoute> ActiveCouncilModelRoutes { get; set; } = [];
    /// <summary>
    /// Gets or sets the active council configuration participants collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The active council configuration participants value exposed by <see cref="Chat"/>.</value>
    List<string> ActiveCouncilConfigurationParticipants { get; set; } = [];
    /// <summary>
    /// Gets or sets the stable active council configuration run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The active council configuration run identifier value exposed by <see cref="Chat"/>.</value>
    Guid? ActiveCouncilConfigurationRunId { get; set; }
    /// <summary>
    /// Gets or sets the active council configuration revision value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council configuration revision value exposed by <see cref="Chat"/>.</value>
    long ActiveCouncilConfigurationRevision { get; set; }
    /// <summary>
    /// Gets or sets the active council resource load percent value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council resource load percent value exposed by <see cref="Chat"/>.</value>
    int ActiveCouncilResourceLoadPercent { get; set; } = 100;
    /// <summary>
    /// Gets or sets the active council max output tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council max output tokens value exposed by <see cref="Chat"/>.</value>
    int ActiveCouncilMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the active council max context tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council max context tokens value exposed by <see cref="Chat"/>.</value>
    int ActiveCouncilMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the active council fallback Ollama num GPU value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council fallback Ollama num GPU value exposed by <see cref="Chat"/>.</value>
    int? ActiveCouncilFallbackOllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether active council allow parallel hardware roads applies to the chat state.
    /// </summary>
    /// <value>The active council allow parallel hardware roads value exposed by <see cref="Chat"/>.</value>
    bool ActiveCouncilAllowParallelHardwareRoads { get; set; } = true;
    /// <summary>
    /// Gets or sets the active council max parallel models value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council max parallel models value exposed by <see cref="Chat"/>.</value>
    int ActiveCouncilMaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets the active council model timeout seconds value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council model timeout seconds value exposed by <see cref="Chat"/>.</value>
    int ActiveCouncilModelTimeoutSeconds { get; set; } = 1800;
    /// <summary>
    /// Gets a value indicating whether editing running council configuration applies to the chat state.
    /// </summary>
    /// <value>The editing running council configuration value exposed by <see cref="Chat"/>.</value>
    bool EditingRunningCouncilConfiguration =>
        ActiveCouncilConfigurationRunId is Guid runId &&
        SelectedCouncilRunId == runId &&
        CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    /// <summary>
    /// Gets the council editor model names collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council editor model names value exposed by <see cref="Chat"/>.</value>
    IReadOnlyCollection<string> CouncilEditorModelNames =>
        EditingRunningCouncilConfiguration ? ActiveCouncilConfigurationParticipants : SelectedCouncilModelNames;
    /// <summary>
    /// Gets the council editor routes collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council editor routes value exposed by <see cref="Chat"/>.</value>
    List<OneWireCouncilModelRoute> CouncilEditorRoutes =>
        EditingRunningCouncilConfiguration ? ActiveCouncilModelRoutes : CouncilModelRoutes;
    /// <summary>
    /// Gets the council editor resource load percent value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council editor resource load percent value exposed by <see cref="Chat"/>.</value>
    int CouncilEditorResourceLoadPercent =>
        EditingRunningCouncilConfiguration ? ActiveCouncilResourceLoadPercent : CouncilResourceLoadPercent;
    /// <summary>
    /// Gets the council editor max output tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council editor max output tokens value exposed by <see cref="Chat"/>.</value>
    int CouncilEditorMaxOutputTokens =>
        EditingRunningCouncilConfiguration ? ActiveCouncilMaxOutputTokens : CouncilMaxOutputTokens;
    /// <summary>
    /// Gets the council editor max context tokens value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council editor max context tokens value exposed by <see cref="Chat"/>.</value>
    int CouncilEditorMaxContextTokens =>
        EditingRunningCouncilConfiguration ? ActiveCouncilMaxContextTokens : CouncilMaxContextTokens;
    /// <summary>
    /// Gets or sets the selected model preset value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected model preset value exposed by <see cref="Chat"/>.</value>
    CouncilModelPreset? SelectedModelPreset { get; set; }
    /// <summary>
    /// Gets or sets the council teams collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The council teams value exposed by <see cref="Chat"/>.</value>
    List<OrganicCouncilTeamDefinition> CouncilTeams { get; set; } = [];
    /// <summary>
    /// Gets or sets the stable selected council team key used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The selected council team key value exposed by <see cref="Chat"/>.</value>
    string SelectedCouncilTeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets the model preset name value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model preset name value exposed by <see cref="Chat"/>.</value>
    string ModelPresetName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether create project per council run applies to the chat state.
    /// </summary>
    /// <value>The create project per council run value exposed by <see cref="Chat"/>.</value>
    bool CreateProjectPerCouncilRun { get; set; } = true;
    /// <summary>
    /// Stores the internal is model preset busy state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool isModelPresetBusy;
    /// <summary>
    /// Gets the selected model preset value value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected model preset value value exposed by <see cref="Chat"/>.</value>
    string SelectedModelPresetValue => SelectedModelPreset?.Id.ToString() ?? string.Empty;
    /// <summary>
    /// Gets or sets the chat projects collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The chat projects value exposed by <see cref="Chat"/>.</value>
    List<LocalGptProjectSummary> ChatProjects { get; set; } = new();
    /// <summary>
    /// Gets or sets the selected chat project details value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected chat project details value exposed by <see cref="Chat"/>.</value>
    LocalGptProjectDetails? SelectedChatProjectDetails { get; set; }
    /// <summary>
    /// Gets or sets the feedback targets collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The feedback targets value exposed by <see cref="Chat"/>.</value>
    List<ChatFeedbackTarget> FeedbackTargets { get; set; } = new();
    /// <summary>
    /// Gets or sets the saved feedback collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The saved feedback value exposed by <see cref="Chat"/>.</value>
    List<ChatMessageFeedbackSnapshot> SavedFeedback { get; set; } = new();
    /// <summary>
    /// Gets or sets the selected conversation value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected conversation value exposed by <see cref="Chat"/>.</value>
    ChatMemoryConversationSummary? SelectedConversation { get; set; }
    /// <summary>
    /// Gets or sets the stable active conversation identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The active conversation identifier value exposed by <see cref="Chat"/>.</value>
    Guid? ActiveConversationId { get; set; }
    /// <summary>
    /// Gets or sets the selected feedback sort order value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected feedback sort order value exposed by <see cref="Chat"/>.</value>
    int? SelectedFeedbackSortOrder { get; set; }
    /// <summary>
    /// Gets or sets the feedback comment value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The feedback comment value exposed by <see cref="Chat"/>.</value>
    string FeedbackComment { get; set; } = string.Empty;
    /// <summary>
    /// Stores the internal memory status state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string memoryStatus = string.Empty;
    /// <summary>
    /// Stores the internal model status state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string modelStatus = string.Empty;
    /// <summary>
    /// Stores the internal model selection notice state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string modelSelectionNotice = string.Empty;
    /// <summary>
    /// Stores the internal had unavailable provider selections state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool hadUnavailableProviderSelections;
    /// <summary>
    /// Stores the internal feedback status state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string feedbackStatus = string.Empty;
    /// <summary>
    /// Stores the internal is memory busy state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool isMemoryBusy;
    /// <summary>
    /// Stores the internal is model refresh busy state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool isModelRefreshBusy;
    /// <summary>
    /// Stores the internal chat configuration open state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool chatConfigurationOpen;
    /// <summary>
    /// Stores the cancellation source used by <see cref="Chat"/> to stop its current background or asynchronous operation.
    /// </summary>
    readonly CancellationTokenSource componentLifetimeCts = new();
    /// <summary>
    /// Stores the internal auto save started state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool autoSaveStarted;
    /// <summary>
    /// Stores the internal interactive attached state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool interactiveAttached;
    /// <summary>
    /// Stores the internal chat control initialized state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool chatControlInitialized;
    /// <summary>
    /// Stores the internal chat runtime started state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool chatRuntimeStarted;
    /// <summary>
    /// Stores the internal chat runtime activation scheduled state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool chatRuntimeActivationScheduled;
    /// <summary>
    /// Stores the internal is disposed state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool isDisposed;
    /// <summary>
    /// Stores the internal collaboration snapshot state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    HumanCollaborationSnapshot collaborationSnapshot = new(new HumanCouncilParticipantProfile(), [], [], []);
    /// <summary>
    /// Gets or sets the stable requested rejoin council run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The requested rejoin council run identifier value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "rejoinCouncilRunId")]
    public Guid? RequestedRejoinCouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable requested council team key used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The requested council team key value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "team")]
    public string? RequestedCouncilTeamKey { get; set; }
    /// <summary>
    /// Gets or sets the requested model preset name value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested model preset name value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "preset")]
    public string? RequestedModelPresetName { get; set; }
    /// <summary>
    /// Gets or sets the stable requested council starter key used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The requested council starter key value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "starter")]
    public string? RequestedCouncilStarterKey { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether auto start council starter applies to the chat state.
    /// </summary>
    /// <value>The auto start council starter value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "autoStartCouncil")]
    public bool AutoStartCouncilStarter { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether start new council chat applies to the chat state.
    /// </summary>
    /// <value>The start new council chat value exposed by <see cref="Chat"/>.</value>
    [Parameter]
    [SupplyParameterFromQuery(Name = "newCouncil")]
    public bool StartNewCouncilChat { get; set; }
    /// <summary>
    /// Stores the internal direct council starter dispatched state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool directCouncilStarterDispatched;
    /// <summary>
    /// Stores the internal direct council starter dispatching state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool directCouncilStarterDispatching;
    /// <summary>
    /// Stores the internal direct council starter dispatch attempts state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    int directCouncilStarterDispatchAttempts;
    /// <summary>
    /// Gets or sets the stable selected council run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The selected council run identifier value exposed by <see cref="Chat"/>.</value>
    Guid? SelectedCouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable rejoin council run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The rejoin council run identifier value exposed by <see cref="Chat"/>.</value>
    Guid? RejoinCouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable attached live council run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The attached live council run identifier value exposed by <see cref="Chat"/>.</value>
    Guid? AttachedLiveCouncilRunId { get; set; }
    /// <summary>
    /// Stores the internal live council refresh scheduled state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    int liveCouncilRefreshScheduled;
    /// <summary>
    /// Stores the internal live council list refresh scheduled state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    int liveCouncilListRefreshScheduled;
    /// <summary>
    /// Stores the internal owns live council stream state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool ownsLiveCouncilStream;
    /// <summary>
    /// Stores the internal last attached live council updated at UTC state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    DateTime lastAttachedLiveCouncilUpdatedAtUtc;
    /// <summary>
    /// Stores the internal attached live council snapshot state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    CouncilLiveSessionAttachmentSnapshot? attachedLiveCouncilSnapshot;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to live council attach gate state owned by <see cref="Chat"/>.
    /// </summary>
    readonly SemaphoreSlim liveCouncilAttachGate = new(1, 1);
    /// <summary>
    /// Defines the live council message marker prefix constant used by <see cref="Chat"/> so callers and internal logic share the same stable value.
    /// </summary>
    const string LiveCouncilMessageMarkerPrefix = "<!-- localgpt-live-council:";
    /// <summary>
    /// Gets the selected council run value value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected council run value value exposed by <see cref="Chat"/>.</value>
    string SelectedCouncilRunValue => SelectedCouncilRunId?.ToString() ?? string.Empty;
    /// <summary>
    /// Returns the current independently streamed participant lanes for a live Council run.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<CouncilLiveParticipantActivitySnapshot> LiveCouncilParticipantActivities(Guid runId)
    {
        try
        {
            return CouncilLiveSessions.GetParticipantActivitiesForDisplay(runId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not read live participant activities for Council run {RunId}.", runId);
            return [];
        }
    }

    /// <summary>
    /// Performs live council transcript for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="fallbackContent">Fallback content value supplied to the chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string LiveCouncilTranscript(Guid runId, string fallbackContent)
    {
        var latestTranscript = CouncilLiveSessions.GetTranscriptForDisplay(runId);
        return string.IsNullOrWhiteSpace(latestTranscript) ? fallbackContent : latestTranscript;
    }

    /// <summary>
    /// Gets the running live council sessions collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The running live council sessions value exposed by <see cref="Chat"/>.</value>
    IReadOnlyList<CouncilLiveSessionSummary> RunningLiveCouncilSessions => CouncilLiveSessions.GetActiveSummaries();
    /// <summary>
    /// Gets the active live council session value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active live council session value exposed by <see cref="Chat"/>.</value>
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
    /// <summary>
    /// Gets a value indicating whether live council interaction available applies to the chat state.
    /// </summary>
    /// <value>The is live council interaction available value exposed by <see cref="Chat"/>.</value>
    bool IsLiveCouncilInteractionAvailable => ActiveLiveCouncilSession?.IsRunning == true;
    /// <summary>
    /// Gets a value indicating whether rejoin selected council run applies to the chat state.
    /// </summary>
    /// <value>The can rejoin selected council run value exposed by <see cref="Chat"/>.</value>
    bool CanRejoinSelectedCouncilRun =>
        SelectedCouncilRunId is Guid runId && CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    /// <summary>
    /// Gets a value indicating whether selected council session attached applies to the chat state.
    /// </summary>
    /// <value>The is selected council session attached value exposed by <see cref="Chat"/>.</value>
    bool IsSelectedCouncilSessionAttached =>
        SelectedCouncilRunId is Guid runId
        && (AttachedLiveCouncilRunId == runId || RejoinCouncilRunId == runId)
        && CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
    /// <summary>
    /// Gets a value indicating whether join selected council run applies to the chat state.
    /// </summary>
    /// <value>The can join selected council run value exposed by <see cref="Chat"/>.</value>
    bool CanJoinSelectedCouncilRun => CanRejoinSelectedCouncilRun && !IsSelectedCouncilSessionAttached;
    /// <summary>
    /// Gets the stable active council interaction run identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The active council interaction run identifier value exposed by <see cref="Chat"/>.</value>
    Guid? ActiveCouncilInteractionRunId => ActiveCouncilRun?.RunId ?? ActiveLiveCouncilSession?.RunId;
    /// <summary>
    /// Gets the stable active council run short identifier used to identify or correlate this chat instance with related application state.
    /// </summary>
    /// <value>The active council run short identifier value exposed by <see cref="Chat"/>.</value>
    string ActiveCouncilRunShortId => ActiveCouncilInteractionRunId is Guid runId ? ShortCouncilRunId(runId) : string.Empty;
    /// <summary>
    /// Gets or sets the running council contribution value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The running council contribution value exposed by <see cref="Chat"/>.</value>
    string RunningCouncilContribution { get; set; } = string.Empty;
    /// <summary>
    /// Gets the pending live council user messages collection maintained or exposed by this chat instance for downstream processing.
    /// </summary>
    /// <value>The pending live council user messages value exposed by <see cref="Chat"/>.</value>
    List<BlazorChatMessage> PendingLiveCouncilUserMessages { get; } = [];
    /// <summary>
    /// Gets the active council run value that forms part of the chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active council run value exposed by <see cref="Chat"/>.</value>
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
    /// <summary>
    /// Stores the internal auto save failure notified state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool autoSaveFailureNotified;
    /// <summary>
    /// Stores the internal initial model refresh started state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool initialModelRefreshStarted;
    /// <summary>
    /// Stores the internal initial state initialization started state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool initialStateInitializationStarted;
    /// <summary>
    /// Stores the internal initial state ready state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool initialStateReady;
    /// <summary>
    /// Stores the internal has user selected session state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    bool hasUserSelectedSession;
    /// <summary>
    /// Stores the internal last saved signature state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    string lastSavedSignature = string.Empty;
    /// <summary>
    /// Stores the internal chat interop reference state used by <see cref="Chat"/> while executing its surrounding workflow.
    /// </summary>
    DotNetObjectReference<Chat>? chatInteropReference;



    /// <summary>
    /// Performs toggle game console for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    private void ToggleGameConsole() => showGameConsole = !showGameConsole;

    /// <summary>
    /// Closes game console for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    private void CloseGameConsole() => showGameConsole = false;

    /// <summary>
    /// Handles the initialized async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs initialize chat state for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Loads council teams for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Loads database backed defaults for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs migrate council defaults for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Attempts to apply database variable for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="Chat"/>.</typeparam>
    /// <param name="definition">Definition value supplied to the chat operation and used when producing its result.</param>
    /// <param name="apply">Apply value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
