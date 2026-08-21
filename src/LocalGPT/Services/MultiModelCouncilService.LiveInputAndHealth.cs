using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Builds live council interruption prompt as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="contributions">Human council contribution dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildLiveCouncilInterruptionPrompt(IReadOnlyList<HumanCouncilContribution> contributions)
        {
            try
            {
                var builder = new StringBuilder()
                    .AppendLine("The local user added new conversation input while your previous response was still generating.")
                    .AppendLine("This is the highest-priority current conversation context. React to every entry now, revise incompatible assumptions, and explicitly answer or acknowledge it.")
                    .AppendLine("Do not claim that you cannot see the message. Do not continue the old draft unchanged. Do not transform it into an unrelated older project request.")
                    .AppendLine("LocalGPT is general-purpose: available functions and Council roles do not limit ordinary assistance to LocalGPT development.");

                foreach (var contribution in contributions)
                {
                    builder.AppendLine()
                        .AppendLine("<<<LOCALGPT_LIVE_USER_INPUT")
                        .Append("Author: ").AppendLine(contribution.HumanDisplayName)
                        .Append("Role: ").AppendLine(contribution.HumanRole)
                        .AppendLine("Content:")
                        .AppendLine(contribution.Content)
                        .AppendLine("LOCALGPT_LIVE_USER_INPUT>>>");
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not build a live Council interruption prompt; user message content was omitted from logs.");
                return "The local user sent a live message. Stop the old draft and respond to the visible current user message directly.";
            }
        }

        /// <summary>
        /// Performs limit live council context as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maximumCharacters">Maximum characters value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string LimitLiveCouncilContext(string value, int maximumCharacters)
        {
    try
    {
                if (string.IsNullOrEmpty(value) || value.Length <= maximumCharacters)
                    return value;
                return value[^maximumCharacters..];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(LimitLiveCouncilContext)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(LimitLiveCouncilContext)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs retry participant with safe limits as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="councilMembers">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="prompt">Prompt value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxOutputTokens">Max output tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="originalFailure">Original failure value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="executionPlan">The exact run-scoped hardware road used by the failed attempt. Recovery preserves its acceleration policy.</param>
        /// <returns>The multi model council step produced by the operation.</returns>
        private async Task<MultiModelCouncilStep?> RetryParticipantWithSafeLimitsAsync(
            string baseUri,
            string modelName,
            IReadOnlyList<string> councilMembers,
            int round,
            string phase,
            string role,
            string prompt,
            string bootstrap,
            int maxOutputTokens,
            string keepAlive,
            int maxContextTokens,
            int modelTimeoutSeconds,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken,
            string originalFailure,
            CouncilHardwareRoadPlan executionPlan)
        {
            try
            {
                var recoveryOutput = Math.Clamp(Math.Min(maxOutputTokens, 8192), catalog.MinOutputTokens, catalog.MaxOutputTokens);
                var recoveryContext = Math.Clamp(Math.Min(maxContextTokens, 65536), catalog.MinContextTokens, catalog.MaxContextTokens);
                var recoveryModel = await providerModels.ResolveAsync(modelName, cancellationToken).ConfigureAwait(false);
                var isOllama = recoveryModel.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase);
                if (isOllama)
                {
                    var availabilityWait = TimeSpan.FromSeconds(Math.Clamp(modelTimeoutSeconds / 6, 30, 120));
                    streamUpdate?.Invoke(
                        Environment.NewLine + Environment.NewLine +
                        $"> {WebUtility.HtmlEncode(modelName)} failed in {WebUtility.HtmlEncode(phase)}. LocalGPT is checking the same Ollama host/model for reavailability before retrying; the current hardware road remains unchanged." +
                        Environment.NewLine + Environment.NewLine);
                    var available = await providerModels
                        .WaitForAvailabilityAsync(recoveryModel, availabilityWait, cancellationToken)
                        .ConfigureAwait(false);
                    if (!available)
                        return null;
                }

                var recoveryPlan = executionPlan with
                {
                    EffectiveMaxOutputTokens = recoveryOutput,
                    EffectiveMaxContextTokens = recoveryContext
                };
                var acceleration = isOllama
                    ? $"Ollama num_gpu={(recoveryPlan.OllamaNumGpu?.ToString() ?? "auto")}"
                    : $"{recoveryModel.ProviderName} provider route";
                var recoveryDescription =
                    $"bounded retry on existing hardware road {recoveryPlan.LaneKey}; {acceleration}";
                streamUpdate?.Invoke(
                    Environment.NewLine + Environment.NewLine +
                    $"> {WebUtility.HtmlEncode(modelName)} is retrying once with {WebUtility.HtmlEncode(recoveryDescription)}." +
                    Environment.NewLine + Environment.NewLine);
                logger.LogInformation(
                    "Retrying Council participant {ModelName} after failure in {Phase} with output {MaxOutputTokens}, context {MaxContextTokens}, preserved hardware road {HardwareRoad}, Ollama num_gpu {OllamaNumGpu}.",
                    modelName,
                    phase,
                    recoveryOutput,
                    recoveryContext,
                    recoveryPlan.LaneKey,
                    recoveryPlan.OllamaNumGpu?.ToString() ?? "auto");
                var recovered = await RunParticipantAsync(
                    baseUri,
                    modelName,
                    councilMembers,
                    round,
                    phase,
                    $"{role} (automatic recovery)",
                    prompt + Environment.NewLine + Environment.NewLine +
                    "Recovery instruction: the previous attempt failed. Produce a concise final answer, avoid optional tools, and report only actionable blockers.",
                    bootstrap,
                    recoveryOutput,
                    keepAlive,
                    isOllama ? recoveryPlan.OllamaNumGpu : null,
                    recoveryContext,
                    Math.Max(60, Math.Min(modelTimeoutSeconds, 600)),
                    streamUpdate,
                    cancellationToken,
                    allowRecovery: false,
                    fallbackPlan: recoveryPlan,
                    useRunConfiguration: false).ConfigureAwait(false);
                if (recovered is null)
                    return null;
                if (string.IsNullOrWhiteSpace(recovered.Error))
                {
                    recovered.VisibleContent = $"_Automatic recovery succeeded after: {originalFailure}_" + Environment.NewLine + Environment.NewLine + recovered.VisibleContent;
                    recovered.Content = recovered.VisibleContent;
                    return recovered;
                }

                recovered.Error = $"{originalFailure} | Recovery failed: {recovered.Error}";
                recovered.VisibleContent = $"**{modelName} failed and its automatic recovery also failed.**" + Environment.NewLine + recovered.Error;
                recovered.Content = recovered.VisibleContent;
                return recovered;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Automatic Council recovery failed for {ModelName} in {Phase}.", modelName, phase);
                return null;
            }
        }

        /// <summary>
        /// Performs select healthy participant as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="preferredModel">Preferred model value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string SelectHealthyParticipant(
            MultiModelCouncilResult result,
            IReadOnlyList<string> participants,
            string? preferredModel = null)
        {
    try
    {
                if (participants.Count == 0)
                    throw new InvalidOperationException("The Council has no model participant available.");

                var failedModels = result.Steps
                    .Where(step => !string.IsNullOrWhiteSpace(step.Error))
                    .Select(step => step.ModelName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(preferredModel) && !failedModels.Contains(preferredModel))
                    return preferredModel;
                return participants.FirstOrDefault(model => !failedModels.Contains(model)) ?? participants[0];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(SelectHealthyParticipant)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(SelectHealthyParticipant)} failed.");
        throw;
    }
}

        /// <summary>
        /// Applies approved one run model exclusions as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="selectedParticipants">Selected participants value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<(List<string> Active, List<string> Excluded)> ApplyApprovedOneRunModelExclusionsAsync(
            List<string> selectedParticipants,
            CancellationToken cancellationToken)
        {
            var active = selectedParticipants.ToList();
            var excluded = new List<string>();
            try
            {
                var snapshot = await humanCollaboration.GetSnapshotAsync(includeResolved: true, take: 200, cancellationToken).ConfigureAwait(false);
                foreach (var modelName in selectedParticipants)
                {
                    var spec = CreateModelHealthExclusionRequest(modelName, null, string.Empty);
                    var approved = snapshot.Requests.FirstOrDefault(request =>
                        string.Equals(request.CorrelationId, spec.CorrelationId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(request.OperationKey, spec.OperationKey, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(request.Status, vocabulary.Get().HumanStatusApproved, StringComparison.OrdinalIgnoreCase));
                    if (approved is null || active.Count <= 1)
                        continue;

                    var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(spec, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (!gate.IsAuthorized)
                        continue;
                    active.RemoveAll(model => string.Equals(model, modelName, StringComparison.OrdinalIgnoreCase));
                    excluded.Add(modelName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Approved one-run model exclusions could not be applied. The selected Council models remain available.");
            }
            return (active, excluded);
        }

        /// <summary>
        /// Performs queue model health exclusion review as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task QueueModelHealthExclusionReviewAsync(
            MultiModelCouncilResult result,
            string modelName,
            CancellationToken cancellationToken)
        {
            try
            {
                var failures = result.Steps
                    .Where(step => string.Equals(step.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(step.Error))
                    .Select(step => $"{step.Phase}: {step.Error}")
                    .Take(4)
                    .ToList();
                var spec = CreateModelHealthExclusionRequest(modelName, result.RunId, string.Join(" | ", failures));
                await humanCollaboration.AuthorizeOrEnqueueAsync(spec, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Warnings.Add($"A local approval was queued to exclude {modelName} from one subsequent Council run. This does not permanently disable the model.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not queue a model-health review for {ModelName}.", modelName);
            }
        }

        /// <summary>
        /// Creates model health exclusion request as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
        /// <param name="failureSummary">Failure summary value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The human approval request spec produced by the operation.</returns>
        private HumanApprovalRequestSpec CreateModelHealthExclusionRequest(
            string modelName,
            Guid? councilRunId,
            string failureSummary)
        {
    try
    {
                var normalized = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(modelName.Trim())))[..16].ToLowerInvariant();
                return new HumanApprovalRequestSpec(
                    CorrelationId: $"council:model-health:{normalized}",
                    OperationKey: $"council.model.exclude-next-run.{normalized}",
                    Title: $"Exclude failed model once: {modelName}",
                    Description: string.IsNullOrWhiteSpace(failureSummary)
                        ? $"A previous Council run requested that {modelName} be skipped for one run after repeated recovery failure."
                        : $"{modelName} failed after LocalGPT's bounded automatic recovery. Approving skips it for one subsequent Council run, then it becomes eligible for benchmarking again. Evidence: {failureSummary}",
                    RiskLevel: "Low",
                    Source: nameof(MultiModelCouncilService),
                    RequestedBy: "AI Council health guard",
                    RequestedRole: "Local model reliability reviewer",
                    CouncilRunId: councilRunId,
                    EarliestCouncilRound: 0,
                    RequiredBeforeCompletion: false,
                    IsSensitive: false,
                    SuggestedResponsesText: "Exclude for one run\nKeep available and retry",
                    ResponsePrompt: "Approve only when the failed model should be skipped for the next Council run.",
                    AllowFreeText: true);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateModelHealthExclusionRequest)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateModelHealthExclusionRequest)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs order participants by observed health as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IEnumerable<string> OrderParticipantsByObservedHealth(
            MultiModelCouncilResult result,
            IEnumerable<string> participants)
        {
    try
    {
                var originalOrder = participants.Select((model, index) => new { Model = model, Index = index }).ToList();
                return originalOrder
                    .Select(item => new
                    {
                        item.Model,
                        item.Index,
                        Failed = result.Steps.Count(step =>
                            string.Equals(step.ModelName, item.Model, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(step.Error)),
                        SuccessfulDurations = result.Steps
                            .Where(step =>
                                string.Equals(step.ModelName, item.Model, StringComparison.OrdinalIgnoreCase) &&
                                string.IsNullOrWhiteSpace(step.Error) &&
                                step.DurationSeconds > 0)
                            .Select(step => step.DurationSeconds)
                            .ToList()
                    })
                    .OrderBy(item => item.Failed)
                    .ThenBy(item => item.SuccessfulDurations.Count == 0 ? double.MaxValue : item.SuccessfulDurations.Average())
                    .ThenBy(item => item.Index)
                    .Select(item => item.Model);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(OrderParticipantsByObservedHealth)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(OrderParticipantsByObservedHealth)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs append runtime benchmark summary as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        private void AppendRuntimeBenchmarkSummary(MultiModelCouncilResult result)
        {
    try
    {
                foreach (var group in result.Steps
                    .Where(step => !string.IsNullOrWhiteSpace(step.ModelName))
                    .GroupBy(step => step.ModelName, StringComparer.OrdinalIgnoreCase))
                {
                    var completed = group.Where(step => string.IsNullOrWhiteSpace(step.Error)).ToList();
                    var measured = completed.Where(step => step.DurationSeconds > 0).ToList();
                    var failed = group.Count(step => !string.IsNullOrWhiteSpace(step.Error));
                    var successRate = group.Any() ? (int)Math.Round(completed.Count * 100d / group.Count()) : 0;
                    var averageSeconds = measured.Count == 0 ? 0 : measured.Average(step => step.DurationSeconds);
                    var maximumLoad = group.Max(step => step.EffectiveLoadPercent);
                    var maximumOutput = group.Max(step => step.EffectiveMaxOutputTokens);
                    var maximumContext = group.Max(step => step.EffectiveMaxContextTokens);
                    result.Warnings.Add(
                        $"Runtime benchmark {group.Key}: {successRate}% successful across {group.Count()} step(s), " +
                        $"average {averageSeconds:0.0}s for completed measured steps, {failed} failure(s), " +
                        $"observed road up to {maximumLoad}% / output {maximumOutput:n0} / context {maximumContext:n0}. " +
                        "This run-local evidence is persisted with the Council knowledge entry and does not silently rewrite user-approved hardware roads.");
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(AppendRuntimeBenchmarkSummary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(AppendRuntimeBenchmarkSummary)} failed.");
        throw;
    }
}

        /// <summary>
        /// Retrieves configured Ollama providers as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The collection produced by the operation.</returns>
        private IEnumerable<OllamaCoreOptions> GetConfiguredOllamaProviders()
        {
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(options.OllamaCore.Uri))
                {
                    seen.Add($"{councilText.MultiModelCouncilServiceNormalizeEndpoint(options.OllamaCore.Uri, logger)}|{options.OllamaCore.ModelName}");
                    yield return options.OllamaCore;
                }

                foreach (var provider in options.OllamaCores.Where(provider => !string.IsNullOrWhiteSpace(provider.Uri)))
                {
                    if (seen.Add($"{councilText.MultiModelCouncilServiceNormalizeEndpoint(provider.Uri, logger)}|{provider.ModelName}"))
                        yield return provider;
                }
            }
            finally
            {
                logger.LogInformation($"Ended GetConfiguredOllamaProviders");
            }
            
        }

        /// <summary>
        /// Performs probe Ollama models as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> ProbeOllamaModelsAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                using var http = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var tags = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", cancellationToken).ConfigureAwait(false) ?? new OllamaTagsResponse();
                var running = await MultiModelCouncilServiceProbeRunningModelNamesAsync(http, cancellationToken, logger).ConfigureAwait(false);

                return tags.Models
                    .Where(model => !string.IsNullOrWhiteSpace(model.Name))
                    .Select(model =>
                    {
                        var modelName = model.Name!.Trim();
                        return new MultiModelCouncilModelCandidate(
                            modelName,
                            "Installed Ollama",
                            endpoint,
                            IsInstalled: true,
                            IsConfigured: false,
                            IsLoaded: running.Contains(modelName),
                            Details: string.Join(", ", new[]
                            {
                                model.Details?.Family,
                                model.Details?.ParameterSize,
                                model.Details?.QuantizationLevel
                            }.Where(value => !string.IsNullOrWhiteSpace(value))));
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not probe Ollama models at {Endpoint}.", endpoint);
                return [];
            }
        }

    }
}
