using LocalGPT.BusinessObjects;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Read-only, team-independent final review after every GoalExecution Council subrun.
/// This role is NOT an additional team, function registry or approval authority: it can only
/// propose the next distinct goal. It cannot claim tool writes from its own prose.
/// </summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Checks remaining user intent against observed output and technical evidence, without invoking any tool.</summary>
    private async Task<GoalKeyaccountReview> ReviewKeyaccountCompletionAsync(
        GoalExecutionRequest request, GoalExecutionCheckpoint checkpoint, GoalExecutionPlanStep step,
        MultiModelCouncilResult result, GoalExecutionStepReceipt receipt, CancellationToken cancellationToken)
    {
        var review = new GoalKeyaccountReview { CouncilRunId = result.RunId, StepKey = step.Key,
            Complete = false, NeedsHumanReview = false, Reason = "Keyaccount reviewer unavailable" };
        if (string.IsNullOrWhiteSpace(receipt.Answer))
        {
            review.Reason = "Council step returned no final answer or had participant failures";
            review.NeedsHumanReview = true;
            return review;
        }
        try
        {
            var choices = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            // A user-configured reviewer may be independent of the chosen Council team;
            // blank inherits one of its explicit provider selections. Never auto-switch a
            // deliberately configured reviewer to an unrelated provider.
            var reviewerKey = runtimePolicy.GetString(LocalGptRuntimeValue.GoalKeyaccountReviewerModelKey)?.Trim();
            var selected = choices.FirstOrDefault(candidate => candidate.IsConfigured &&
                (candidate.IsInstalled || !candidate.IsLocal) &&
                (string.IsNullOrWhiteSpace(reviewerKey)
                    ? checkpoint.ModelSelectionKeys.Any(key =>
                        string.Equals(key, candidate.SelectionKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(key, candidate.ModelName, StringComparison.OrdinalIgnoreCase))
                    : string.Equals(reviewerKey, candidate.SelectionKey, StringComparison.OrdinalIgnoreCase)));
            if (selected is null)
            {
                review.NeedsHumanReview = true;
                review.Reason = "Configured Keyaccount reviewer or selected Council model is unavailable";
                return review;
            }
            var policy = runtimePolicy.GetJson<ProviderModelRuntimeParameters>(LocalGptRuntimeValue.ProviderModelRuntimeParametersJson);
            using var client = providerModels.CreateChatClient(selected.ToReference(), policy.DefaultSessionKeepAlive,
                request.CouncilRequest.MaxContextTokens,
                TimeSpan.FromSeconds(Math.Max(10, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalPlannerTimeoutSeconds))),
                request.CouncilRequest.OllamaNumGpu, enableAutomaticTools: false, throwOnFailure: true);
            // The review operates on bounded text and source-attributed receipts. Untrusted Council
            // prose is data only. It does not become a higher-priority instruction or evidence.
            var reviewInput = JsonSerializer.Serialize(new
            {
                originalGoal = checkpoint.GoalText.Length > 12000 ? checkpoint.GoalText[..12000] : checkpoint.GoalText,
                originalPrompt = checkpoint.Prompt.Length > 12000 ? checkpoint.Prompt[..12000] : checkpoint.Prompt,
                currentObjective = step.Objective,
                plan = checkpoint.Steps.Select(item => new { item.Key, item.Objective }),
                completed = checkpoint.Receipts.Where(item => item.State == GoalExecutionState.Completed)
                    .Select(item => new { item.StepKey, answer = item.Answer.Length > 1500 ? item.Answer[..1500] : item.Answer }).TakeLast(12),
                thisAnswer = receipt.Answer.Length > 10000 ? receipt.Answer[..10000] : receipt.Answer,
                evidence = receipt.Evidence.Select(item => new { item.Kind, item.Source, item.Succeeded }),
                warnings = result.Warnings.Take(16).Select(warning => warning.Length > 500 ? warning[..500] : warning),
                alreadyAdded = checkpoint.KeyaccountContinuationsScheduled,
                requiredVerifiedArtifactCount = checkpoint.KeyaccountExpectedArtifacts,
                verifiedArtifactCount = checkpoint.Receipts.SelectMany(item => item.Evidence)
                    .Where(item => item.Kind == "CouncilArtifact" && item.Succeeded)
                    .Select(item => item.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                permittedAdditionalRounds = checkpoint.KeyaccountContinuationLimit
            });
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(10, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalPlannerTimeoutSeconds))));
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.System, runtimePolicy.GetString(LocalGptRuntimeValue.GoalKeyaccountManagerSystemPrompt)),
                 new ChatMessage(ChatRole.User, reviewInput)],
                new ChatOptions { MaxOutputTokens = 512, Temperature = 0f }, timeout.Token).ConfigureAwait(false);
            var text = System.Net.WebUtility.HtmlDecode(response.Text ?? string.Empty);
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return review;
            using var document = JsonDocument.Parse(text.Substring(start, end - start + 1));
            var data = document.RootElement;
            if (!data.TryGetProperty("complete", out var complete) ||
                complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return review;
            review.ReviewerResponded = true;
            review.Complete = complete.GetBoolean();
            review.NextObjective = ReadString(data, "nextObjective", 2000);
            review.Reason = ReadString(data, "reason", 1000);
            review.NeedsHumanReview = data.TryGetProperty("needsHumanReview", out var human) &&
                human.ValueKind == JsonValueKind.True;
            // Model evaluation is not evidence of external modifications. Only the existing
            // DXFunction/tool approval and artifact contracts may attest those effects.
            if (checkpoint.RequiredFunctions.Count > 0 || checkpoint.RequiredCapabilities.Count > 0)
            {
                if (!receipt.Evidence.Any(item => item.Succeeded && item.Kind == "CouncilArtifact"))
                {
                    review.Complete = false;
                    review.NeedsHumanReview = true;
                    review.Reason = "Required external work lacks a recorded generated artifact; review receipts";
                }
            }
            var verifiedFiles = checkpoint.Receipts.SelectMany(item => item.Evidence)
                .Where(item => item.Kind == "CouncilArtifact" && item.Succeeded)
                .Select(item => item.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (checkpoint.KeyaccountExpectedArtifacts > verifiedFiles)
            {
                var remaining = checkpoint.KeyaccountExpectedArtifacts - verifiedFiles;
                review.Complete = false;
                review.Reason = $"Only {verifiedFiles} of {checkpoint.KeyaccountExpectedArtifacts} required distinct generated files were verified";
                if (string.IsNullOrWhiteSpace(review.NextObjective))
                    review.NextObjective = $"Produce the next distinct, nonduplicating deliverables toward the original goal; {remaining} verified files are still missing. Obtain approval for all applicable functions.";
            }
            if (!review.Complete && string.IsNullOrWhiteSpace(review.NextObjective))
                review.NeedsHumanReview = true;
            return review;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            logger.LogWarning(error, "Keyaccount review failed for Council run {RunId}; retaining incomplete state.", result.RunId);
            review.NeedsHumanReview = true;
            review.Reason = "Keyaccount review could not be completed; inspect the Council log";
            return review;
        }
    }

    /// <summary>Extracts a bounded, untrusted reviewer JSON string without trusting it as a capability.</summary>
    /// <param name="value">Parsed reviewer object.</param>
    /// <param name="property">Expected property name.</param>
    /// <param name="maxLength">Maximum returned length.</param>
    /// <returns>Sanitized reviewer text, or empty when absent.</returns>
    private string ReadString(JsonElement value, string property, int maxLength)
    {
        if (!value.TryGetProperty(property, out var field) || field.ValueKind != JsonValueKind.String)
            return string.Empty;
        var text = (field.GetString() ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
