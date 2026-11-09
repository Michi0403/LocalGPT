using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Delivery evaluation for goals whose external work is approval gated on both sides (for example PublisherStudio
/// text proposals: LocalGPT approval first, then the receiving application's own authorization). Such goals are not
/// stopped for human review merely because no local file artifact exists; recorded delivery requests are the technical
/// evidence, and a missing delivery becomes a concrete continuation objective within the Keyaccount limit.
/// </summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Evidence kind for an approval-gated function request recorded by a Council run.</summary>
    private const string ApprovalGatedRequestEvidenceKind = "ApprovalGatedRequest";
    /// <summary>Step-key prefix of Keyaccount continuations whose only objective is delivering the finished result.</summary>
    private const string DeliveryStepKeyPrefix = "delivery-";
    /// <summary>Runtime collection of capability prefixes whose remote side applies work only after its own authorization.</summary>
    private const string ApprovalGatedCapabilityPrefixesKey = "GoalApprovalGatedCapabilityPrefixes";
    /// <summary>Runtime template of the delivery continuation objective.</summary>
    private const string DeliveryObjectiveTemplateKey = "GoalDeliveryObjectiveTemplate";
    /// <summary>Runtime JSON map from capability keys to the DXFunctions that serve them (shared with Council tool policy).</summary>
    private const string CapabilityNativeFunctionsKey = "CouncilCapabilityNativeFunctionsJson";

    /// <summary>
    /// Determines whether every external requirement of the goal is an approval-gated delivery capability, so
    /// intermediate unverifiable steps may continue to the next planned step without replaying earlier effects.
    /// </summary>
    /// <param name="checkpoint">Durable goal journal.</param>
    /// <returns><see langword="true"/> for goals that only require approval-gated delivery capabilities.</returns>
    private bool IsApprovalGatedDeliveryGoal(GoalExecutionCheckpoint checkpoint)
    {
        try
        {
            if (checkpoint.RequiredFunctions.Count > 0 || checkpoint.RequiredCapabilities.Count == 0)
                return false;
            IReadOnlyCollection<string> prefixes;
            try { prefixes = runtimePolicy.GetCollection(ApprovalGatedCapabilityPrefixesKey); }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Runtime collection {Key} is unavailable; goal {GoalId} keeps the strict verification stop.", ApprovalGatedCapabilityPrefixesKey, checkpoint.GoalId);
                return false;
            }
            return prefixes.Count > 0 && checkpoint.RequiredCapabilities.All(capability =>
                prefixes.Any(prefix => !string.IsNullOrWhiteSpace(prefix) &&
                    capability.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not classify the external requirements of goal {GoalId}.", checkpoint.GoalId);
            return false;
        }
    }

    /// <summary>Reads the approval-gated function requests one Council run recorded and turns them into goal evidence.</summary>
    /// <param name="councilRunId">Council run of the goal step.</param>
    /// <param name="cancellationToken">Cancels the evidence query.</param>
    /// <returns>One evidence record per recorded request; succeeded unless it was declined or failed.</returns>
    private async Task<List<GoalExecutionEvidence>> ObserveApprovalGatedRequestsAsync(Guid councilRunId, CancellationToken cancellationToken)
    {
        try
        {
            var vocabularySnapshot = vocabulary.Get();
            var requests = await deferredInvocations.GetForRunAsync(councilRunId, cancellationToken).ConfigureAwait(false);
            var evidence = requests.Select(item => new GoalExecutionEvidence
            {
                Kind = ApprovalGatedRequestEvidenceKind,
                Source = item.FunctionName + ":" + item.Status,
                Succeeded = !string.Equals(item.Status, vocabularySnapshot.DeferredDeclined, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(item.Status, vocabularySnapshot.DeferredFailed, StringComparison.OrdinalIgnoreCase),
                CouncilRunId = councilRunId
            }).ToList();
            logger.LogInformation("Council run {CouncilRunId} recorded {Count} approval-gated delivery request(s): {Requests}.",
                councilRunId, evidence.Count, evidence.Count == 0 ? "none" : string.Join(", ", evidence.Select(item => item.Source)));
            return evidence;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read approval-gated delivery evidence for Council run {CouncilRunId}; the step is reviewed without it.", councilRunId);
            return [];
        }
    }

    /// <summary>
    /// Applies the deterministic delivery rule to a Keyaccount review of an approval-gated delivery goal: without a
    /// recorded delivery request the goal is unfinished and receives the delivery objective (no human stop); with a
    /// recorded request, a reviewer demand for human review is satisfied by the pending approval itself.
    /// </summary>
    /// <param name="checkpoint">Durable goal journal including the current receipt.</param>
    /// <param name="review">Review returned by the Keyaccount Manager (or its failure placeholder).</param>
    private void ApplyApprovalGatedDeliveryRule(GoalExecutionCheckpoint checkpoint, GoalKeyaccountReview review)
    {
        try
        {
            if (!IsApprovalGatedDeliveryGoal(checkpoint))
                return;
            var delivered = checkpoint.Receipts.SelectMany(item => item.Evidence)
                .Any(item => item.Succeeded && item.Kind == ApprovalGatedRequestEvidenceKind);
            if (!delivered)
            {
                review.Complete = false;
                review.NeedsHumanReview = false;
                review.NextObjective = BuildDeliveryObjective(checkpoint);
                review.Reason = string.IsNullOrWhiteSpace(review.Reason)
                    ? "The result has not been delivered through the requested approval-gated capability yet"
                    : review.Reason + "; the result has not been delivered through the requested approval-gated capability yet";
                logger.LogInformation("Goal {GoalId} has no recorded delivery request; the Keyaccount Manager schedules delivery instead of stopping for human review.", checkpoint.GoalId);
                return;
            }

            if (review.NeedsHumanReview)
            {
                // The delivery request already waits for the local human in the approval inbox; a second,
                // unspecific human stop would only block the remaining planned work.
                review.NeedsHumanReview = false;
                if (!review.Complete && string.IsNullOrWhiteSpace(review.NextObjective))
                    review.Complete = true;
                review.Reason = (string.IsNullOrWhiteSpace(review.Reason) ? string.Empty : review.Reason + "; ") +
                    "delivery request recorded and waiting for the local approval decision";
            }
            logger.LogInformation("Goal {GoalId} has a recorded approval-gated delivery request; review complete={Complete}.", checkpoint.GoalId, review.Complete);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not apply the approval-gated delivery rule to goal {GoalId}; the reviewer result is kept unchanged.", checkpoint.GoalId);
        }
    }

    /// <summary>Renders the database-owned delivery objective for the goal's requested capabilities.</summary>
    /// <param name="checkpoint">Durable goal journal.</param>
    /// <returns>The delivery continuation objective.</returns>
    private string BuildDeliveryObjective(GoalExecutionCheckpoint checkpoint)
    {
        try
        {
            var functions = ResolveDeliveryFunctions(checkpoint);
            string template;
            try { template = runtimePolicy.GetString(DeliveryObjectiveTemplateKey); }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Runtime value {Key} is unavailable; using the built-in delivery objective.", DeliveryObjectiveTemplateKey);
                template = "Deliver the finished result of the original goal now through {{Functions}} exactly as the original request specifies (target, format and full text). Reuse the completed step outputs; do not redo earlier work.";
            }
            return template
                .Replace("{{Functions}}", functions.Count == 0 ? string.Join(", ", checkpoint.RequiredCapabilities) : string.Join(", ", functions), StringComparison.Ordinal)
                .Replace("{{Capabilities}}", string.Join(", ", checkpoint.RequiredCapabilities), StringComparison.Ordinal)
                .Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not render the delivery objective of goal {GoalId}.", checkpoint.GoalId);
            throw;
        }
    }

    /// <summary>Maps the goal's required capabilities to the DXFunctions that deliver through them.</summary>
    /// <param name="checkpoint">Durable goal journal.</param>
    /// <returns>Serving function names; empty when the runtime map is unavailable.</returns>
    private IReadOnlyList<string> ResolveDeliveryFunctions(GoalExecutionCheckpoint checkpoint)
    {
        try
        {
            var map = new Dictionary<string, List<string>>(
                runtimePolicy.GetJson<Dictionary<string, List<string>>>(CapabilityNativeFunctionsKey), StringComparer.OrdinalIgnoreCase);
            return checkpoint.RequiredCapabilities
                .SelectMany(capability => map.TryGetValue(capability, out var served) ? served : Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime value {Key} is unavailable; the delivery objective names capabilities instead of functions.", CapabilityNativeFunctionsKey);
            return [];
        }
    }

    /// <summary>
    /// Selects the clean deliverable of the goal: the answer of the last content step (delivery continuations only
    /// report the hand-over), without a leading "Final answer:" label.
    /// </summary>
    /// <param name="checkpoint">Durable goal journal.</param>
    /// <returns>The deliverable text, or an empty string when no step produced visible output.</returns>
    private string BuildDeliverableText(GoalExecutionCheckpoint checkpoint)
    {
        try
        {
            var answered = checkpoint.Receipts
                .Where(receipt => (receipt.State is GoalExecutionState.Completed or GoalExecutionState.NeedsVerification) &&
                    !string.IsNullOrWhiteSpace(receipt.Answer))
                .ToList();
            var content = answered.LastOrDefault(receipt => !receipt.StepKey.StartsWith(DeliveryStepKeyPrefix, StringComparison.Ordinal))
                ?? answered.LastOrDefault();
            var text = content?.Answer.Trim() ?? string.Empty;
            const string label = "Final answer:";
            if (text.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                text = text[label.Length..].TrimStart();
            return text;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not select the deliverable of goal {GoalId}; callers fall back to the full final answer.", checkpoint.GoalId);
            return string.Empty;
        }
    }
}
