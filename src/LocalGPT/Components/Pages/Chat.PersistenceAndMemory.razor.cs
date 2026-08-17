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
    private async Task RefreshModelPresetsAfterBenchmarkAsync(CouncilModelPreset appliedPreset)
    {
        ModelPresets = (await ModelPresetService.GetPresetsAsync().ConfigureAwait(false)).ToList();
        SelectedModelPreset = ModelPresets.FirstOrDefault(item => item.Id == appliedPreset.Id) ?? appliedPreset;
        ModelPresetName = SelectedModelPreset.Name;
    }

    private void SaveMessagesForSelectedSession(IEnumerable<BlazorChatMessage> saveMessages)
    {
        try
        {
            if (ChatClientProvider?.SelectedSession != null)
            {
                var mergedMessages = saveMessages.ToList();
                foreach (var pending in PendingLiveCouncilUserMessages)
                {
                    if (!mergedMessages.Any(message =>
                        message.Role == pending.Role &&
                        string.Equals(message.Content, pending.Content, StringComparison.Ordinal)))
                    {
                        mergedMessages.Add(pending);
                    }
                }

                ChatClientProvider.SelectedSession.Messages.Clear();
                ChatClientProvider.SelectedSession.Messages.AddRange(mergedMessages);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    private void LoadSelectedSessionMessages()
    {
        try
        {
            if (ChatClientProvider?.SelectedSession is not null && DxAiChat is not null)
                DxAiChat.LoadMessages(ChatClientProvider.SelectedSession.Messages);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task SaveCurrentConversationAsync()
    {
        try
        {
            await PersistCurrentConversationAsync(force: true, showToast: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    private async Task LoadLatestConversationAsync()
    {
        try
        {
            await LoadLatestConversationIntoSessionAsync(loadIntoDxChat: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }


    }

    private async Task LoadSelectedConversationAsync()
    {
        try
        {
            if (SelectedConversation is not null)
                await LoadSavedConversationAsync(SelectedConversation).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading the selected saved chat failed.");
            Notifier.ShowError(toastName, "The selected saved chat could not be loaded. See local application logs for technical details.", "Recall chat failed");
        }
    }

    private async Task OnSavedConversationSelectionChangedAsync(ChangeEventArgs args)
    {
        try
        {
            if (!Guid.TryParse(args.Value?.ToString(), out var conversationId))
                return;

            SelectedConversation = SavedConversations.FirstOrDefault(item => item.Id == conversationId);
            if (SelectedConversation is not null)
                await LoadSavedConversationAsync(SelectedConversation).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Selecting a saved chat from the complete memory list failed.");
            Notifier.ShowError(toastName, "The saved chat could not be selected. See local application logs for technical details.", "Recall chat failed");
        }
    }

    private async Task LoadLatestConversationIntoSessionAsync(bool loadIntoDxChat)
    {
        try
        {
            if (SavedConversations.Count == 0)
                return;

            await LoadSavedConversationAsync(SavedConversations[0], saveCurrentFirst: false, loadIntoDxChat).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }


    }

    private async Task LoadSavedConversationAsync(ChatMemoryConversationSummary? conversation)
    {
        try
        {
            await LoadSavedConversationAsync(conversation, saveCurrentFirst: true, loadIntoDxChat: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task LoadSavedConversationAsync(ChatMemoryConversationSummary? conversation, bool saveCurrentFirst, bool loadIntoDxChat)
    {
        try
        {
            if (conversation is null || ChatClientProvider is null)
                return;

            try
            {
                if (saveCurrentFirst)
                    await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);

                isMemoryBusy = true;
                var snapshot = await ChatMemory.LoadConversationAsync(conversation.Id).ConfigureAwait(false);
                if (snapshot is null)
                    return;

                var restoredSession = ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
                    string.Equals(session.Name, snapshot.ProviderName, StringComparison.OrdinalIgnoreCase));
                restoredSession ??= ChatClientProvider.SelectedSession;
                restoredSession ??= ChatClientProvider.AvailableChatClients.FirstOrDefault();
                if (restoredSession is null)
                {
                    memoryStatus = "The saved chat is ready, but no AI provider session is currently available. Configure a provider and recall it again.";
                    return;
                }

                ChatClientProvider.SelectedSession = restoredSession;
                ActiveConversationId = snapshot.Id;
                SessionContext.Restore(new ChatSessionContextSnapshot(
                    snapshot.Id,
                    snapshot.ProjectId,
                    snapshot.ProjectVersionId,
                    SessionContext.ApplicationVersion));
                await LoadSelectedChatProjectDetailsAsync(snapshot.ProjectId).ConfigureAwait(false);
                SelectedConversation = conversation;
                restoredSession.Messages.Clear();
                restoredSession.Messages.AddRange(snapshot.Messages);
                lastSavedSignature = CouncilText.CreateMessageSignature(snapshot.Messages, Logger);
                memoryStatus = string.Equals(restoredSession.Name, snapshot.ProviderName, StringComparison.OrdinalIgnoreCase)
                    ? $"Loaded chat: {snapshot.Title} · restored {restoredSession.Name}"
                    : $"Loaded legacy chat: {snapshot.Title} · continued with {restoredSession.Name}";
                SavedFeedback = (await ChatMemory.GetMessageFeedbackAsync(snapshot.Id).ConfigureAwait(false)).ToList();

                if (loadIntoDxChat)
                {
                    await InvokeAsync(() =>
                    {
                        if (DxAiChat is not null)
                            DxAiChat.LoadMessages(snapshot.Messages);
                    }).ConfigureAwait(false);
                }

                RefreshFeedbackTargets();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadSavedConversationAsync failed: {Message}", ex.Message);
                Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            }
            finally
            {
                isMemoryBusy = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    private async Task PersistCurrentConversationAsync(bool force, bool showToast)
    {
        try
        {
            var messages = await CaptureCurrentMessagesAsync().ConfigureAwait(false);
            if (messages.Count == 0)
                return;

            // Database, project-context and memory refresh work deliberately runs outside the
            // renderer synchronization context. Only the synchronous DevExpress message capture
            // above is renderer-affine. This keeps the supervised 12-second auto-save from
            // periodically monopolizing the interactive circuit.
            await PersistMessagesAsync(messages, force, showToast).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private async Task<IReadOnlyList<BlazorChatMessage>> CaptureCurrentMessagesAsync()
    {
        if (isDisposed)
            return [];

        List<BlazorChatMessage> captured = [];
        await InvokeAsync(() =>
        {
            if (DxAiChat is null)
                return;

            captured = DxAiChat.SaveMessages().ToList();
            MergeAuthoritativeLiveCouncilMessage(captured);
            SaveMessagesForSelectedSession(captured);
        }).ConfigureAwait(false);

        return captured;
    }

    private void MergeAuthoritativeLiveCouncilMessage(List<BlazorChatMessage> captured)
    {
        if (AttachedLiveCouncilRunId is not Guid runId || ChatClientProvider?.SelectedSession is null)
            return;

        var marker = $"{LiveCouncilMessageMarkerPrefix}{runId:N} -->";
        var authoritative = ChatClientProvider.SelectedSession.Messages.FirstOrDefault(message =>
            message.Role == ChatMessageRole.Assistant
            && CouncilText.ContainsText(message.Content, marker, StringComparison.Ordinal));
        if (authoritative is null)
            return;

        var capturedIndex = captured.FindIndex(message =>
            message.Role == ChatMessageRole.Assistant
            && CouncilText.ContainsText(message.Content, marker, StringComparison.Ordinal));
        if (capturedIndex >= 0)
            captured[capturedIndex] = authoritative;
        else
            captured.Add(authoritative);

        foreach (var pending in PendingLiveCouncilUserMessages)
        {
            if (!captured.Any(message =>
                message.Role == pending.Role
                && string.Equals(message.Content, pending.Content, StringComparison.Ordinal)))
            {
                captured.Add(pending);
            }
        }
    }

    private async Task PersistMessagesAsync(IReadOnlyList<BlazorChatMessage> messages, bool force, bool showToast)
    {
        try
        {
            if (ChatClientProvider?.SelectedSession is null || messages.Count == 0)
                return;

            var signature = CouncilText.CreateMessageSignature(messages, Logger);
            if (!force && signature == lastSavedSignature)
                return;

            try
            {
                isMemoryBusy = true;
                var savedConversationId = await ChatMemory.SaveConversationAsync(
                    ChatClientProvider.SelectedSession.Name,
                    messages,
                    ActiveConversationId).ConfigureAwait(false);
                if (savedConversationId is not Guid persistedId)
                {
                    memoryStatus = "The conversation could not be saved. See local application logs.";
                    return;
                }

                ActiveConversationId = persistedId;
                SessionContext.SetConversation(persistedId);
                lastSavedSignature = signature;
                RefreshFeedbackTargets(messages);
                await RefreshMemoryAsync().ConfigureAwait(false);
                memoryStatus = $"Memory saved at {DateTime.Now:t}.";

                if (showToast)
                    Notifier.ShowSuccess(toastName, "Chat saved to SQLite memory.", "Memory saved");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PersistMessagesAsync failed: {Message}", ex.Message);
                if (showToast)
                    Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            }
            finally
            {
                isMemoryBusy = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not persist the current conversation; message content was omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    private async Task RefreshMemoryAsync()
    {
        try
        {
            SavedConversations = (await ChatMemory.GetConversationsAsync().ConfigureAwait(false)).ToList();
            RecentThoughts = (await ChatMemory.GetRecentThoughtsAsync().ConfigureAwait(false)).ToList();

            if (ActiveConversationId is Guid id)
            {
                SelectedConversation = SavedConversations.FirstOrDefault(conversation => conversation.Id == id);
                SavedFeedback = (await ChatMemory.GetMessageFeedbackAsync(id).ConfigureAwait(false)).ToList();
            }
            else
            {
                SelectedConversation = SavedConversations.FirstOrDefault();
                SavedFeedback.Clear();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }

    }

    private string SelectedChatProjectValue => SessionContext.ProjectId?.ToString() ?? string.Empty;

    private string SelectedChatProjectVersionValue => SessionContext.ProjectVersionId?.ToString() ?? string.Empty;

    private string SelectedFeedbackSortOrderValue => SelectedFeedbackSortOrder?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private async Task LoadChatProjectsAsync()
    {
        ChatProjects = (await ProjectService.GetProjectsAsync().ConfigureAwait(false)).ToList();
        await LoadSelectedChatProjectDetailsAsync(SessionContext.ProjectId).ConfigureAwait(false);
    }

    private async Task LoadSelectedChatProjectDetailsAsync(Guid? projectId)
    {
        SelectedChatProjectDetails = projectId is Guid id
            ? await ProjectService.GetProjectAsync(id).ConfigureAwait(false)
            : null;
    }

    private async Task OnChatProjectChangedAsync(ChangeEventArgs args)
    {
        if (!Guid.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var projectId))
        {
            SessionContext.SetProject(null, null);
            SelectedChatProjectDetails = null;
            memoryStatus = "Chat is not linked to a project.";
        }
        else
        {
            await LoadSelectedChatProjectDetailsAsync(projectId).ConfigureAwait(false);
            var currentVersion = SelectedChatProjectDetails?.Versions.FirstOrDefault(version => version.IsCurrent)
                ?? SelectedChatProjectDetails?.Versions.OrderByDescending(version => version.CreatedAtUtc).FirstOrDefault();
            SessionContext.SetProject(projectId, currentVersion?.Id);
            memoryStatus = currentVersion is null
                ? "Chat linked to project; no saved project version selected."
                : $"Chat linked to {SelectedChatProjectDetails!.Project.Name} {currentVersion.Version}.";
        }

        if (ActiveConversationId is not null)
            await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);
    }

    private async Task OnChatProjectVersionChangedAsync(ChangeEventArgs args)
    {
        if (SessionContext.ProjectId is not Guid projectId || SelectedChatProjectDetails is null)
            return;

        Guid? versionId = Guid.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var parsed)
            ? parsed
            : null;
        if (versionId is Guid id && SelectedChatProjectDetails.Versions.All(version => version.Id != id))
            return;

        SessionContext.SetProject(projectId, versionId);
        var selectedVersion = versionId is Guid selectedId
            ? SelectedChatProjectDetails.Versions.First(version => version.Id == selectedId)
            : null;
        memoryStatus = selectedVersion is null
            ? $"Chat linked to {SelectedChatProjectDetails.Project.Name}; exact version unspecified."
            : $"Chat linked to {SelectedChatProjectDetails.Project.Name} {selectedVersion.Version}.";

        if (ActiveConversationId is not null)
            await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);
    }

    private void RefreshFeedbackTargets() =>
        RefreshFeedbackTargets(DxAiChat?.SaveMessages().ToList() ?? []);

    private void RefreshFeedbackTargets(IReadOnlyList<BlazorChatMessage> messages)
    {
        FeedbackTargets = messages
            .Select((message, sortOrder) => new { message, sortOrder })
            .Where(item => item.message.Role == ChatMessageRole.Assistant && !string.IsNullOrWhiteSpace(item.message.Content))
            .Select(item => new ChatFeedbackTarget(
                item.sortOrder,
                $"#{item.sortOrder + 1}: {CouncilText.BuildFeedbackPreview(item.message.Content, Logger)}"))
            .ToList();

        if (FeedbackTargets.Count == 0)
        {
            SelectedFeedbackSortOrder = null;
            FeedbackComment = string.Empty;
            return;
        }

        if (SelectedFeedbackSortOrder is not int selected || FeedbackTargets.All(target => target.SortOrder != selected))
            SelectedFeedbackSortOrder = FeedbackTargets[^1].SortOrder;

        LoadFeedbackEditor();
    }

    private void OnFeedbackTargetChanged(ChangeEventArgs args)
    {
        try
        {
            SelectedFeedbackSortOrder = int.TryParse(
                Convert.ToString(args.Value, CultureInfo.InvariantCulture),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var sortOrder)
                ? sortOrder
                : null;
            LoadFeedbackEditor();
            Logger.LogDebug($"{nameof(OnFeedbackTargetChanged)} selected feedback target {SelectedFeedbackSortOrderValue}.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(OnFeedbackTargetChanged)} failed while changing the feedback target.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(OnFeedbackTargetChanged), exception);
            Notifier.ShowError(toastName, "The feedback target could not be changed. See local logs for details.", "Feedback selection failed");
        }
    }

    private void LoadFeedbackEditor()
    {
        try
        {
            var existing = SelectedFeedbackSortOrder is int sortOrder
                ? SavedFeedback.FirstOrDefault(item => item.SortOrder == sortOrder)
                : null;
            FeedbackComment = existing?.Comment ?? string.Empty;
            Logger.LogDebug($"{nameof(LoadFeedbackEditor)} loaded feedback metadata for target {SelectedFeedbackSortOrderValue}; comment content was omitted from logs.");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(LoadFeedbackEditor)} failed while loading feedback metadata; comment content was omitted from logs.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(LoadFeedbackEditor), exception);
            Notifier.ShowError(toastName, "The saved feedback could not be loaded. See local logs for details.", "Feedback load failed");
        }
    }

    private async Task ClearSelectedFeedbackAsync()
    {
        try
        {
            Logger.LogDebug($"{nameof(ClearSelectedFeedbackAsync)} is clearing the selected feedback rating.");
            await RecordSelectedFeedbackAsync(null).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(ClearSelectedFeedbackAsync)} failed while clearing the selected feedback rating.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(ClearSelectedFeedbackAsync), exception);
            Notifier.ShowError(toastName, "The feedback rating could not be cleared. See local logs for details.", "Feedback clear failed");
        }
    }

    private async Task RecordSelectedFeedbackAsync(bool? isPositive)
    {
        if (SelectedFeedbackSortOrder is not int sortOrder)
            return;

        await PersistCurrentConversationAsync(force: true, showToast: false).ConfigureAwait(false);
        if (ActiveConversationId is not Guid conversationId)
        {
            feedbackStatus = "Save the conversation before recording feedback.";
            return;
        }

        var saved = await ChatMemory.RecordMessageFeedbackAsync(
            conversationId,
            sortOrder,
            isPositive,
            FeedbackComment,
            CancellationToken.None).ConfigureAwait(false);
        if (!saved)
        {
            feedbackStatus = "The selected assistant response is no longer available.";
            return;
        }

        SavedFeedback = (await ChatMemory.GetMessageFeedbackAsync(conversationId).ConfigureAwait(false)).ToList();
        feedbackStatus = isPositive switch
        {
            true => "Helpful rating saved locally.",
            false => "Not-helpful rating saved locally.",
            _ => "Rating cleared; comment retained if supplied."
        };
    }


    private void StartInitialModelRefresh()
    {
        try
        {
            if (initialModelRefreshStarted || isDisposed || !interactiveAttached || !chatControlInitialized)
                return;

            initialModelRefreshStarted = true;
            TaskRunner.Run(
                nameof(Chat),
                "InitialModelRefresh",
                async cancellationToken =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested || isDisposed)
                            return;
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                        if (cancellationToken.IsCancellationRequested || isDisposed)
                            return;
                        await DiscoverAndApplyOllamaModelsAsync(showToast: false, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || isDisposed)
                    {
                        Logger.LogDebug("Initial Chat model refresh stopped during navigation or shutdown.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Initial Chat model refresh failed; configured sessions remain available.");
                        ComponentActivity.RecordFailure(nameof(Chat), "InitialModelRefresh", ex);
                        await InvokeAsync(() => Notifier.ShowWarning(toastName, "Local model refresh failed. Configured sessions remain available; see local logs for details.", "Model refresh warning")).ConfigureAwait(false);
                    }
                },
                componentLifetimeCts.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not schedule the initial Chat model refresh.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(StartInitialModelRefresh), ex);
            Notifier.ShowError(toastName, "The initial model refresh could not be scheduled. See local logs for details.", "Model refresh error");
        }
    }

    private void StartAutoSaveLoop()
    {
        if (autoSaveStarted || !interactiveAttached || !chatControlInitialized || isDisposed)
            return;

        autoSaveStarted = true;
        TaskRunner.Run(
            nameof(Chat),
            "AutoSaveLoop",
            AutoSaveLoopAsync,
            componentLifetimeCts.Token);
    }

    private async Task<bool> WaitForAutoSaveIntervalAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug($"{nameof(WaitForAutoSaveIntervalAsync)} observed cancellation before waiting.");
                return false;
            }

            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = cancellationToken.Register(state =>
                ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancelled);
            var delay = Task.Delay(TimeSpan.FromSeconds(12));
            var completed = await Task.WhenAny(delay, cancelled.Task).ConfigureAwait(false);
            var shouldContinue = completed == delay && !cancellationToken.IsCancellationRequested;
            Logger.LogDebug($"{nameof(WaitForAutoSaveIntervalAsync)} completed with continuation state {shouldContinue}.");
            return shouldContinue;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, $"{nameof(WaitForAutoSaveIntervalAsync)} failed while supervising the auto-save interval.");
            ComponentActivity.RecordFailure(nameof(Chat), nameof(WaitForAutoSaveIntervalAsync), exception);
            Notifier.ShowError(toastName, "Auto-save supervision failed. See local logs for details.", "Auto-save error");
            return false;
        }
    }

    private async Task AutoSaveLoopAsync(CancellationToken cancellationToken)
    {
        while (await WaitForAutoSaveIntervalAsync(cancellationToken).ConfigureAwait(false))
        {
            if (isDisposed)
                break;

            try
            {
                // Do not call the synchronous DevExpress SaveMessages API for a brand-new empty
                // control. That work has no persistence value and can contend with the control's
                // first client-side render. Once a session contains data or has a persisted ID,
                // the normal supervised auto-save path remains active.
                var hasPersistableState = ActiveConversationId is not null
                    || (ChatClientProvider?.SelectedSession?.Messages.Count ?? 0) > 0;
                if (!hasPersistableState)
                    continue;

                await PersistCurrentConversationAsync(force: false, showToast: false).ConfigureAwait(false);
                autoSaveFailureNotified = false;
            }
            catch (ObjectDisposedException) when (isDisposed)
            {
                break;
            }
            catch (InvalidOperationException) when (isDisposed || cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Chat memory auto-save failed; conversation content was omitted from logs.");
                ComponentActivity.RecordFailure(nameof(Chat), "AutoSaveConversation", ex);
                if (!autoSaveFailureNotified)
                {
                    autoSaveFailureNotified = true;
                    Notifier.ShowWarning(toastName, "Chat auto-save failed. Your current screen remains available; review local logs before closing the conversation.", "Auto-save warning");
                }
            }
        }
    }

    }
}
