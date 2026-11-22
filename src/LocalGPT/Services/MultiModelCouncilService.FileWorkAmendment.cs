using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>Bounded planning corrections for source drafts that are not yet approved.</summary>
public sealed partial class MultiModelCouncilService
{
    /// <summary>Asks an eligible architect for an explicit delta, then applies it through the same registered,
    /// policy-gated function that owns initial source manifests. No candidate bytes or approvals are replayed.</summary>
    private async Task<bool> TryAmendIncompleteSourceDraftAsync(MultiModelCouncilResult result,
        MultiModelCouncilRequest request, CouncilFileWorkRuntimeParameters policy,
        CouncilWorkflowStepDefinition planStep, string baseUri, IReadOnlyList<string> routes,
        IReadOnlyList<string> architects, IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
        int maxParallelModels, string keepAlive, int? ollamaNumGpu, int maxContextTokens,
        int modelTimeoutSeconds, int round, CodeGenerationDraftProgress progress,
        string closureDiagnostic, CancellationToken cancellationToken)
    {
        if (!progress.IsDraft || progress.ManifestWorkOrders.Count == 0 ||
            string.IsNullOrWhiteSpace(progress.ManifestSha256) ||
            string.IsNullOrWhiteSpace(closureDiagnostic))
            return false;
        // A received human answer is part of the source requirements even after its heartbeat consumed it.
        var decisions = await humanCollaboration.GetSourcePlanningDecisionsAsync(result.RunId, cancellationToken).ConfigureAwait(false);
        var goal = string.IsNullOrWhiteSpace(request.GoalText) ? request.Prompt : request.GoalText;
        var instruction = await promptConfig.GetPromptAsync("CouncilFileWorkAmendment", cancellationToken: cancellationToken).ConfigureAwait(false);
        var previousError = closureDiagnostic;
        var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var attempts = Math.Min(Math.Max(0, policy.MaximumDraftAmendments), architects.Count);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // No speculative loop over an uncertain gateway write. Each new proposal begins with a fresh read.
            var current = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
            if (current is not { IsDraft: true } || current.ReviewId != progress.ReviewId)
                return false;
            var architect = architects.FirstOrDefault(model => tried.Add(model));
            if (architect is null) return false;
            var boundedWorkOrders = current.ManifestWorkOrders.ToDictionary(item => item.Key,
                item => item.Value.Length > Math.Max(256, policy.MaximumPlanEvidenceCharacters) ?
                    item.Value[..Math.Max(256, policy.MaximumPlanEvidenceCharacters)] : item.Value,
                StringComparer.OrdinalIgnoreCase);
            var prompt = policy.PlanPrompt + "\n" + runtimePolicy.GetString("CouncilFileWorkHumanDecisionInstruction") +
                "\n" + instruction + "\n" + JsonSerializer.Serialize(new
                {
                    Goal = goal,
                    HumanDecisions = decisions,
                    ExistingManifest = boundedWorkOrders,
                    StoredSourceHashes = current.CompletedFiles,
                    ManifestSha256 = current.ManifestSha256,
                    SourceClosureDiagnostic = previousError,
                    RequestedDotNetTargets = GetExplicitFileWorkTargets(goal)
                });
            var replies = await RunFileWorkPhaseAsync(result, request, planStep, baseUri, routes,
                [architect], round + attempt, $"{planStep.Phase} · source amendment {attempt + 1}",
                _ => prompt, modelRoutes, maxParallelModels, keepAlive, ollamaNumGpu,
                maxContextTokens, modelTimeoutSeconds, null, cancellationToken).ConfigureAwait(false);
            var reply = replies.LastOrDefault(step => string.IsNullOrWhiteSpace(step.Error));
            if (reply is null)
            {
                previousError = replies.LastOrDefault()?.Error ?? "No valid amendment response.";
                continue;
            }
            try
            {
                var fields = new Dictionary<string, JsonElement>(
                    ReadFileWorkJson<Dictionary<string, JsonElement>>(ReadFileWorkAnswer(reply)), StringComparer.OrdinalIgnoreCase);
                if (!fields.TryGetValue("draftManifest", out var manifestElement) || manifestElement.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("Amendment must contain draftManifest, even when only reopening existing paths.");
                var additions = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestElement.GetRawText()) ?? [];
                if (additions.Any(item => current.ManifestWorkOrders.ContainsKey(item.Key)))
                    throw new ArgumentException("Amendment restated an existing manifest path; provide only added paths and reopenPaths.");
                var dependencyChanges = fields.TryGetValue("draftDependencies", out var dependencyElement) &&
                    dependencyElement.ValueKind == JsonValueKind.Object
                    ? JsonSerializer.Deserialize<Dictionary<string, List<string>>>(dependencyElement.GetRawText()) ?? []
                    : new Dictionary<string, List<string>>();
                var reopenPaths = fields.TryGetValue("reopenPaths", out var reopenElement) &&
                    reopenElement.ValueKind == JsonValueKind.Array
                    ? JsonSerializer.Deserialize<List<string>>(reopenElement.GetRawText()) ?? [] : [];
                var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in reopenPaths)
                {
                    if (string.IsNullOrWhiteSpace(path) || !current.CompletedFiles.TryGetValue(path, out var hash) ||
                        !hashes.TryAdd(path, hash))
                        throw new ArgumentException("reopenPaths must contain distinct paths with exact current source receipts.");
                }
                if (additions.Count + hashes.Count == 0)
                    throw new ArgumentException("The response did not propose any new path or explicitly reopened file.");
                var submitted = await InvokeFileWorkFunctionAsync(result, request, round + attempt,
                    planStep.Phase, "codegen.review.create", new
                    {
                        draftOperation = "amend", draftAmendmentDraftId = current.ReviewId,
                        draftAmendmentManifestHash = current.ManifestSha256,
                        draftManifest = additions, draftDependencies = dependencyChanges,
                        draftAmendmentReopenHashes = hashes
                    }, cancellationToken).ConfigureAwait(false);
                if (submitted.Succeeded)
                {
                    var updated = await codeGenerationWorkflow.GetCouncilDraftProgressAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                    // Never replay an uncertain write; a changed hash or newly missing path is the receipt.
                    return updated is { IsDraft: true } && updated.ReviewId == current.ReviewId &&
                        (updated.ManifestSha256 != current.ManifestSha256 ||
                            updated.MissingPaths.Except(current.MissingPaths, StringComparer.OrdinalIgnoreCase).Any());
                }
                if (submitted.Status != "InvalidParameters") return false;
                previousError = submitted.Error ?? "Registered amendment validation rejected the proposed delta.";
            }
            catch (Exception exception) when (exception is ArgumentException or JsonException)
            {
                previousError = exception.Message;
            }
            logger.LogInformation("Council {RunId} source amendment attempt {Attempt} rejected: {Category}.",
                result.RunId, attempt + 1, "Validation");
        }
        result.Warnings.Add("Source amendment remained blocked: " + previousError);
        return false;
    }
}
