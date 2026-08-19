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
    /// Handles the after render async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (isDisposed)
            return;

        try
        {
            if (firstRender)
            {
                interactiveAttached = true;
                chatInteropReference ??= DotNetObjectReference.Create(this);
                if (AutoStartCouncilStarter || !string.IsNullOrWhiteSpace(RequestedCouncilStarterKey))
                    await JS.InvokeVoidAsync("localGptChatUi.prepareDirectCouncilStarter").ConfigureAwait(false);
                Logger.LogInformation($"Chat interactive render attached; waiting for the DXAiChat control before background work starts.");
                ComponentActivity.RecordInformation(nameof(Chat), "InteractiveAttach", "The Chat page attached to the interactive circuit and is waiting for its chat control.");
                if (!initialStateInitializationStarted)
                {
                    initialStateInitializationStarted = true;
                    TaskRunner.Run(
                        nameof(Chat),
                        "InitializeChatState",
                        InitializeChatStateAsync,
                        componentLifetimeCts.Token);
                }
                ScheduleChatRuntimeActivation();
            }

            if (chatInteropReference is not null)
            {
                await JS.InvokeVoidAsync(
                    "localGptChatUi.registerCouncilComposer",
                    chatInteropReference,
                    IsLiveCouncilInteractionAvailable).ConfigureAwait(false);
            }

            await JS.InvokeVoidAsync(
                "localGptReconnect.setCouncilRun",
                ResolveRunningCouncilRunId()?.ToString()).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) when (isDisposed)
        {
            // The browser circuit disconnected while the Chat page was being disposed.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not finish the Chat interactive attach or live Council composer sequence.");
            ComponentActivity.RecordFailure(nameof(Chat), "InteractiveAttach", ex);
            if (firstRender)
                Notifier.ShowError(toastName, "Chat could not finish attaching to the interactive UI. See local logs for details.", "Chat attach error");
        }
    }

    /// <summary>
    /// Performs chat initialized for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ChatInitialized()
    {
        try
        {
            chatControlInitialized = true;
            Logger.LogInformation($"DXAiChat initialized; activation is queued until the DevExpress initialization callback has returned.");
            ComponentActivity.RecordInformation(nameof(Chat), nameof(ChatInitialized), "The DXAiChat control initialized; runtime activation was queued outside its initialization callback.");
            ScheduleChatRuntimeActivation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in ChatInitialized: {Message}", ex.Message);
            ComponentActivity.RecordFailure(nameof(Chat), nameof(ChatInitialized), ex);
            Notifier.ShowError(toastName, "The chat control could not finish initializing. See local application logs for technical details.", "Chat initialization failed");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs schedule chat runtime activation for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    private void ScheduleChatRuntimeActivation()
    {
        if (chatRuntimeActivationScheduled || chatRuntimeStarted || isDisposed || !interactiveAttached || !chatControlInitialized || !initialStateReady)
            return;

        chatRuntimeActivationScheduled = true;
        TaskRunner.Run(
            nameof(Chat),
            "ActivateChatRuntime",
            async cancellationToken =>
            {
                // Do not call LoadMessages/SaveMessages from inside DxAIChat.Initialized. A short
                // asynchronous boundary lets DevExpress finish its own render and JS attachment.
                await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested || isDisposed)
                    return;
                await InvokeAsync(TryStartChatRuntimeAsync).ConfigureAwait(false);
            },
            componentLifetimeCts.Token);
    }

    /// <summary>
    /// Attempts to start chat runtime for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task TryStartChatRuntimeAsync()
    {
        if (chatRuntimeStarted || isDisposed || !interactiveAttached || !chatControlInitialized)
            return;
        if (DxAiChat is null)
        {
            chatRuntimeActivationScheduled = false;
            return;
        }

        chatRuntimeStarted = true;
        try
        {
            ApplyDiagnosticQueryOptions(selectSession: true);
            var initialMessages = ChatClientProvider?.SelectedSession?.Messages?.ToList() ?? [];
            var explicitRejoinRequested = RejoinCouncilRunId is not null;
            var initialMessagesLoaded = false;
            if (!explicitRejoinRequested && initialMessages.Count > 0)
            {
                DxAiChat.LoadMessages(initialMessages);
                initialMessagesLoaded = true;
            }
            RefreshFeedbackTargets(initialMessages);

            var selectedSessionIsCouncil = string.Equals(
                ChatClientProvider?.SelectedSession?.Name,
                Catalog.CouncilSessionName,
                StringComparison.OrdinalIgnoreCase);
            var linkedLiveCouncil = selectedSessionIsCouncil
                ? initialMessages
                    .Select(message => ResolveLiveCouncilMessage(message.Content))
                    .LastOrDefault(session => session?.IsRunning == true)
                : null;
            if (linkedLiveCouncil is not null)
            {
                SelectedCouncilRunId = linkedLiveCouncil.RunId;
                RejoinCouncilRunId = linkedLiveCouncil.RunId;
            }

            if (RejoinCouncilRunId is Guid rejoinRunId)
                await AttachToLiveCouncilSessionAsync(rejoinRunId, reloadChatControl: !initialMessagesLoaded).ConfigureAwait(false);

            StartAutoSaveLoop();
            StartInitialModelRefresh();
            ScheduleDirectCouncilStarterDispatch();
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            Logger.LogInformation($"Chat runtime activated after DevExpress initialization completed; the UI remains responsive while supervised work runs off-dispatcher.");
            ComponentActivity.RecordInformation(nameof(Chat), nameof(TryStartChatRuntimeAsync), "The Chat runtime activated after the DevExpress initialization callback completed.");
        }
        catch (Exception ex)
        {
            chatRuntimeStarted = false;
            chatRuntimeActivationScheduled = false;
            Logger.LogError(ex, $"{nameof(TryStartChatRuntimeAsync)} failed while activating the interactive chat runtime.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(TryStartChatRuntimeAsync), ex);
            Notifier.ShowError(toastName, "Chat activation failed. See local logs for details.", "Chat activation");
        }
    }



    /// <summary>
    /// Performs clear history for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ClearHistoryAsync()
    {
        try
        {
            if (ChatClientProvider?.SelectedSession is null || DxAiChat is null)
                return;

            await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);
            ChatClientProvider.SelectedSession.Messages.Clear();
            ActiveConversationId = null;
            SessionContext.SetConversation(null);
            SelectedConversation = null;
            FeedbackTargets.Clear();
            SavedFeedback.Clear();
            SelectedFeedbackSortOrder = null;
            FeedbackComment = string.Empty;
            feedbackStatus = string.Empty;
            lastSavedSignature = string.Empty;
            memoryStatus = "New empty chat started.";
            DxAiChat.LoadMessages(ChatClientProvider.SelectedSession.Messages);
            RejoinCouncilRunId = null;
            AttachedLiveCouncilRunId = null;
            attachedLiveCouncilSnapshot = null;
            ownsLiveCouncilStream = false;
            lastAttachedLiveCouncilUpdatedAtUtc = default;
            SelectedCouncilRunId = null;
            LoadCouncilRunConfiguration(null);
            await JS.InvokeVoidAsync("localGptChatUi.clearLiveUserMessages").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Adds architecture poll to chat for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task AddArchitecturePollToChatAsync()
    {
        try
        {
            if (ChatClientProvider?.SelectedSession is null || DxAiChat is null)
                return;

            var messages = DxAiChat.SaveMessages().ToList();
            var pollMessage = BuildArchitecturePollMessage();
            messages.Add(new BlazorChatMessage(ChatRole.User, pollMessage, new List<AIChatUploadFileInfo>()));
            ChatClientProvider.SelectedSession.Messages.Clear();
            ChatClientProvider.SelectedSession.Messages.AddRange(messages);
            DxAiChat.LoadMessages(messages);
            await PersistMessagesAsync(messages, force: true, showToast: false).ConfigureAwait(false);
            ArchitecturePollStatus = "Architecture decision guidance was added to the DXAiChat conversation.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in AddArchitecturePollToChatAsync: {Message}", ex.Message);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    /// <summary>
    /// Builds architecture poll message for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string BuildArchitecturePollMessage()
    {
        try
        {
            var message = CouncilText.BuildArchitecturePollMessage(
                ArchitectureLanguageToolchain,
                ArchitectureUiStack,
                ArchitectureSolutionShape,
                ArchitectureRenderMode,
                ArchitectureReferenceLook,
                AllowCouncilToChooseSandboxDetails,
                ArchitecturePollNotes,
                Logger);
            Logger.LogDebug($"{nameof(BuildArchitecturePollMessage)} created the architecture decision message without logging its content.");
            return message;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(BuildArchitecturePollMessage)} failed; architecture choices were omitted from logs.");
            Notifier.ShowError(toastName, "The architecture decision message could not be created. See local logs for details.", "Architecture message failed");
            return string.Empty;
        }
    }

    /// <summary>
    /// Performs reset architecture poll for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    private void ResetArchitecturePoll()
    {
        try
        {
            ArchitectureLanguageToolchain = Catalog.ArchitectureLanguageToolchainOptions[0];
            ArchitectureUiStack = Catalog.ArchitectureUiStackOptions[0];
            ArchitectureSolutionShape = Catalog.ArchitectureSolutionShapeOptions[0];
            ArchitectureRenderMode = Catalog.ArchitectureRenderModeOptions[0];
            ArchitectureReferenceLook = Catalog.ArchitectureReferenceLookOptions[0];
            ArchitecturePollNotes = string.Empty;
            AllowCouncilToChooseSandboxDetails = false;
            ArchitecturePollStatus = "Architecture choices reset to ask-at-runtime defaults.";
            Logger.LogDebug($"{nameof(ResetArchitecturePoll)} restored the architecture poll defaults.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(ResetArchitecturePoll)} failed.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(ResetArchitecturePoll), exception);
            Notifier.ShowError(toastName, "Architecture choices could not be reset. See local logs for details.", "Reset failed");
        }
    }

    /// <summary>
    /// Handles the provider selection changed lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnProviderSelectionChanged(ChangeEventArgs args)
    {
        try
        {
            var selectedName = args.Value?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName)) return;
            var selectedSession = ModelsList.FirstOrDefault(session =>
                string.Equals(session.Name, selectedName, StringComparison.Ordinal));
            if (selectedSession is null)
            {
                Logger.LogWarning("The selected AI provider session was no longer available.");
                return;
            }

            await OnModelChanged(selectedSession).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Selecting an AI provider from the Chat configuration failed.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(OnProviderSelectionChanged), exception);
            Notifier.ShowError(toastName, "The AI provider selection could not be changed. See local logs for details.", "Provider selection failed");
        }
    }

    /// <summary>
    /// Performs provider option label for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="session">Session value supplied to the chat operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ProviderOptionLabel(ChatClientSession session)
    {
        var provider = string.IsNullOrWhiteSpace(session.Provider) ? session.Name : session.Provider;
        var model = string.IsNullOrWhiteSpace(session.ModelName) ? session.Name : session.ModelName;
        return string.IsNullOrWhiteSpace(session.Endpoint)
            ? $"{model} — {provider}"
            : $"{model} — {provider} — {session.Endpoint}";
    }

    /// <summary>
    /// Handles the model changed lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnModelChanged(ChatClientSession value)
    {
        try
        {
            if (ChatClientProvider is null || DxAiChat is null || value is null)
                return;

            hasUserSelectedSession = true;

            if (!string.IsNullOrWhiteSpace(DiagnosticRequestedSessionName) &&
                ! (MatchesRequestedDiagnosticSession(value, DiagnosticRequestedSessionName) ?? false))
            {
                var diagnosticSession = FindRequestedDiagnosticSession(DiagnosticRequestedSessionName);
                if (diagnosticSession is not null)
                {
                    ChatClientProvider.SelectedSession = diagnosticSession;
                    ChatClientProvider.LockedSessionName = diagnosticSession.Name;
                    DxAiChat.LoadMessages(diagnosticSession.Messages);
                    modelStatus = $"Diagnostic session locked to {diagnosticSession.Name}.";
                }

                return;
            }

            var currentMessages = DxAiChat.SaveMessages().ToList();
            SaveMessagesForSelectedSession(currentMessages);
            await PersistMessagesAsync(currentMessages, force: true, showToast: false).ConfigureAwait(false);

            ChatClientProvider.LockedSessionName = null;
            ChatClientProvider.ForcedMaxOutputTokens = null;
            ChatClientProvider.ForcedMaxPromptCharacters = null;
            ChatClientProvider.SuppressBootstrapContext = false;
            ChatClientProvider.SelectedSession = value;
            if (!string.Equals(value.Name, Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase))
            {
                AttachedLiveCouncilRunId = null;
                RejoinCouncilRunId = null;
                attachedLiveCouncilSnapshot = null;
                ownsLiveCouncilStream = false;
                lastAttachedLiveCouncilUpdatedAtUtc = default;
            }

            if (ReuseContextWhenSwitching && value.Messages.Count == 0 && currentMessages.Count > 0)
                value.Messages.AddRange(currentMessages);

            DxAiChat.LoadMessages(value.Messages);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    /// <summary>
    /// Handles the chat configuration summary clicked async lifecycle or event notification for <see cref="Chat"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task OnChatConfigurationSummaryClickedAsync()
    {
        chatConfigurationOpen = !chatConfigurationOpen;
        if (chatConfigurationOpen && !isDisposed)
        {
            TaskRunner.Run(
                nameof(Chat),
                "RefreshServiceBackedChatConfigurationOnOpen",
                RefreshServiceBackedChatConfigurationAsync,
                componentLifetimeCts.Token);

            if (!isModelRefreshBusy)
            {
                TaskRunner.Run(
                    nameof(Chat),
                    "RefreshProvidersOnChatConfigurationOpen",
                    cancellationToken => DiscoverAndApplyOllamaModelsAsync(showToast: false, cancellationToken),
                    componentLifetimeCts.Token);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reloads the service-backed Chat configuration lists whenever the configuration ribbon opens without overwriting unsaved manual runtime values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that stops the refresh when the Chat component is disposed or the owning operation is cancelled.</param>
    /// <returns>A task that completes after the latest teams, presets, memory and project lists have been synchronized.</returns>
    private async Task RefreshServiceBackedChatConfigurationAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref chatConfigurationRefreshGate, 1) != 0)
            return;

        try
        {
            try
            {
                await RefreshCouncilTeamItemsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
            {
                Logger.LogDebug("Council team refresh was cancelled while opening Chat configuration.");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Council teams could not be refreshed while opening Chat configuration.");
                ComponentActivity.RecordWarning(nameof(Chat), "RefreshCouncilTeamsOnConfigurationOpen", "Council teams could not be refreshed; the current in-memory team selection remains available.");
            }

            await RefreshModelPresetItemsAsync(cancellationToken).ConfigureAwait(false);
            await LoadHardwarePerformancePresetsAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await InvokeAsync(() => AllPromptSuggestions = Catalog.GetSuggestion()).ConfigureAwait(false);
                await LoadPersistentPromptSuggestionsAsync(cancellationToken).ConfigureAwait(false);
                await InvokeAsync(RefreshPromptSuggestions).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
            {
                Logger.LogDebug("Prompt starter refresh was cancelled while opening Chat configuration.");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Prompt starters could not be refreshed while opening Chat configuration.");
            }

            try
            {
                await LoadChatProjectsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
            {
                Logger.LogDebug("Project refresh was cancelled while opening Chat configuration.");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Projects could not be refreshed while opening Chat configuration.");
                ComponentActivity.RecordWarning(nameof(Chat), "RefreshProjectsOnConfigurationOpen", "Project choices could not be refreshed; the current project selection remains available.");
            }

            await RefreshMemoryAsync(cancellationToken).ConfigureAwait(false);
            if (!isDisposed)
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref chatConfigurationRefreshGate, 0);
        }
    }

    /// <summary>
    /// Refreshes Ollama models for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RefreshOllamaModelsAsync()
    {
        await DiscoverAndApplyOllamaModelsAsync(showToast: true, componentLifetimeCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes Ollama models for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="showToast">Value indicating whether show toast should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RefreshOllamaModelsAsync(bool showToast)
    {
        await DiscoverAndApplyOllamaModelsAsync(showToast, componentLifetimeCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers and apply Ollama models for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="showToast">Value indicating whether show toast should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DiscoverAndApplyOllamaModelsAsync(bool showToast, CancellationToken cancellationToken)
    {
        if (ChatClientProvider is null || isDisposed || cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await InvokeAsync(() =>
            {
                isModelRefreshBusy = true;
                StateHasChanged();
            }).ConfigureAwait(false);

            // Discovery runs from the supervised task context. Only the small state-application
            // phase is marshalled back to the renderer, so a slow Ollama probe cannot monopolize
            // the Blazor circuit or freeze unrelated controls.
            var discovered = await CouncilService.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            if (isDisposed || cancellationToken.IsCancellationRequested)
                return;

            await InvokeAsync(() =>
            {
                ApplyDiscoveredOllamaModels(discovered, showToast);
                // This refresh is started outside the Blazor event callback by the supervised task runner.
                // Explicitly invalidate the component so newly discovered provider sessions are visible
                // immediately when Chat configuration opens, without requiring a second ribbon/menu click.
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
        {
            Logger.LogDebug("Ollama model discovery stopped during navigation or shutdown.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RefreshOllamaModelsAsync failed: {Message}", ex.Message);
            ComponentActivity.RecordFailure(nameof(Chat), nameof(DiscoverAndApplyOllamaModelsAsync), ex);
            await InvokeAsync(() =>
            {
                modelStatus = "Model refresh failed. Configured sessions remain available; see local logs for details.";
                if (showToast)
                    Notifier.ShowError(toastName, modelStatus, "Model refresh failed");
            }).ConfigureAwait(false);
        }
        finally
        {
            if (!isDisposed)
            {
                try
                {
                    await InvokeAsync(() =>
                    {
                        isModelRefreshBusy = false;
                        StateHasChanged();
                    }).ConfigureAwait(false);
                }
                catch (InvalidOperationException) when (isDisposed) { }
            }
        }
    }

    /// <summary>
    /// Applies discovered Ollama models for <see cref="Chat"/>, keeping the operation consistent with the state and invariants of the surrounding chat workflow.
    /// </summary>
    /// <param name="discovered">Multi model council model candidate dependency used by the chat workflow to provide the corresponding application capability.</param>
    /// <param name="showToast">Value indicating whether show toast should apply to this operation.</param>
    private void ApplyDiscoveredOllamaModels(IReadOnlyList<MultiModelCouncilModelCandidate> discovered, bool showToast)
    {
        OllamaCandidates = discovered
            .Where(candidate => candidate.IsInstalled || candidate.IsConfigured)
            .OrderByDescending(candidate => candidate.IsLoaded)
            .ThenBy(candidate => candidate.Endpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        modelSelectionNotice = string.Empty;
        hadUnavailableProviderSelections = false;
        SelectedCouncilModelNames = ReconcileAvailableProviderSelectionKeys(SelectedCouncilModelNames, "Council");
        DiagnosticCouncilModelNames = ReconcileAvailableProviderSelectionKeys(DiagnosticCouncilModelNames, "Diagnostic Council");
        SynchronizeSelectedCouncilRoutes();

        if (SelectedCouncilModelNames.Count == 0 && DiagnosticCouncilModelNames.Count == 0 && !hadUnavailableProviderSelections)
        {
            var preferred = OllamaCandidates.FirstOrDefault(candidate => candidate.IsInstalled && candidate.ModelName.Equals("gpt-oss:20b", StringComparison.OrdinalIgnoreCase))
                ?? OllamaCandidates.FirstOrDefault(candidate => candidate.IsInstalled);
            if (preferred is not null)
                SelectedCouncilModelNames.Add(preferred.SelectionKey);
        }

        var preferCouncilByDefault = !hasUserSelectedSession
            && DiagnosticCouncilModelNames.Count == 0
            && OllamaCandidates.Count(candidate => candidate.IsInstalled) >= 2;
        RebuildDynamicSessions();
        if (preferCouncilByDefault && ChatClientProvider is not null)
        {
            var councilSession = ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
                session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase));
            if (councilSession is not null)
            {
                ChatClientProvider.SelectedSession = councilSession;
                // A route-driven Council starter must not force-load messages while the
                // DevExpress provider is still being rebuilt. That race throws before the
                // starter scheduler runs. The selected composite session is sufficient; the
                // normal render boundary updates the visible transcript.
                if (DxAiChat is not null && !AutoStartCouncilStarter && string.IsNullOrWhiteSpace(RequestedCouncilStarterKey))
                {
                    try
                    {
                        DxAiChat.LoadMessages(councilSession.Messages);
                    }
                    catch (InvalidOperationException exception)
                    {
                        Logger.LogDebug(exception, "Deferred Council message loading because the DevExpress chat control is still rendering.");
                    }
                }
            }
        }
        modelStatus = OllamaCandidates.Count == 0
            ? "No configured or discovered provider models detected."
            : $"Detected {OllamaCandidates.Count(candidate => candidate.IsInstalled)} discovered/reachable model(s) and {OllamaCandidates.Count(candidate => candidate.IsConfigured && !candidate.IsInstalled)} configured model(s) awaiting call-time verification across {OllamaCandidates.Select(candidate => $"{candidate.ProviderKind}|{candidate.Endpoint}").Distinct(StringComparer.OrdinalIgnoreCase).Count()} provider host(s). Provider-qualified entries are available to AI Council without same-name collisions.";
        if (!string.IsNullOrWhiteSpace(modelSelectionNotice))
            modelStatus = $"{modelStatus} {modelSelectionNotice}";
        if (DiagnosticCouncilModelNames.Count > 0)
            modelStatus = $"Diagnostic council model override: {CouncilText.FormatInlineNameList(DiagnosticCouncilModelNames)}. LocalGPT will not auto-select a different council member for this run.";

        if (AutoStartCouncilStarter && !directCouncilStarterDispatched && !directCouncilStarterDispatching)
            ScheduleDirectCouncilStarterDispatch();

        if (showToast)
            Notifier.ShowSuccess(toastName, modelStatus, "Provider models refreshed");
    }

    /// <summary>Queues a renderer-affine retry for the route-requested Council starter.</summary>
    private void ScheduleDirectCouncilStarterDispatch()
    {
        if (!AutoStartCouncilStarter || directCouncilStarterDispatched || directCouncilStarterDispatching ||
            string.IsNullOrWhiteSpace(RequestedCouncilStarterKey) || isDisposed || directCouncilStarterDispatchAttempts >= 12)
            return;

        directCouncilStarterDispatchAttempts++;
        var attempt = directCouncilStarterDispatchAttempts;
        TaskRunner.Run(
            nameof(Chat),
            $"DirectCouncilStarter-{attempt}",
            async cancellationToken =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 + (attempt * 100)), cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested || isDisposed || directCouncilStarterDispatched)
                    return;
                await InvokeAsync(AutoStartRequestedCouncilPromptAsync).ConfigureAwait(false);
            },
            componentLifetimeCts.Token);
    }

    /// <summary>Finds and starts the direct Council prompt requested by the installer or home-page route.</summary>
    /// <returns>A task that completes after the prompt is submitted or a retry is scheduled.</returns>
    private async Task AutoStartRequestedCouncilPromptAsync()
    {
        if (directCouncilStarterDispatched || directCouncilStarterDispatching || string.IsNullOrWhiteSpace(RequestedCouncilStarterKey))
            return;

        var retryRequested = false;
        directCouncilStarterDispatching = true;
        try
        {
            var starter = AllPromptSuggestions.FirstOrDefault(item =>
                item.StartsCouncilDirectly && string.Equals(item.Key, RequestedCouncilStarterKey.Trim(), StringComparison.OrdinalIgnoreCase));
            if (starter is null)
            {
                modelStatus = $"Council starter '{RequestedCouncilStarterKey}' was not found. Select a maintained starter below.";
                return;
            }

            var submitted = await StartCouncilPromptAsync(starter, StartNewCouncilChat).ConfigureAwait(true) /* renderer-affine direct-start continuation */;
            retryRequested = !submitted && directCouncilStarterDispatchAttempts < 12;
        }
        catch (InvalidOperationException exception)
        {
            Logger.LogWarning(exception, "Direct Council starter {StarterKey} ran before the interactive renderer was ready; retrying.", RequestedCouncilStarterKey);
            retryRequested = directCouncilStarterDispatchAttempts < 12;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Direct Council starter {StarterKey} failed.", RequestedCouncilStarterKey);
            modelStatus = "The requested Council starter failed. Review LocalGPT logs or start it from the visible prompt card.";
        }
        finally
        {
            directCouncilStarterDispatching = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true) /* renderer-affine direct-start completion */;
        }

        if (retryRequested)
            ScheduleDirectCouncilStarterDispatch();
    }

    /// <summary>Selects AI Council, optionally clears the Council chat, and submits one maintained team pre-prompt.</summary>
    /// <param name="starter">Maintained direct Council starter prompt.</param>
    /// <param name="startFresh">Whether to create a fresh Council chat before submission.</param>
    /// <returns>A task whose result is true when the browser accepted and submitted the prompt.</returns>
    private async Task<bool> StartCouncilPromptAsync(PromptSuggestion starter, bool startFresh)
    {
        ArgumentNullException.ThrowIfNull(starter);
        if (ChatClientProvider is null || DxAiChat is null || !chatRuntimeStarted)
        {
            modelStatus = "AI Council is still initializing. The requested starter will retry automatically.";
            return false;
        }

        var councilSession = ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
            session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase));
        if (councilSession is null)
        {
            modelStatus = "The AI Council chat session is not available yet. The requested starter will retry after model discovery.";
            return false;
        }

        var requestedTeam = starter.TeamKeys.FirstOrDefault(teamKey =>
            CouncilTeams.Any(team => string.Equals(team.Key, teamKey, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(RequestedCouncilTeamKey) &&
            CouncilTeams.Any(team => string.Equals(team.Key, RequestedCouncilTeamKey, StringComparison.OrdinalIgnoreCase)))
            requestedTeam = RequestedCouncilTeamKey;
        if (!string.IsNullOrWhiteSpace(requestedTeam))
        {
            SelectedCouncilTeamKey = requestedTeam;
            RefreshPromptSuggestions();
        }

        ChatClientProvider.LockedSessionName = null;
        ChatClientProvider.SelectedSession = councilSession;
        hasUserSelectedSession = true;
        if (startFresh)
        {
            councilSession.Messages.Clear();
            ActiveConversationId = null;
            SessionContext.SetConversation(null);
            SelectedConversation = null;
            FeedbackTargets.Clear();
            SavedFeedback.Clear();
            SelectedFeedbackSortOrder = null;
            FeedbackComment = string.Empty;
            feedbackStatus = string.Empty;
            lastSavedSignature = string.Empty;
            RejoinCouncilRunId = null;
            AttachedLiveCouncilRunId = null;
            attachedLiveCouncilSnapshot = null;
            SelectedCouncilRunId = null;
            ownsLiveCouncilStream = false;
            lastAttachedLiveCouncilUpdatedAtUtc = default;
            LoadCouncilRunConfiguration(null);
            await JS.InvokeVoidAsync("localGptChatUi.clearLiveUserMessages").ConfigureAwait(true) /* renderer-affine chat reset */;
        }

        // Do not call DxAIChat.LoadMessages while the DevExpress control is changing providers.
        // During route-driven starts that call can race the control's internal render and raise an
        // InvalidOperationException before the composer accepts the prompt. The selected composite
        // session already controls the next request; a normal render boundary updates the visible UI.
        SavePreparationConfiguration();
        await InvokeAsync(StateHasChanged).ConfigureAwait(true) /* renderer-affine provider switch */;
        await JS.InvokeVoidAsync("localGptChatUi.prepareDirectCouncilStarter").ConfigureAwait(true) /* renderer-affine modal close */;
        await Task.Delay(240).ConfigureAwait(true) /* allow the DevExpress composer to re-render for the selected session */;
        var submitted = await JS.InvokeAsync<bool>("localGptChatUi.submitSuggestionOrPrompt", starter.Title, starter.PromptMessage).ConfigureAwait(true) /* renderer-affine browser submission */;
        directCouncilStarterDispatched = submitted;
        if (submitted)
        {
            modelStatus = $"Started '{starter.Title}' with Council team '{SelectedCouncilTeamKey}' and the currently selected reachable provider members.";
            ComponentActivity.RecordInformation(nameof(Chat), "DirectCouncilStarter", $"Started maintained Council prompt {starter.Key}.");
            return true;
        }

        if (!AutoStartCouncilStarter || directCouncilStarterDispatchAttempts >= 12)
        {
            await JS.InvokeVoidAsync("localGptChatUi.restoreComposerDraft", starter.PromptMessage).ConfigureAwait(true) /* renderer-affine composer fallback */;
            modelStatus = $"Prepared '{starter.Title}' in the composer. Press Send to start the Council run.";
            Notifier.ShowWarning(toastName, modelStatus, "Council starter prepared");
        }
        return false;
    }

    }
}
