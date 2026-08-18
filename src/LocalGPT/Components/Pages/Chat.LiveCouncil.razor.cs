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
    // Completed Council lanes remain cheap until the developer explicitly asks to inspect one.
    // Running lanes still stream immediately.
    private readonly HashSet<string> revealedCompletedCouncilActivityEvidence = new(StringComparer.Ordinal);

    private async Task RefreshHumanCollaborationAsync()
    {
        try
        {
            var snapshot = await HumanCollaboration.GetSnapshotAsync(includeResolved: false, take: 100).ConfigureAwait(false);
            var activeLiveSessions = CouncilLiveSessions.GetActiveSummaries();
            var activeLiveRunIds = activeLiveSessions.Select(session => session.RunId).ToHashSet();

            await InvokeAsync(() =>
            {
                collaborationSnapshot = snapshot;
                if (SelectedCouncilRunId is null && activeLiveSessions.FirstOrDefault() is { } newestLiveSession)
                    SelectedCouncilRunId = newestLiveSession.RunId;
                if (SelectedCouncilRunId is Guid selectedRunId
                    && collaborationSnapshot.ActiveRuns.All(run => run.RunId != selectedRunId)
                    && !activeLiveRunIds.Contains(selectedRunId))
                {
                    SelectedCouncilRunId = null;
                }

                LoadCouncilRunConfiguration(SelectedCouncilRunId);
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not refresh running council participation state; user and model content were omitted.");
        }
    }

    private void OnActiveCouncilRunChanged(ChangeEventArgs args)
    {
        SelectedCouncilRunId = Guid.TryParse(args.Value?.ToString(), out var runId) ? runId : null;
        LoadCouncilRunConfiguration(SelectedCouncilRunId);
    }

    private async Task OnBenchmarkStartedAsync(Guid runId)
    {
        SelectedCouncilRunId = runId;
        RejoinCouncilRunId = runId;
        AttachedLiveCouncilRunId = null;
        attachedLiveCouncilSnapshot = null;
        lastAttachedLiveCouncilUpdatedAtUtc = default;
        await RefreshHumanCollaborationAsync().ConfigureAwait(false);
        var attached = await AttachToLiveCouncilSessionAsync(runId).ConfigureAwait(false);
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        if (!attached)
            Logger.LogWarning("Benchmark Council {RunId} started, but the current browser circuit could not attach to its live UI.", runId);
        await InvokeAsync(() => JS.InvokeVoidAsync("localGptChatUi.prepareDirectCouncilStarter").AsTask()).ConfigureAwait(false);
        var isRunning = CouncilLiveSessions.GetSummary(runId)?.IsRunning == true;
        await InvokeAsync(() => JS.InvokeVoidAsync("localGptChatUi.refreshCouncilComposer", isRunning).AsTask()).ConfigureAwait(false);
        await InvokeAsync(() => Notifier.ShowSuccess(toastName, $"Benchmark Council {ShortCouncilRunId(runId)} is now visible in Chat.", "Benchmark joined")).ConfigureAwait(false);
    }

    private async Task JoinCouncilSessionAsync(Guid runId)
    {
        SelectedCouncilRunId = runId;
        await RejoinSelectedCouncilSessionAsync().ConfigureAwait(false);
        await InvokeAsync(() => JS.InvokeVoidAsync("localGptChatUi.prepareDirectCouncilStarter").AsTask()).ConfigureAwait(false);
    }

    private async Task RejoinSelectedCouncilSessionAsync()
    {
        var runId = SelectedCouncilRunId;
        if (runId is not Guid selectedRunId || CouncilLiveSessions.GetSummary(selectedRunId)?.IsRunning != true)
        {
            Notifier.ShowError(toastName, "The selected Council session is no longer running.", "Rejoin Council");
            return;
        }

        RejoinCouncilRunId = selectedRunId;
        AttachedLiveCouncilRunId = null;
        attachedLiveCouncilSnapshot = null;
        lastAttachedLiveCouncilUpdatedAtUtc = default;
        var attached = await AttachToLiveCouncilSessionAsync(selectedRunId).ConfigureAwait(false);
        if (!attached)
        {
            await InvokeAsync(() => Notifier.ShowWarning(toastName, "The Council is still server-owned, but this browser circuit could not attach. Reconnect the UI and try Rejoin again.", "Council rejoin interrupted")).ConfigureAwait(false);
            return;
        }
        await InvokeAsync(() => JS.InvokeVoidAsync("localGptChatUi.refreshCouncilComposer", true).AsTask()).ConfigureAwait(false);
        await InvokeAsync(() => Notifier.ShowSuccess(toastName, $"Rejoined Council {ShortCouncilRunId(selectedRunId)} with live stop, message and transcript controls.", "Council rejoined")).ConfigureAwait(false);
    }

    private async Task EnableHumanParticipationAsync()
    {
        try
        {
            var current = collaborationSnapshot.Profile;
            var enabled = new HumanCouncilParticipantProfile
            {
                Id = current.Id,
                DisplayName = string.IsNullOrWhiteSpace(current.DisplayName) ? "Human User" : current.DisplayName,
                RoleName = string.IsNullOrWhiteSpace(current.RoleName) ? "Human collaborator" : current.RoleName,
                Expertise = current.Expertise,
                WorkingStyle = current.WorkingStyle,
                IsEnabled = true,
                ProfileVersion = Math.Max(1, current.ProfileVersion),
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = string.IsNullOrWhiteSpace(current.DisplayName) ? "Human User" : current.DisplayName
            };
            var activeRun = ActiveCouncilRun;
            var activeRunId = activeRun?.RunId ?? ActiveLiveCouncilSession?.RunId;
            using var humanScope = HumanAmbientContext.PushHumanInteraction(
                RuntimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId),
                enabled.DisplayName,
                nameof(Chat),
                councilRunId: activeRunId,
                councilRound: activeRun?.CurrentRound ?? 0,
                phase: activeRun?.Phase ?? "Enable human Council participation");
            await HumanCollaboration.SaveProfileAsync(enabled).ConfigureAwait(false);
            Notifier.ShowSuccess(toastName, "You can now contribute to the selected running Council heartbeat.", "Human participation enabled");
            await RefreshHumanCollaborationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not enable the local human council participant profile.");
            Notifier.ShowError(toastName, "Human participation could not be enabled. Review LocalGPT logs.", "Human participation");
        }
    }

    private async Task QueueRunningCouncilContributionAsync()
    {
        var activeRun = ActiveCouncilRun;
        var activeRunId = activeRun?.RunId ?? ActiveLiveCouncilSession?.RunId;
        if (activeRunId is not Guid councilRunId || string.IsNullOrWhiteSpace(RunningCouncilContribution))
            return;
        if (!collaborationSnapshot.Profile.IsEnabled)
        {
            Notifier.ShowInfo(toastName, "Enable your Human Council Participant profile in the Human team panel first.", "Take Part");
            return;
        }

        var content = RunningCouncilContribution.Trim();
        try
        {
            using var humanScope = HumanAmbientContext.PushHumanInteraction(
                RuntimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId),
                collaborationSnapshot.Profile.DisplayName,
                nameof(Chat),
                councilRunId: councilRunId,
                councilRound: activeRun?.CurrentRound ?? 0,
                phase: activeRun?.Phase ?? "Rejoined live Council");
            await HumanCollaboration.QueueContributionAsync(councilRunId, content).ConfigureAwait(false);
            RunningCouncilContribution = string.Empty;
            Notifier.ShowSuccess(toastName, "Your message will enter the next council heartbeat without cancelling the current model.", "Human council contribution");
            ComponentActivity.RecordInformation(nameof(Chat), "QueueHumanContribution", "A human peer contribution was queued for the next active council heartbeat.");
            await RefreshHumanCollaborationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not queue a human council contribution from Chat; content was omitted from logs.");
            Notifier.ShowError(toastName, ex is InvalidOperationException ? ex.Message : "The contribution could not be queued. Review LocalGPT logs.", "Human council contribution");
            ComponentActivity.RecordFailure(nameof(Chat), "QueueHumanContribution", ex);
        }
    }


    [JSInvokable]
    public async Task<bool> QueueLiveCouncilUserMessageAsync(string content, IReadOnlyList<LiveCouncilUploadFile>? files)
    {
        var activeRun = ActiveCouncilRun;
        var activeRunId = activeRun?.RunId ?? ActiveLiveCouncilSession?.RunId;
        var uploadedFiles = files?.Where(file => file is not null && !string.IsNullOrWhiteSpace(file.Name)).ToList() ?? [];
        if (activeRunId is not Guid councilRunId || (string.IsNullOrWhiteSpace(content) && uploadedFiles.Count == 0) || isDisposed)
            return false;

        try
        {
            using var humanScope = HumanAmbientContext.PushHumanInteraction(
                RuntimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId),
                string.IsNullOrWhiteSpace(collaborationSnapshot.Profile.DisplayName)
                    ? "Human User"
                    : collaborationSnapshot.Profile.DisplayName,
                nameof(Chat),
                councilRunId: councilRunId,
                councilRound: activeRun?.CurrentRound ?? 0,
                phase: activeRun?.Phase ?? "Rejoined live Council");

            var normalizedContent = string.IsNullOrWhiteSpace(content) ? "Please review the attached files." : content.Trim();
            var deliveredContent = normalizedContent;
            ChatUploadWorkspaceResult? uploadWorkspace = null;
            if (uploadedFiles.Count > 0)
            {
                var workspaceInputs = uploadedFiles.Select(file => new ChatUploadWorkspaceInputFile(
                    file.Name.Trim(),
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType.Trim(),
                    file.SizeBytes > 0 ? file.SizeBytes : file.Data.LongLength,
                    file.Data)).ToArray();
                uploadWorkspace = await ChatUploadWorkspaces.CreateWorkspaceAsync(
                    normalizedContent,
                    workspaceInputs,
                    componentLifetimeCts.Token).ConfigureAwait(false);
                deliveredContent = $"{normalizedContent}\n\n{CouncilText.BuildUploadWorkspaceSystemPrompt(uploadWorkspace, Logger)}";
            }

            await HumanCollaboration.QueueUserMessageAsync(
                councilRunId,
                deliveredContent,
                componentLifetimeCts.Token).ConfigureAwait(false);

            var displayContent = BuildLiveCouncilUserDisplayContent(
                normalizedContent,
                uploadWorkspace?.Files.Select(file => file.RelativePath) ?? uploadedFiles.Select(file => file.Name));
            CouncilLiveSessions.AppendUserMessage(councilRunId, displayContent);

            var chatMessage = new BlazorChatMessage(
                ChatRole.User,
                displayContent,
                new List<AIChatUploadFileInfo>());
            PendingLiveCouncilUserMessages.Add(chatMessage);
            if (ChatClientProvider?.SelectedSession is not null)
                ChatClientProvider.SelectedSession.Messages.Add(chatMessage);

            // Keep the user message inside the authoritative Blazor/DevExpress message model. The old JavaScript
            // shadow bubble lived inside a renderer-owned message subtree and was repeatedly removed/re-added on
            // Council heartbeats, which produced visible flicker. Reload only for this explicit human send event.
            await InvokeAsync(() =>
            {
                LoadSelectedSessionMessages();
                StateHasChanged();
            }).ConfigureAwait(false);

            ComponentActivity.RecordInformation(
                nameof(Chat),
                "QueueLiveCouncilUserMessage",
                uploadedFiles.Count > 0
                    ? $"A direct user message with {uploadedFiles.Count} upload(s) was accepted into a new chat upload workspace for the running Council."
                    : "A direct user message was accepted for immediate model interruption/resume and subsequent Council context.");
            await RefreshHumanCollaborationAsync().ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (componentLifetimeCts.IsCancellationRequested || isDisposed)
        {
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not queue a direct user message or upload from the active Chat composer; content was omitted from logs.");
            ComponentActivity.RecordFailure(nameof(Chat), "QueueLiveCouncilUserMessage", ex);
            Notifier.ShowError(
                toastName,
                ex is InvalidOperationException ? ex.Message : "The message or attached files could not be added to the running Council. Review LocalGPT logs.",
                "Council user message");
            return false;
        }
    }

    /// <summary>
    /// Builds the visible user-message content used while a live upload workspace is already participating in a Council run.
    /// </summary>
    private string BuildLiveCouncilUserDisplayContent(string content, IEnumerable<string> fileNames)
    {
        return CouncilText.BuildAttachmentPresentation(content, fileNames);
    }

    private async Task RunUiActionAsync(Func<Task> action, string operation)
    {
        ArgumentNullException.ThrowIfNull(action);
        ComponentActivity.RecordInformation(nameof(Chat), operation, "The chat UI operation started.");
        try
        {
            await action().ConfigureAwait(false);
            ComponentActivity.RecordInformation(nameof(Chat), operation, "The chat UI operation completed.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Chat UI operation {Operation} was cancelled.", operation);
            ComponentActivity.RecordWarning(nameof(Chat), operation, "The chat UI operation was cancelled.");
            Notifier.ShowWarning(toastName, "The operation was cancelled.", "Operation cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chat UI operation {Operation} failed; prompt, model output, and preset content were omitted from logs.", operation);
            ComponentActivity.RecordFailure(nameof(Chat), operation, ex);
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }


    private void OnCouncilLiveSessionChanged(Guid runId)
    {
        if (isDisposed)
            return;

        var attachedToChangedRun = AttachedLiveCouncilRunId == runId || RejoinCouncilRunId == runId;
        if (!attachedToChangedRun || (ownsLiveCouncilStream && AttachedLiveCouncilRunId == runId))
        {
            ScheduleLiveCouncilListRefresh();
            return;
        }

        if (Interlocked.Exchange(ref liveCouncilRefreshScheduled, 1) != 0)
            return;

        try
        {
            TaskRunner.Run(
                nameof(Chat),
                "RefreshAttachedLiveCouncilSession",
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                        if (cancellationToken.IsCancellationRequested || isDisposed)
                            return;
                        _ = await AttachToLiveCouncilSessionAsync(runId).ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref liveCouncilRefreshScheduled, 0);
                        ScheduleLiveCouncilListRefresh();
                        if (!isDisposed
                            && CouncilLiveSessions.GetSummary(runId) is { } latest
                            && latest.UpdatedAtUtc > lastAttachedLiveCouncilUpdatedAtUtc)
                        {
                            OnCouncilLiveSessionChanged(runId);
                        }
                    }
                },
                componentLifetimeCts.Token);
        }
        catch (ObjectDisposedException) when (isDisposed)
        {
            Interlocked.Exchange(ref liveCouncilRefreshScheduled, 0);
        }
    }

    private void ScheduleLiveCouncilListRefresh()
    {
        if (isDisposed || Interlocked.Exchange(ref liveCouncilListRefreshScheduled, 1) != 0)
            return;

        try
        {
            TaskRunner.Run(
                nameof(Chat),
                "RefreshLiveCouncilSessionList",
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                        if (!cancellationToken.IsCancellationRequested && !isDisposed)
                        {
                            await InvokeAsync(() =>
                            {
                                if (SelectedCouncilRunId is null && CouncilLiveSessions.GetActiveSummaries().FirstOrDefault() is { } newestLiveSession)
                                    SelectedCouncilRunId = newestLiveSession.RunId;
                                StateHasChanged();
                            }).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref liveCouncilListRefreshScheduled, 0);
                    }
                },
                componentLifetimeCts.Token);
        }
        catch (ObjectDisposedException) when (isDisposed)
        {
            Interlocked.Exchange(ref liveCouncilListRefreshScheduled, 0);
        }
    }

    /// <summary>Attaches the current browser circuit to server-owned Council state without copying the live transcript into DevExpress while the run is active.</summary>
    /// <param name="runId">Identifier of the Council run to attach.</param>
    /// <param name="reloadChatControl">Whether the DevExpress chat message collection must be rebound for this circuit.</param>
    /// <returns><see langword="true"/> when the attachment was established; otherwise <see langword="false"/>.</returns>
    private async Task<bool> AttachToLiveCouncilSessionAsync(Guid runId, bool reloadChatControl = false)
    {
        if (isDisposed || DxAiChat is null || ChatClientProvider is null)
            return false;

        var gateEntered = false;
        try
        {
            await liveCouncilAttachGate.WaitAsync(componentLifetimeCts.Token).ConfigureAwait(false);
            gateEntered = true;
            if (isDisposed || DxAiChat is null || ChatClientProvider is null)
                return false;

            var snapshot = CouncilLiveSessions.GetAttachmentSnapshot(runId);
            if (snapshot is null)
                return false;

            var firstAttachmentToRun = AttachedLiveCouncilRunId != runId;
            if (!firstAttachmentToRun
                && snapshot.UpdatedAtUtc == lastAttachedLiveCouncilUpdatedAtUtc
                && attachedLiveCouncilSnapshot?.RunId == runId)
            {
                return true;
            }

            var shouldReloadChatControl = reloadChatControl || firstAttachmentToRun;
            var composerDraft = string.Empty;
            if (shouldReloadChatControl)
            {
                try
                {
                    string? capturedComposerDraft = null;
                    await InvokeAsync(async () =>
                    {
                        capturedComposerDraft = await JS.InvokeAsync<string>("localGptChatUi.readComposerDraft").ConfigureAwait(true);
                    }).ConfigureAwait(false);
                    composerDraft = capturedComposerDraft ?? string.Empty;
                }
                catch (JSDisconnectedException)
                {
                    return false;
                }
                catch (TaskCanceledException) when (componentLifetimeCts.IsCancellationRequested || isDisposed)
                {
                    return false;
                }
                catch (JSException exception)
                {
                    Logger.LogDebug(exception, "The live Council composer draft was not available before the circuit attachment.");
                }
            }

            var councilSession = ChatClientProvider.AvailableChatClients
                .FirstOrDefault(session => string.Equals(session.Name, Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase));
            if (councilSession is null)
                return false;

            // A running Council is represented in DevExpress by a tiny marker only. The Razor template
            // reads transcript and participant lanes directly from the server-owned live-session service.
            // When the run finishes, materialize the transcript once so persisted chat history remains complete.
            var marker = $"{LiveCouncilMessageMarkerPrefix}{runId:N} -->";
            var completedTranscript = snapshot.IsRunning ? string.Empty : CouncilLiveSessions.GetTranscript(runId);
            var assistantContent = snapshot.IsRunning || string.IsNullOrWhiteSpace(completedTranscript)
                ? marker
                : $"{marker}\n{completedTranscript}";

            await InvokeAsync(() =>
            {
                AttachedLiveCouncilRunId = runId;
                RejoinCouncilRunId = runId;
                ownsLiveCouncilStream = false;
                lastAttachedLiveCouncilUpdatedAtUtc = snapshot.UpdatedAtUtc;
                attachedLiveCouncilSnapshot = snapshot;
                SelectedCouncilRunId = runId;
                LoadCouncilRunConfiguration(runId);
                ChatClientProvider.SelectedSession = councilSession;
                hasUserSelectedSession = true;

                var replacement = new BlazorChatMessage(ChatRole.Assistant, assistantContent, new List<AIChatUploadFileInfo>());
                var markerMessageIndices = councilSession.Messages
                    .Select((message, index) => new { message, index })
                    .Where(item => item.message.Role == ChatMessageRole.Assistant
                        && CouncilText.ContainsText(item.message.Content, marker, StringComparison.Ordinal))
                    .Select(item => item.index)
                    .ToList();
                var messageIndex = markerMessageIndices.FirstOrDefault(-1);
                for (var duplicateIndex = markerMessageIndices.Count - 1; duplicateIndex >= 1; duplicateIndex--)
                    councilSession.Messages.RemoveAt(markerMessageIndices[duplicateIndex]);

                var initiatingMessageAlreadyPresent = messageIndex > 0
                    && councilSession.Messages[messageIndex - 1].Role == ChatMessageRole.User
                    && string.Equals(councilSession.Messages[messageIndex - 1].Content, snapshot.UserMessage, StringComparison.Ordinal);
                if (!string.IsNullOrWhiteSpace(snapshot.UserMessage) && !initiatingMessageAlreadyPresent)
                {
                    var userMessage = new BlazorChatMessage(ChatRole.User, snapshot.UserMessage, new List<AIChatUploadFileInfo>());
                    if (messageIndex >= 0)
                    {
                        councilSession.Messages.Insert(messageIndex, userMessage);
                        messageIndex++;
                    }
                    else
                    {
                        councilSession.Messages.Add(userMessage);
                    }
                }

                if (messageIndex >= 0)
                    councilSession.Messages[messageIndex] = replacement;
                else
                {
                    councilSession.Messages.Add(replacement);
                    messageIndex = councilSession.Messages.Count - 1;
                }

                var insertionIndex = messageIndex + 1;
                foreach (var additionalUserMessage in snapshot.AdditionalUserMessages)
                {
                    var existingIndex = councilSession.Messages.FindIndex(
                        insertionIndex,
                        message => message.Role == ChatMessageRole.User
                            && string.Equals(message.Content, additionalUserMessage, StringComparison.Ordinal));
                    if (existingIndex >= 0)
                    {
                        insertionIndex = existingIndex + 1;
                        continue;
                    }
                    councilSession.Messages.Insert(insertionIndex++, new BlazorChatMessage(ChatRole.User, additionalUserMessage, new List<AIChatUploadFileInfo>()));
                }

                if (shouldReloadChatControl && ReferenceEquals(ChatClientProvider.SelectedSession, councilSession) && DxAiChat is not null)
                    DxAiChat.LoadMessages(councilSession.Messages);
                StateHasChanged();
            }).ConfigureAwait(false);

            if (shouldReloadChatControl && !string.IsNullOrWhiteSpace(composerDraft))
            {
                try
                {
                    await InvokeAsync(() => JS.InvokeVoidAsync("localGptChatUi.restoreComposerDraft", composerDraft).AsTask()).ConfigureAwait(false);
                }
                catch (JSDisconnectedException)
                {
                    return false;
                }
                catch (TaskCanceledException) when (componentLifetimeCts.IsCancellationRequested || isDisposed)
                {
                    return false;
                }
                catch (JSException exception)
                {
                    Logger.LogDebug(exception, "The live Council composer draft could not be restored after circuit attachment.");
                }
            }

            if (!snapshot.IsRunning && !string.IsNullOrWhiteSpace(completedTranscript))
                await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (componentLifetimeCts.IsCancellationRequested || isDisposed)
        {
            return false;
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (InvalidOperationException exception) when (isDisposed || componentLifetimeCts.IsCancellationRequested)
        {
            Logger.LogDebug(exception, "The browser circuit ended while attaching to Council {RunId}.", runId);
            return false;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Attaching Chat to live Council {RunId} failed; transcript content was omitted.", runId);
            return false;
        }
        finally
        {
            if (gateEntered)
                liveCouncilAttachGate.Release();
        }
    }

    [JSInvokable]
    public async Task<bool> StopActiveCouncilRunAsync()
    {
        var activeRunId = ResolveRunningCouncilRunId();
        if (activeRunId is not Guid runId)
            return false;

        var cancelled = CouncilLiveSessions.Cancel(runId);
        if (!cancelled)
            return false;

        ComponentActivity.RecordWarning(nameof(Chat), "StopCouncilRun", "The user explicitly stopped the active Council run.");
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        try
        {
            await JS.InvokeVoidAsync("localGptChatUi.refreshCouncilComposer", false).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The cancellation already reached the Council runtime; the browser circuit closed before UI acknowledgement.
        }
        return true;
    }

    private Guid? ResolveRunningCouncilRunId()
    {
        Guid?[] candidates = [AttachedLiveCouncilRunId, RejoinCouncilRunId];
        foreach (var candidate in candidates)
        {
            if (candidate is Guid runId && CouncilLiveSessions.GetSummary(runId)?.IsRunning == true)
                return runId;
        }

        return null;
    }

    private async Task StopSelectedCouncilSessionAsync()
    {
        if (await StopActiveCouncilRunAsync().ConfigureAwait(false))
        {
            Notifier.ShowSuccess(toastName, "The selected Council run received an explicit stop request.", "Council stopped");
            await RefreshHumanCollaborationAsync().ConfigureAwait(false);
        }
        else
        {
            Notifier.ShowError(toastName, "No running Council session was available to stop.", "Stop Council");
        }
    }

    private Task SkipCurrentCouncilRoundAsync()
    {
        var runId = SelectedCouncilRunId
            ?? ActiveCouncilRun?.RunId
            ?? AttachedLiveCouncilRunId;
        if (runId is not Guid activeRunId)
        {
            Notifier.ShowError(toastName, "No running Council round is available to skip.", "Skip round");
            return Task.CompletedTask;
        }

        if (CouncilRunConfigurations.RequestSkipCurrentRound(activeRunId))
        {
            ComponentActivity.RecordWarning(nameof(Chat), "SkipCouncilRound", "The user skipped the current Council round; other running sessions were not changed.");
            Notifier.ShowSuccess(toastName, "The current Council round is stopping. The run will continue with its next phase.", "Round skipped");
        }
        else
        {
            Notifier.ShowError(toastName, "The selected Council has already left that round or is no longer running.", "Skip round");
        }

        return Task.CompletedTask;
    }

    private string LiveCouncilRunningTitle(CouncilLiveSessionSummary session)
    {
        return CouncilText.FormatLiveCouncilRunningTitle(
            L("Chat.LiveCouncil.Running", "Council {id} is still running. Waiting for the next streamed update…"),
            ShortCouncilRunId(session.RunId),
            Logger);
    }

    private string LiveCouncilRunningDetail(CouncilLiveSessionSummary session)
    {
        var elapsed = DateTime.UtcNow - session.StartedAtUtc;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;
        var elapsedText = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes:00}m {elapsed.Seconds:00}s"
            : $"{elapsed.Minutes:00}m {elapsed.Seconds:00}s";
        var status = string.IsNullOrWhiteSpace(session.StatusMessage)
            ? L("Chat.LiveCouncil.DefaultStatus", "Waiting for local model or tool output; heartbeat updates stay outside member text.")
            : session.StatusMessage;
        return CouncilText.FormatLiveCouncilElapsedStatus(
            L("Chat.LiveCouncil.ElapsedStatus", "Running for {elapsed} · {status}"),
            elapsedText,
            status,
            Logger);
    }

    private string ShortCouncilRunId(Guid runId) => runId.ToString("N")[..8];

    private CouncilLiveSessionSummary? ResolveLiveCouncilMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        return CouncilText.TryExtractMarkedGuid(content, LiveCouncilMessageMarkerPrefix, out var runId)
            ? CouncilLiveSessions.GetSummary(runId)
            : null;
    }

    private void OnHumanCollaborationChanged()
    {
        if (isDisposed)
            return;

        try
        {
            TaskRunner.Run(
                nameof(Chat),
                "RefreshHumanCollaboration",
                _ => RefreshHumanCollaborationAsync(),
                componentLifetimeCts.Token);
        }
        catch (ObjectDisposedException) when (isDisposed)
        {
            // A collaboration event raced with circuit disposal.
        }
    }

    public void Dispose()
    {
        try
        {
            if (isDisposed)
                return;

            SavePreparationConfiguration();
            isDisposed = true;
            HumanCollaboration.Changed -= OnHumanCollaborationChanged;
            CouncilLiveSessions.Changed -= OnCouncilLiveSessionChanged;
            CouncilRunConfigurations.Changed -= OnCouncilRunConfigurationChanged;
            componentLifetimeCts.Cancel();
            if (interactiveAttached && DxAiChat is not null)
            {
                try
                {
                    SaveMessagesForSelectedSession(DxAiChat.SaveMessages());
                    Logger.LogDebug($"{nameof(Dispose)} captured the final DXAiChat messages before disposal.");
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogDebug(ex, $"{nameof(Dispose)} skipped final message capture because disposal was cancelled.");
                }
                catch (InvalidOperationException ex) when (isDisposed)
                {
                    Logger.LogDebug(ex, $"{nameof(Dispose)} skipped final message capture because the renderer was already shutting down.");
                }
            }
            chatInteropReference?.Dispose();
            chatInteropReference = null;
            componentLifetimeCts.Dispose();
            Logger.LogDebug($"{nameof(Dispose)} completed Chat component disposal.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{nameof(Dispose)} failed; conversation content was omitted from logs.");
            ComponentActivity.RecordFailure(nameof(Chat), "Dispose", ex);
        }
    }

    
    }
}
