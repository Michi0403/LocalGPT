using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Executes user-selected follow-on Council teams through the existing X-Round child run and approval boundaries.</summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>Optionally hands off a completed team's verified result to the configured next team.</summary>
        /// <param name="result">Parent run and authoritative function receipts.</param>
        /// <param name="request">Original run's participant and project context.</param>
        /// <param name="team">Saved source team and its explicit follow-on policy.</param>
        /// <param name="steps">Enabled source workflow steps for approval and provenance.</param>
        /// <param name="answer">Parent team's answer, retained if handoff is skipped or declined.</param>
        /// <param name="cancellationToken">Cancels the follow-on handoff.</param>
        /// <returns>Original answer optionally followed by the recorded child team's answer.</returns>
        private async Task<string> RunConfiguredFollowOnAsync(
            MultiModelCouncilResult result, MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team, IReadOnlyList<CouncilWorkflowStepDefinition> steps,
            string answer, CancellationToken cancellationToken)
        {
            try
            {
                var policy = team.FollowOn;
                if (policy is null || string.IsNullOrWhiteSpace(policy.TeamKey)) return answer;
                if (policy.RequireVerifiedCompletion && team.RequiredCompletedFunctions.Any(name =>
                    !HasVerifiedCouncilFunctionResult(result, name)))
                {
                    result.Warnings.Add($"Configured follow-on team '{policy.TeamKey}' was not started because required function receipts are missing.");
                    return answer;
                }
                if (string.Equals(policy.TeamKey, team.Key, StringComparison.OrdinalIgnoreCase) ||
                    request.FollowOnTeamLineage.Contains(policy.TeamKey, StringComparer.OrdinalIgnoreCase))
                {
                    result.Warnings.Add($"Configured follow-on team '{policy.TeamKey}' was not started because it repeats a team in this chain.");
                    return answer;
                }
                // The originating team's saved chain limit must survive each handoff;
                // a downstream team may reduce but must not expand that limit.
                var maximumDepth = Math.Min(Math.Clamp(policy.MaximumDepth, 1,
                    runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(
                        LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumChildCouncilDepth),
                    request.FollowOnChainDepthLimit ?? int.MaxValue);
                if (request.XRoundChildDepth >= maximumDepth)
                {
                    result.Warnings.Add($"Configured follow-on team '{policy.TeamKey}' reached the bounded child-Council depth limit.");
                    return answer;
                }
                var followOnTeam = await organicCouncilBlueprints.FindTeamAsync(policy.TeamKey, cancellationToken).ConfigureAwait(false);
                if (followOnTeam is null || !string.Equals(followOnTeam.Key, policy.TeamKey, StringComparison.OrdinalIgnoreCase))
                {
                    result.Warnings.Add($"Configured follow-on team '{policy.TeamKey}' is unavailable; no substitute was selected.");
                    return answer;
                }
                var sourceStep = steps.Last();
                var boundedAnswer = answer[..Math.Min(answer.Length, 48000)];
                var prompt = policy.PromptTemplate
                    .Replace("{{ParentAnswer}}", boundedAnswer, StringComparison.Ordinal)
                    .Replace("{{OriginalPrompt}}", request.Prompt, StringComparison.Ordinal)
                    .Replace("{{ParentTeamKey}}", team.Key, StringComparison.Ordinal)
                    .Replace("{{ParentRunId}}", result.RunId.ToString("N"), StringComparison.Ordinal);
                var directive = new CouncilXRoundDirective(
                    Guid.NewGuid(), result.RunId, result.Steps.LastOrDefault()?.Round ?? 1,
                    sourceStep.Phase, sourceStep.Key, CouncilXRoundAction.StartCouncil,
                    string.Empty, string.Empty, string.Empty, prompt,
                    policy.TeamKey, string.Empty, team.DisplayName, DateTime.UtcNow);
                if (policy.RequiresHumanApproval && !await WaitForXRoundApprovalAsync(
                    result, request, team, sourceStep, directive, cancellationToken).ConfigureAwait(false))
                    return answer;
                var childAnswer = await RunXRoundChildCouncilAsync(
                    result, request, team, sourceStep, directive, cancellationToken,
                    configuredChildDepthOverride: maximumDepth).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(childAnswer) ? answer :
                    $"{answer.TrimEnd()}{Environment.NewLine}{Environment.NewLine}{childAnswer}";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} could not complete configured follow-on team routing.", result.RunId);
                throw;
            }
        }
    }
}
