using LocalGPT.BusinessObjects;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Pages
{
    /// <summary>
    /// Contains application-level commands exposed through the persistent ASCII Operator surface.
    /// </summary>
    public partial class Chat
    {
        /// <summary>Submits a normal chat prompt from the ASCII Operator without requiring the graphical composer.</summary>
        /// <param name="prompt">Human-entered chat prompt to submit through the existing Chat composer bridge.</param>
        /// <returns>A task that completes when the prompt has been submitted or restored to the composer.</returns>
        private async Task SubmitOperatorChatPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            try
            {
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                var submitted = await JS.InvokeAsync<bool>(
                    "localGptChatUi.submitSuggestionOrPrompt",
                    "ASCII Operator prompt",
                    prompt.Trim()).ConfigureAwait(false);
                await InvokeAsync(() =>
                {
                    modelStatus = submitted
                        ? "Submitted a chat prompt from the ASCII Operator."
                        : "The chat composer was not ready; the Operator prompt was restored to the composer.";
                }).ConfigureAwait(false);
                if (!submitted)
                {
                    await JS.InvokeVoidAsync(
                        "localGptChatUi.restoreComposerDraft",
                        prompt.Trim()).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Submitting a chat prompt from the ASCII Operator failed; prompt text was omitted from diagnostics.");
                modelStatus = "The ASCII Operator chat prompt could not be submitted. Review LocalGPT diagnostics.";
            }
        }

        /// <summary>Starts an AI Council prompt from the ASCII Operator using the currently selected Council team.</summary>
        /// <param name="prompt">Human-entered Council prompt to start with the current team selection.</param>
        /// <returns>A task that completes when the Council request has been handed to the existing Chat workflow.</returns>
        private async Task SubmitOperatorCouncilPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            try
            {
                var teamKeys = string.IsNullOrWhiteSpace(SelectedCouncilTeamKey)
                    ? Array.Empty<string>()
                    : [SelectedCouncilTeamKey];
                var starter = new PromptSuggestion(
                    "ASCII Operator Council prompt",
                    "Council prompt submitted from the persistent ASCII Operator surface.",
                    prompt.Trim(),
                    key: $"ascii-operator-{Guid.NewGuid():N}",
                    teamKeys: teamKeys,
                    startsCouncilDirectly: true);
                await InvokeAsync(() => StartCouncilPromptAsync(starter, startFresh: false)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Starting a Council prompt from the ASCII Operator failed; prompt text was omitted from diagnostics.");
                modelStatus = "The ASCII Operator Council prompt could not be started. Review LocalGPT diagnostics.";
            }
        }

        /// <summary>Opens a saved conversation selected by GUID, GUID prefix, or title from the ASCII Operator.</summary>
        /// <param name="selector">Service-parsed saved-conversation selector entered by the human operator.</param>
        /// <returns>A task that completes when the selected conversation is loaded or a status is reported.</returns>
        private async Task OpenOperatorSavedConversationAsync(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return;

            try
            {
                var normalized = selector.Trim();
                var conversation = ConsoleOperator.ResolveSavedConversation(SavedConversations, normalized);
                if (conversation is null)
                {
                    await InvokeAsync(() => memoryStatus = $"No unique saved chat matched '{normalized}'. Use :session list to see the current choices.").ConfigureAwait(false);
                    return;
                }

                await InvokeAsync(async () =>
                {
                    SelectedConversation = conversation;
                    await LoadSavedConversationAsync(conversation).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Opening a saved conversation from the ASCII Operator failed.");
                memoryStatus = "The ASCII Operator could not open the selected saved chat. Review LocalGPT diagnostics.";
            }
        }
    }
}
