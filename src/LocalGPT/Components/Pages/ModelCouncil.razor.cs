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

namespace LocalGPT.Components.Pages
{
    /// <summary>
    /// Renders the model council Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
    /// </summary>
    public partial class ModelCouncil
    {
    /// <summary>
    /// Stores the internal toast name state used by <see cref="ModelCouncil"/> while executing its surrounding workflow.
    /// </summary>
    readonly string toastName = "ModelCouncilToasts";
    /// <summary>
    /// Stores the in-memory selected models collection maintained internally by <see cref="ModelCouncil"/> for its current workflow state.
    /// </summary>
    readonly HashSet<string> SelectedModels = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets the candidates collection maintained or exposed by this model council instance for downstream processing.
    /// </summary>
    /// <value>The candidates value exposed by <see cref="ModelCouncil"/>.</value>
    List<MultiModelCouncilModelCandidate> Candidates { get; set; } = [];
    /// <summary>
    /// Gets the selected provider models collection maintained or exposed by this model council instance for downstream processing.
    /// </summary>
    /// <value>The selected provider models value exposed by <see cref="ModelCouncil"/>.</value>
    IReadOnlyList<ProviderModelReference> SelectedProviderModels => Candidates
        .Where(candidate => SelectedModels.Contains(candidate.SelectionKey))
        .Select(candidate => candidate.ToReference())
        .ToList();
    /// <summary>
    /// Gets or sets the saved council conversations collection maintained or exposed by this model council instance for downstream processing.
    /// </summary>
    /// <value>The saved council conversations value exposed by <see cref="ModelCouncil"/>.</value>
    List<ChatMemoryConversationSummary> SavedCouncilConversations { get; set; } = [];
    /// <summary>
    /// Gets or sets the project summaries collection maintained or exposed by this model council instance for downstream processing.
    /// </summary>
    /// <value>The project summaries value exposed by <see cref="ModelCouncil"/>.</value>
    List<LocalGptProjectSummary> ProjectSummaries { get; set; } = [];
    /// <summary>
    /// Gets or sets the selected project details value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected project details value exposed by <see cref="ModelCouncil"/>.</value>
    LocalGptProjectDetails? SelectedProjectDetails { get; set; }
    /// <summary>
    /// Gets or sets the selected council conversation value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected council conversation value exposed by <see cref="ModelCouncil"/>.</value>
    ChatMemoryConversationSummary? SelectedCouncilConversation { get; set; }
    /// <summary>
    /// Gets or sets the last result value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last result value exposed by <see cref="ModelCouncil"/>.</value>
    MultiModelCouncilResult? LastResult { get; set; }
    /// <summary>
    /// Gets or sets the last generation execution value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last generation execution value exposed by <see cref="ModelCouncil"/>.</value>
    CodeGenerationExecutionResult? LastGenerationExecution { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether build approved review applies to the model council state.
    /// </summary>
    /// <value>The build approved review value exposed by <see cref="ModelCouncil"/>.</value>
    bool BuildApprovedReview { get; set; }
    /// <summary>
    /// Gets or sets the manual model name value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The manual model name value exposed by <see cref="ModelCouncil"/>.</value>
    string ManualModelName { get; set; } = "qwen3-coder:30b";
    /// <summary>
    /// Gets or sets the custom poll feedback value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The custom poll feedback value exposed by <see cref="ModelCouncil"/>.</value>
    string CustomPollFeedback { get; set; } = string.Empty;
    /// <summary>
    /// Stores the internal status text state used by <see cref="ModelCouncil"/> while executing its surrounding workflow.
    /// </summary>
    string statusText = string.Empty;
    /// <summary>
    /// Stores the internal is busy state used by <see cref="ModelCouncil"/> while executing its surrounding workflow.
    /// </summary>
    bool isBusy;

    /// <summary>
    /// Gets or sets the request value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ModelCouncil"/>.</value>
    MultiModelCouncilRequest Request { get; set; } = new()
    {
        Prompt = "Ask gpt-oss:20b and the coder model to design the best and most ethical LocalGPT multi-model council feature." + "\n\n" +
            "Cover:\n" +
            "- how several models should peacefully negotiate and correct each other,\n" +
            "- how the transcript and visible reasoning notes should be shown to users,\n" +
            "- how SQLite memory should make prior council work available to later model calls,\n" +
            "- how this should help generate complex Java Minecraft mods safely,\n" +
            "- what privacy, safety, and user-control limits should exist.\n\n" +
            "Separate what LocalGPT already does from proposed future improvements. Mark anything uncertain under Needs verification.",
        MaxRounds = 1,
        MaxOutputTokens = 262144,
        MaxParallelModels = 1,
        ResourceLoadPercent = 100,
        MaxContextTokens = 262144,
        ModelTimeoutSeconds = 1800,
        OllamaKeepAlive = "0s",
        OllamaNumGpu = null,
        IncludeMemory = true,
        SaveToMemory = true,
        GenerateImplementationArtifact = false
    };

    /// <summary>
    /// Gets the selected project value value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected project value value exposed by <see cref="ModelCouncil"/>.</value>
    string SelectedProjectValue => Request.ProjectId?.ToString() ?? string.Empty;
    /// <summary>
    /// Gets the selected topic value value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selected topic value value exposed by <see cref="ModelCouncil"/>.</value>
    string SelectedTopicValue => Request.ProjectTopicId?.ToString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether run applies to the model council state.
    /// </summary>
    /// <value>The can run value exposed by <see cref="ModelCouncil"/>.</value>
    bool CanRun => !isBusy && SelectedModels.Count > 0 && !string.IsNullOrWhiteSpace(Request.Prompt);
    /// <summary>
    /// Gets or sets the keep alive text value that forms part of the model council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The keep alive text value exposed by <see cref="ModelCouncil"/>.</value>
    string KeepAliveText
    {
        get => Request.OllamaKeepAlive ?? string.Empty;
        set => Request.OllamaKeepAlive = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Gets or sets a value indicating whether force CPU only Ollama applies to the model council state.
    /// </summary>
    /// <value>The force CPU only Ollama value exposed by <see cref="ModelCouncil"/>.</value>
    bool ForceCpuOnlyOllama
    {
        get => Request.OllamaNumGpu == 0;
        set => Request.OllamaNumGpu = value ? 0 : null;
    }

    /// <summary>
    /// Handles the initialized async lifecycle or event notification for <see cref="ModelCouncil"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadCandidatesAsync().ConfigureAwait(false);
            await LoadSavedCouncilConversationsAsync().ConfigureAwait(false);
            await LoadProjectsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Loads candidates for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task LoadCandidatesAsync()
    {
        try
        {
            await RunUiActionAsync(async () =>
       {
           var previouslySelectedCandidates = Candidates
               .Where(candidate => SelectedModels.Contains(candidate.SelectionKey))
               .ToList();
           var discoveredCandidates = (await CouncilService.GetCandidatesAsync().ConfigureAwait(false)).ToList();
           foreach (var preserved in previouslySelectedCandidates)
           {
               if (!discoveredCandidates.Any(candidate => candidate.SelectionKey.Equals(preserved.SelectionKey, StringComparison.OrdinalIgnoreCase)))
                   discoveredCandidates.Add(preserved);
           }
           Candidates = discoveredCandidates
               .OrderByDescending(candidate => candidate.IsInstalled)
               .ThenByDescending(candidate => candidate.IsConfigured)
               .ThenBy(candidate => candidate.Provider)
               .ThenBy(candidate => candidate.ModelName)
               .ToList();

           var normalizedSelections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
           foreach (var selected in SelectedModels)
           {
               var exact = Candidates.FirstOrDefault(candidate => candidate.SelectionKey.Equals(selected, StringComparison.OrdinalIgnoreCase));
               if (exact is not null)
               {
                   normalizedSelections.Add(exact.SelectionKey);
                   continue;
               }

               var byModel = Candidates
                   .Where(candidate => candidate.ModelName.Equals(selected, StringComparison.OrdinalIgnoreCase))
                   .ToList();
               if (byModel.Count == 1)
                   normalizedSelections.Add(byModel[0].SelectionKey);
           }
           SelectedModels.Clear();
           foreach (var selection in normalizedSelections)
               SelectedModels.Add(selection);

           if (SelectedModels.Count == 0)
           {
               foreach (var candidate in Candidates.Where(candidate => candidate.IsInstalled || candidate.IsConfigured).Take(2))
                   SelectedModels.Add(candidate.SelectionKey);
           }

           statusText = Candidates.Count == 0
               ? "No provider models found."
               : $"Found {Candidates.Count(candidate => candidate.IsInstalled)} discovered provider model(s) across {Candidates.Select(candidate => candidate.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count()} provider(s); {Candidates.Count(candidate => candidate.IsConfigured && !candidate.IsInstalled)} configured model(s) will verify connectivity when benchmarked or called.";
       }, "LoadCandidatesAsync").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Performs run council for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task RunCouncilAsync()
    {
        try
        {
            await RunUiActionAsync(async () =>
            {
                Request.ModelNames = SelectedModels.ToList();
                Request.ModelSelections = SelectedProviderModels.ToList();
                try
                {
                    LastResult = await CouncilService.RunAsync(Request).ConfigureAwait(false);
                    LastGenerationExecution = null;
                    BuildApprovedReview = false;
                    if (LastResult.MemoryConversationId is Guid memoryId)
                    {
                        Request.ContinueConversationId = memoryId;
                        await LoadSavedCouncilConversationsAsync(showStatus: false).ConfigureAwait(false);
                        SelectedCouncilConversation = SavedCouncilConversations.FirstOrDefault(conversation => conversation.Id == memoryId);
                    }

                    statusText = $"Council completed with {LastResult.Steps.Count} transcript step(s).";
                    Notifier.ShowSuccess(toastName, "Council transcript saved and visualized.", "Council complete");
                }
                finally
                {
                    // Consequential permissions are deliberately one-run confirmations.
                    Request.GenerateImplementationArtifact = false;
                    Request.UserConfirmedArtifactBuild = false;
                    Request.UserConfirmedProjectLink = false;
                }
            }, "RunCouncilAsync").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Loads saved council conversations for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task LoadSavedCouncilConversationsAsync()
    {
        try
        {
            await LoadSavedCouncilConversationsAsync(showStatus: true).ConfigureAwait(false); ;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Loads saved council conversations for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="showStatus">Value indicating whether show status should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task LoadSavedCouncilConversationsAsync(bool showStatus)
    {
        try
        {
            await RunUiActionAsync(async () =>
       {
           SavedCouncilConversations = (await ChatMemory.GetConversationsAsync(100).ConfigureAwait(false))
               .Where(conversation => CouncilText.StartsWithText(conversation.ProviderName, "AI Council"))
               .ToList();

           SelectedCouncilConversation = Request.ContinueConversationId is Guid selectedId
               ? SavedCouncilConversations.FirstOrDefault(conversation => conversation.Id == selectedId)
               : null;

           if (showStatus)
               statusText = $"Loaded {SavedCouncilConversations.Count} saved council conversation(s).";
       }, "LoadSavedCouncilConversationsAsync").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Loads projects for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task LoadProjectsAsync()
    {
        ProjectSummaries = (await ProjectService.GetProjectsAsync(includeArchived: false).ConfigureAwait(false)).ToList();

        if (Request.ProjectId is Guid projectId)
            SelectedProjectDetails = await ProjectService.GetProjectAsync(projectId).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs select project for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the model council operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task SelectProjectAsync(ChangeEventArgs args)
    {
        Request.ProjectTopicId = null;
        Request.UserConfirmedProjectLink = false;
        SelectedProjectDetails = null;

        if (!Guid.TryParse(Convert.ToString(args.Value), out var projectId))
        {
            Request.ProjectId = null;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            return;
        }

        Request.ProjectId = projectId;
        SelectedProjectDetails = await ProjectService.GetProjectAsync(projectId).ConfigureAwait(false);
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs select project topic for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the model council operation and used when producing its result.</param>
    void SelectProjectTopic(ChangeEventArgs args)
    {
        Request.ProjectTopicId = Guid.TryParse(Convert.ToString(args.Value), out var topicId)
            ? topicId
            : null;
        Request.UserConfirmedProjectLink = false;
    }

    /// <summary>
    /// Performs toggle artifact generation for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the model council operation and used when producing its result.</param>
    void ToggleArtifactGeneration(ChangeEventArgs args)
    {
        var enabled = args.Value is bool flag
            ? flag
            : bool.TryParse(Convert.ToString(args.Value), out var parsed) && parsed;

        Request.GenerateImplementationArtifact = enabled;
        // The council checkbox only requests a review heartbeat. Build approval is never carried into the run.
        Request.UserConfirmedArtifactBuild = false;
    }

    /// <summary>
    /// Adds manual model for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void AddManualModel()
    {
        try
        {
            var model = ManualModelName.Trim();
            if (string.IsNullOrWhiteSpace(model))
                return;

            var candidate = Candidates.FirstOrDefault(item =>
                item.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                && item.ModelName.Equals(model, StringComparison.OrdinalIgnoreCase))
                ?? new MultiModelCouncilModelCandidate(
                    model,
                    "Ollama",
                    "http://127.0.0.1:11434",
                    IsInstalled: false,
                    IsConfigured: false,
                    IsLoaded: false,
                    Details: "Legacy manual Ollama model. The native endpoint will report an error if it is not installed.",
                    ProviderKind: ProviderModelKinds.Ollama,
                    IsLocal: true,
                    SupportsBenchmark: true);
            if (!Candidates.Any(item => item.SelectionKey.Equals(candidate.SelectionKey, StringComparison.OrdinalIgnoreCase)))
                Candidates.Add(candidate);
            SelectedModels.Add(candidate.SelectionKey);
            statusText = $"Added {candidate.SelectionKey}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Performs join running council for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    private void JoinRunningCouncil() => Navigation.NavigateTo("/Chat?joinCouncil=active", forceLoad: true);

    /// <summary>
    /// Performs toggle model for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="selectionKey">Selection key value supplied to the model council operation and used when producing its result.</param>
    /// <param name="isChecked">Value indicating whether is checked should apply to this operation.</param>
    void ToggleModel(string selectionKey, bool isChecked)
    {
        try
        {
            if (isChecked)
                SelectedModels.Add(selectionKey);
            else
                SelectedModels.Remove(selectionKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Provider-qualified model selection failed; model identity was omitted from logs.");
            Notifier.ShowError(toastName, "The model selection could not be changed. See local logs for details.", "Selection failed");
        }
    }

    /// <summary>
    /// Handles the benchmark applied async lifecycle or event notification for <see cref="ModelCouncil"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="applied">Applied value supplied to the model council operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task OnBenchmarkAppliedAsync(ProviderModelBenchmarkAppliedEvent applied)
    {
        SelectedModels.Add(applied.Model.SelectionKey);
        Request.ModelRoutes.RemoveAll(route => route.ModelName.Equals(applied.Route.ModelName, StringComparison.OrdinalIgnoreCase));
        Request.ModelRoutes.Add(applied.Route);
        statusText = $"Applied benchmark settings for {applied.Model.SelectionKey} from preset {applied.Preset.Name}.";
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handles the benchmark council applied async lifecycle or event notification for <see cref="ModelCouncil"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="applied">Applied value supplied to the model council operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task OnBenchmarkCouncilAppliedAsync(ProviderModelBenchmarkBatchAppliedEvent applied)
    {
        foreach (var model in applied.Models)
            SelectedModels.Add(model.SelectionKey);
        foreach (var route in applied.Routes)
        {
            Request.ModelRoutes.RemoveAll(existing =>
                existing.ModelName.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase));
            Request.ModelRoutes.Add(route);
        }
        statusText = $"Applied {applied.Routes.Count} Benchmark Council recommendation(s) from preset {applied.Preset.Name}.";
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Performs select council conversation for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="conversation">Conversation value supplied to the model council operation and used when producing its result.</param>
    void SelectCouncilConversation(ChatMemoryConversationSummary conversation)
    {
        try
        {
            SelectedCouncilConversation = conversation;
            Request.ContinueConversationId = conversation.Id;
            statusText = $"Selected council memory: {conversation.DisplayName}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Starts new council thread for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void StartNewCouncilThread()
    {
        try
        {
            SelectedCouncilConversation = null;
            Request.ContinueConversationId = null;
            Request.GenerateImplementationArtifact = false;
            Request.UserConfirmedArtifactBuild = false;
            Request.UserConfirmedProjectLink = false;
            statusText = "Next council run will start a new memory conversation.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Applies low resource preset for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void ApplyLowResourcePreset()
    {
        try
        {
            Request.MaxRounds = 0;
            Request.MaxOutputTokens = 1024;
            Request.MaxParallelModels = 1;
            Request.ResourceLoadPercent = 25;
            Request.MaxContextTokens = 2048;
            Request.ModelTimeoutSeconds = 300;
            Request.OllamaKeepAlive = "0s";
            Request.OllamaNumGpu = 0;
            Request.IncludeMemory = true;
            Request.SaveToMemory = true;
            statusText = "Low GPU preset active: diagnostics only, one proposal pass, small context, CPU-only Ollama, and unload after each model.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Applies balanced preset for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void ApplyBalancedPreset()
    {
        try
        {
            Request.MaxRounds = 1;
            Request.MaxOutputTokens = 65536;
            Request.MaxParallelModels = 1;
            Request.ResourceLoadPercent = 75;
            Request.MaxContextTokens = 65536;
            Request.ModelTimeoutSeconds = 900;
            Request.OllamaKeepAlive = null;
            Request.OllamaNumGpu = null;
            Request.IncludeMemory = true;
            Request.SaveToMemory = true;
            statusText = "Balanced preset active: 64K source-generation floor, sequential models, one review round, normal Ollama GPU scheduling.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Applies generation preset for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void ApplyGenerationPreset()
    {
        try
        {
            Request.MaxRounds = 1;
            Request.MaxOutputTokens = 262144;
            Request.MaxParallelModels = 1;
            Request.ResourceLoadPercent = 100;
            Request.MaxContextTokens = 262144;
            Request.ModelTimeoutSeconds = 1800;
            Request.OllamaKeepAlive = "0s";
            Request.OllamaNumGpu = null;
            Request.IncludeMemory = true;
            Request.SaveToMemory = true;
            statusText = "Generation preset active: 256K output/context, 100% of each configured hardware road, automatic GPU scheduling, bounded recovery fallback, and unload after each model.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Starts feature request chat for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void StartFeatureRequestChat()
    {
        try
        {
            ApplyGenerationPreset();
            SelectedCouncilConversation = null;
            Request.ContinueConversationId = null;
            Request.SaveToMemory = true;
            Request.IncludeMemory = true;
            Request.GenerateImplementationArtifact = false;
            Request.UserConfirmedArtifactBuild = false;
            Request.Prompt = CouncilText.FormatLines(new[]
            {
            "Start a new LocalGPT implementation-request council chat.",
            "",
            "Goal:",
            "Ask the council to identify one useful feature request, decide which part of LocalGPT owns it, and produce a grounded implementation plan.",
            "",
            "Requirements:",
            "- classify the target area: .NET/Blazor/ASP.NET Core, WinUI/WebView2, Minecraft builder, diagnostics/logging, or frontend UX",
            "- check the DevExpress inventory before proposing DevExpress APIs, reports, Office files, PDFs, or document components",
            "- keep DevExpress Office/report/PDF/file generation in ASP.NET Core backend services with safe download links",
            "- prototype requested features in a harmless sandbox artifact or temporary workspace before integrating into the real project",
            "- never self-expand LocalGPT or integrate generated features without explicit user permission",
            "- never overrule a user decision that denies or limits expansion",
            "- separate current implementation facts from proposed work",
            "- list exact files or services likely to change",
            "- include a small user decision poll if ownership or scope is uncertain",
            "- request a downloadable example file when a .NET/Blazor/ASP.NET Core sketch would help",
            "- mark unsupported claims under Needs verification"
        });
            statusText = "Started a new implementation-request council chat. Review the prompt and explicitly enable a bounded artifact only for the run that should create one.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StartFeatureRequestChat");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }


    /// <summary>
    /// Approves change review for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task ApproveChangeReviewAsync()
    {
        var review = LastResult?.ChangeReview;
        if (review is null || review.ApprovalConsumed || review.Status != CodeGenerationReviewStatuses.AwaitingUserDecision)
            return;

        var buildAfterGeneration = BuildApprovedReview;
        BuildApprovedReview = false;
        isBusy = true;
        statusText = "Executing the exact approved change review in the bounded artifact workspace...";
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            LastGenerationExecution = await CodeGenerationWorkflow.ExecuteReviewAsync(
                review.Id,
                new ExecuteCodeGenerationReviewRequest
                {
                    ExpectedReviewHash = review.ReviewHash,
                    UserConfirmed = true,
                    BuildAfterGeneration = buildAfterGeneration,
                    UserConfirmedBuild = buildAfterGeneration,
                    DecisionNote = buildAfterGeneration
                        ? "Approved in the AI Council heartbeat UI, including the bounded .NET build."
                        : "Approved in the AI Council heartbeat UI for source generation only."
                }).ConfigureAwait(false);

            var refreshed = await CodeGenerationWorkflow.GetReviewAsync(review.Id).ConfigureAwait(false);
            if (LastResult is not null)
                LastResult.ChangeReview = refreshed;

            if (LastGenerationExecution is { DownloadUrl.Length: > 0 } execution && LastResult is not null)
            {
                LastResult.Artifacts.RemoveAll(artifact => artifact.DownloadUrl.Equals(execution.DownloadUrl, StringComparison.OrdinalIgnoreCase));
                LastResult.Artifacts.Add(new CouncilArtifact
                {
                    Name = execution.ZipFileName,
                    Kind = "Reviewed code-generation workspace",
                    FilePath = execution.WorkspacePath,
                    DownloadUrl = execution.DownloadUrl,
                    Summary = $"Review {execution.ReviewId} generated with status {execution.Status}. Build: {execution.BuildStatus}."
                });
            }

            statusText = $"Change review completed with status {LastGenerationExecution.Status}.";
            Notifier.ShowSuccess(toastName, "The exact reviewed payload was generated in the bounded artifact workspace.", "Review completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Change-review execution failed for review {ReviewId}; reviewed source content was omitted from logs.", review.Id);
            var refreshed = await CodeGenerationWorkflow.GetReviewAsync(review.Id).ConfigureAwait(false);
            if (LastResult is not null)
                LastResult.ChangeReview = refreshed;
            statusText = "Change-review execution failed. Review local application logs using the review ID.";
            Notifier.ShowError(toastName, "Generation failed. See local application logs for technical details.", "Review failed");
        }
        finally
        {
            isBusy = false;
            BuildApprovedReview = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Rejects change review for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task RejectChangeReviewAsync()
    {
        var review = LastResult?.ChangeReview;
        if (review is null || review.ApprovalConsumed || review.Status != CodeGenerationReviewStatuses.AwaitingUserDecision)
            return;

        isBusy = true;
        BuildApprovedReview = false;
        statusText = "Rejecting the current change review...";
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            var rejected = await CodeGenerationWorkflow.RejectReviewAsync(
                review.Id,
                new RejectCodeGenerationReviewRequest
                {
                    ExpectedReviewHash = review.ReviewHash,
                    UserConfirmed = true,
                    DecisionNote = "Rejected in the AI Council heartbeat UI."
                }).ConfigureAwait(false);
            if (LastResult is not null)
                LastResult.ChangeReview = rejected;
            LastGenerationExecution = null;
            statusText = "Change review rejected. No reviewed files were written.";
            Notifier.ShowSuccess(toastName, "The review was rejected without generating or building its payload.", "Review rejected");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Change-review rejection failed for review {ReviewId}.", review.Id);
            statusText = "Could not reject the review. Review local application logs.";
            Notifier.ShowError(toastName, "The review could not be rejected. See local application logs.", "Rejection failed");
        }
        finally
        {
            isBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Retrieves generation download URL for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="result">Result value supplied to the model council operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetGenerationDownloadUrl(CodeGenerationExecutionResult result) =>
        Navigation.ToAbsoluteUri(result.DownloadUrl).ToString();

    /// <summary>
    /// Retrieves artifact URL for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="artifact">Artifact value supplied to the model council operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetArtifactUrl(CouncilArtifact artifact)
    {
        try
        {
            return Navigation.ToAbsoluteUri(artifact.DownloadUrl).ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StartFeatureRequestChat");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return string.Empty;
        }
    }



    /// <summary>
    /// Performs exclude model for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="modelName">Model name value supplied to the model council operation and used when producing its result.</param>
    void ExcludeModel(string modelName)
    {
        try
        {
            if (SelectedModels.Count <= 1)
            {
                statusText = "Keep at least one council model selected.";
                return;
            }

            if (SelectedModels.Remove(modelName))
            {
                Request.Prompt = $"{Request.Prompt}\n\nUser decision for the next council round:\nExclude `{modelName}` from the council unless I re-add it. Remaining members must acknowledge the exclusion and continue from the prior transcript.";
                statusText = $"Excluded {modelName} from the next round.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Applies poll option for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="option">Option value supplied to the model council operation and used when producing its result.</param>
    void ApplyPollOption(CouncilUserPollOption option)
    {
        try
        {
            if (option.Kind == CouncilUserPollOptionKind.ExcludeUnavailableMembers && LastResult is not null)
            {
                foreach (var modelName in LastResult.Steps
                    .Where(step => !string.IsNullOrWhiteSpace(step.Error))
                    .Select(step => step.ModelName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList())
                {
                    if (SelectedModels.Count > 1)
                        SelectedModels.Remove(modelName);
                }
            }

            Request.Prompt = $"{Request.Prompt}\n\nUser decision for the next council round:\n{option.FollowUpPrompt}";
            CustomPollFeedback = string.Empty;
            statusText = $"Poll choice added: {option.Label}. Run Council again to continue.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Applies custom poll feedback for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    void ApplyCustomPollFeedback()
    {
        try
        {
            var feedback = CustomPollFeedback.Trim();
            if (string.IsNullOrWhiteSpace(feedback))
            {
                statusText = "Write a custom council direction before adding it.";
                return;
            }

            Request.Prompt = $"{Request.Prompt}\n\nUser custom decision for the next council round:\n{feedback}\n\nAll council members must treat this typed feedback as binding implementation guidance unless the user changes it later.";
            CustomPollFeedback = string.Empty;
            statusText = "Custom poll feedback added. Run Council again to continue.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Performs run UI action for <see cref="ModelCouncil"/>, keeping the operation consistent with the state and invariants of the surrounding model council workflow.
    /// </summary>
    /// <param name="action">Action value supplied to the model council operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the model council operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    async Task RunUiActionAsync(Func<Task> action, string operation)
    {
        ArgumentNullException.ThrowIfNull(action);
        isBusy = true;
        ComponentActivity.RecordInformation(nameof(ModelCouncil), operation, "The AI Council UI operation started.");
        try
        {
            await action().ConfigureAwait(false);
            ComponentActivity.RecordInformation(nameof(ModelCouncil), operation, "The AI Council UI operation completed.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("AI Council UI operation {Operation} was cancelled.", operation);
            statusText = "The operation was cancelled.";
            ComponentActivity.RecordWarning(nameof(ModelCouncil), operation, "The AI Council UI operation was cancelled.");
            Notifier.ShowWarning(toastName, statusText, "Operation cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AI Council UI operation {Operation} failed; prompt and model output were omitted from logs.", operation);
            statusText = "The operation failed. See local application logs for technical details.";
            ComponentActivity.RecordFailure(nameof(ModelCouncil), operation, ex);
            Notifier.ShowError(toastName, statusText, "Operation failed");
        }
        finally
        {
            isBusy = false;
        }
    }

    }
}
