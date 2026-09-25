using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the chat Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class Chat
{
    /// <summary>Handles one persisted microphone recording, keeps its WAV attached to the visible chat, and starts the selected Council only when a transcript is available.</summary>
    /// <param name="recording">Persisted microphone recording with optional Whisper transcript.</param>
    /// <returns>A task that completes after attachment presentation and optional Council submission.</returns>
    private async Task HandleSpeechRecordingAsync(LocalAiMicrophoneRecording recording)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(recording);
            await AppendMicrophoneAttachmentAsync(recording).ConfigureAwait(false);

            if (recording.TranscriptionSucceeded && !string.IsNullOrWhiteSpace(recording.Transcript))
                await StartSpeechCouncilAsync(recording.Transcript).ConfigureAwait(false);
            else
            {
                await InvokeAsync(() =>
                {
                    modelStatus = string.IsNullOrWhiteSpace(recording.TranscriptionStatus)
                        ? L("Chat.Microphone.Attached", "Microphone audio is attached. Install/select Whisper when you want automatic transcription.")
                        : recording.TranscriptionStatus;
                    Notifier.ShowInfo(toastName, modelStatus, L("Chat.Microphone.Start", "Microphone"));
                    StateHasChanged();
                }).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Handling a persisted microphone recording failed with {ExceptionType}; audio and transcript content were omitted.", ex.GetType().Name);
            await InvokeAsync(() => Notifier.ShowError(
                toastName,
                L("Chat.Microphone.Error", "The microphone recording was saved but Chat could not finish presenting it. Review LocalGPT logs."),
                L("Chat.Microphone.Start", "Microphone"))).ConfigureAwait(false);
        }
    }

    /// <summary>Uses the normal Council entry point so the selected team, models and run settings remain authoritative.</summary>
    /// <param name="transcript">Transcript value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task whose result indicates whether the browser accepted the Council prompt.</returns>
    private async Task<bool> StartSpeechCouncilAsync(string transcript)
    {
        try
        {
            var title = L("Chat.Microphone.Start", "Microphone");
            var description = L("Chat.Microphone.Submitted", "Transcribed. Starting Council with your current settings.");
            var suggestionKey = $"microphone-{Guid.NewGuid():N}";
            string[] teamKeys = string.IsNullOrWhiteSpace(SelectedCouncilTeamKey) ? [] : [SelectedCouncilTeamKey];
            var starter = new PromptSuggestion(
                title,
                description,
                transcript,
                suggestionKey,
                teamKeys,
                startsCouncilDirectly: true);
            var submitted = false;
            await InvokeAsync(async () =>
            {
                submitted = await StartCouncilPromptAsync(starter, startFresh: false).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return submitted;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Starting Council from a Whisper transcript failed with {ExceptionType}; transcript omitted.", ex.GetType().Name);
            return false;
        }
    }

    /// <summary>Adds one persisted microphone WAV as a durable visible chat attachment with a local download link.</summary>
    /// <param name="recording">Persisted recording whose artifact is presented.</param>
    /// <returns>A task that completes after the DevExpress chat surface is refreshed.</returns>
    private async Task AppendMicrophoneAttachmentAsync(LocalAiMicrophoneRecording recording)
    {
        if (ChatClientProvider?.SelectedSession is null)
            throw new InvalidOperationException("A chat session is required before a microphone recording can be attached.");

        var fileName = string.IsNullOrWhiteSpace(recording.Artifact.FileName) ? "microphone.wav" : recording.Artifact.FileName;
        var label = L("Chat.Microphone.Attachment", "Microphone recording");
        var content = string.IsNullOrWhiteSpace(recording.Artifact.DownloadUrl)
            ? label
            : $"[{label}]({recording.Artifact.DownloadUrl})";
        var displayContent = CouncilText.BuildAttachmentPresentation(content, [fileName]);
        var chatMessage = new BlazorChatMessage(ChatRole.User, displayContent, new List<AIChatUploadFileInfo>());
        ChatClientProvider.SelectedSession.Messages.Add(chatMessage);
        canonicalConversationMessages.Clear();
        canonicalConversationMessages.AddRange(ChatClientProvider.SelectedSession.Messages);
        await PersistMessagesAsync(ChatClientProvider.SelectedSession.Messages.ToList(), force: true, showToast: false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            LoadSelectedSessionMessages();
            StateHasChanged();
        }).ConfigureAwait(false);
    }

    /// <summary>Starts the explicit Whisper setup team through the existing Council starter route.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartWhisperSetupAsync()
    {
        try
        {
            var setupPrompt = await PromptConfigService.GetPromptAsync("WhisperSetupCouncilPrompt").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(setupPrompt))
                throw new InvalidOperationException("The persisted Whisper setup prompt is unavailable.");

            var title = L("Chat.Microphone.Setup", "Ask Council to set up Whisper");
            var description = L("Chat.Microphone.Missing", "Install a Whisper model with Council or on the Install page first.");
            var suggestionKey = "whisper-setup";
            string[] teamKeys = ["whisper-assistant"];
            var starter = new PromptSuggestion(
                title,
                description,
                setupPrompt,
                suggestionKey,
                teamKeys,
                startsCouncilDirectly: true);
            await InvokeAsync(() => StartCouncilPromptAsync(starter, startFresh: false)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Starting the Whisper setup Council failed with {ExceptionType}.", ex.GetType().Name);
        }
    }
}
