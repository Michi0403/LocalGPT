using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the chat Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class Chat
{
    /// <summary>Uses the normal Council entry point so the selected team, models and run settings remain authoritative.</summary>
    /// <param name="transcript">Transcript value supplied to the chat operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task StartSpeechCouncilAsync(string transcript)
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
            return StartCouncilPromptAsync(starter, startFresh: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Starting Council from a Whisper transcript failed with {ExceptionType}; transcript omitted.", ex.GetType().Name);
            return Task.CompletedTask;
        }
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
