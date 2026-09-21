using LocalGPT.BusinessObjects;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Pages
{
    /// <summary>Bridges the upload-processing advisor into the existing Chat and Council submission workflows.</summary>
    public partial class Chat
    {
        /// <summary>Sends an advisory upload-processing prompt through the normal DevExpress Chat composer bridge.</summary>
        /// <param name="recommendation">Reviewed advisory recommendation produced from quarantine evidence and approved Knowledge.</param>
        /// <returns>A task that completes when the prompt is submitted or restored into the composer.</returns>
        private async Task SubmitUploadRecommendationToChatAsync(UploadProcessingRecommendation recommendation)
        {
            if (string.IsNullOrWhiteSpace(recommendation.SuggestedPrompt))
                return;

            try
            {
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                var submitted = await JS.InvokeAsync<bool>(
                    "localGptChatUi.submitSuggestionOrPrompt",
                    "Upload advisor recommendation",
                    recommendation.SuggestedPrompt.Trim()).ConfigureAwait(false);
                if (!submitted)
                {
                    await JS.InvokeVoidAsync(
                        "localGptChatUi.restoreComposerDraft",
                        recommendation.SuggestedPrompt.Trim()).ConfigureAwait(false);
                    await InvokeAsync(() => modelStatus = "The chat composer was not ready; the upload recommendation was restored to the composer.").ConfigureAwait(false);
                    return;
                }

                await InvokeAsync(() => modelStatus = "Submitted the upload-processing recommendation to Chat.").ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Submitting an upload-processing recommendation to Chat failed; prompt text was omitted from diagnostics.");
                modelStatus = "The upload-processing recommendation could not be submitted to Chat. Review LocalGPT diagnostics.";
            }
        }

        /// <summary>Starts the advisory upload-processing prompt with the recommended existing Council team.</summary>
        /// <param name="recommendation">Reviewed advisory recommendation containing an existing Council team key when one is suitable.</param>
        /// <returns>A task that completes when the Council request is handed to the existing Chat workflow.</returns>
        private async Task SubmitUploadRecommendationToCouncilAsync(UploadProcessingRecommendation recommendation)
        {
            if (string.IsNullOrWhiteSpace(recommendation.SuggestedPrompt))
                return;

            try
            {
                var requestedTeam = CouncilTeams.FirstOrDefault(team =>
                    team.IsEnabled
                    && !team.IsDeleted
                    && string.Equals(team.Key, recommendation.SuggestedTeamKey, StringComparison.OrdinalIgnoreCase));
                if (requestedTeam is not null)
                    SelectedCouncilTeamKey = requestedTeam.Key;

                var teamKeys = string.IsNullOrWhiteSpace(SelectedCouncilTeamKey)
                    ? Array.Empty<string>()
                    : [SelectedCouncilTeamKey];
                var starter = new PromptSuggestion(
                    "Upload advisor Council review",
                    "Process the quarantined upload with the selected evidence-aware Council team.",
                    recommendation.SuggestedPrompt.Trim(),
                    key: $"upload-advisor-{Guid.NewGuid():N}",
                    teamKeys: teamKeys,
                    startsCouncilDirectly: true);
                await InvokeAsync(() => StartCouncilPromptAsync(starter, startFresh: false)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Starting an upload-processing Council recommendation failed; prompt text was omitted from diagnostics.");
                modelStatus = "The upload-processing recommendation could not be started with Council. Review LocalGPT diagnostics.";
            }
        }
    }
}
