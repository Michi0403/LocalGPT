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
        /// Performs run participant as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="allowRecovery">Value indicating whether allow recovery should apply to this operation.</param>
        /// <param name="fallbackPlan">Fallback plan value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="progressMessage">Progress message value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="useRunConfiguration">Value indicating whether use run configuration should apply to this operation.</param>
        /// <param name="enableAutomaticTools">Whether provider-native/DX automatic tool metadata is exposed for this exact workflow step.</param>
        /// <param name="automaticFunctionAllowList">Optional exact registered-function allow-list for this workflow step.</param>
        /// <param name="roleComplianceRetryCount">Number of bounded corrective retries allowed for generic refusal or non-performance of the assigned role task.</param>
        /// <param name="finalAnswerRecoveryEnabled">Whether a separate provider turn may recover a missing final answer after streamed thinking/tool activity.</param>
        /// <param name="finalAnswerRecoveryMaxOutputTokens">Maximum output-token budget for the optional final-answer recovery turn.</param>
        /// <returns>The multi model council step produced by the operation.</returns>
        private async Task<MultiModelCouncilStep?> RunParticipantAsync(
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
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken,
            bool allowRecovery = true,
            CouncilHardwareRoadPlan? fallbackPlan = null,
            Action<string>? progressMessage = null,
            bool useRunConfiguration = true,
            bool enableAutomaticTools = true,
            IReadOnlyCollection<string>? automaticFunctionAllowList = null,
            int roleComplianceRetryCount = 1,
            bool finalAnswerRecoveryEnabled = true,
            int finalAnswerRecoveryMaxOutputTokens = 8192)
        {
            try
            {
                var started = DateTime.UtcNow;
                var stopwatch = Stopwatch.StartNew();
                var providerModel = await providerModels.ResolveAsync(modelName, cancellationToken).ConfigureAwait(false);
                var councilRunId = ambientContext.Current.CouncilRunId;
                var roundSkipToken = councilRunId is Guid runId
                    ? runConfigurations.GetRoundCancellationToken(runId, round, phase)
                    : CancellationToken.None;
                var providerOllamaNumGpu = providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                    ? ollamaNumGpu
                    : null;
                var executionPlan = fallbackPlan ?? new CouncilHardwareRoadPlan(
                    modelName,
                    providerOllamaNumGpu == 0 ? OneWireHardwareKind.Cpu : OneWireHardwareKind.Auto,
                    providerOllamaNumGpu == 0 ? 0 : -1,
                    providerOllamaNumGpu == 0 ? "CPU" : "Automatic",
                    providerOllamaNumGpu == 0 ? "cpu:0:CPU" : $"auto:{modelName}",
                    100,
                    maxOutputTokens,
                    maxContextTokens,
                    providerOllamaNumGpu,
                    1);
                ICouncilModelRequestLease? runtimeLease = null;
                var participantRequestStarted = false;

                try
                {
                    if (useRunConfiguration && councilRunId is Guid activeRunId)
                    {
                        runtimeLease = await runConfigurations
                            .AcquireModelRequestAsync(activeRunId, modelName, executionPlan, cancellationToken)
                            .ConfigureAwait(false);
                        executionPlan = runtimeLease.Plan;
                        if (!runtimeLease.IsEnabled)
                        {
                            stopwatch.Stop();
                            var skipped = new MultiModelCouncilStep
                            {
                                Round = round,
                                Phase = phase,
                                ModelName = modelName,
                                ProviderName = providerModel.ProviderName,
                                ProviderEndpoint = providerModel.Endpoint,
                                ProviderModelName = providerModel.ModelName,
                                CouncilMembers = councilMembers.ToList(),
                                Role = role,
                                Content = $"_{modelName} was disabled for this running Council session before its next request started._",
                                VisibleContent = $"_{modelName} was disabled for this running Council session before its next request started._",
                                StartedAtUtc = started,
                                CompletedAtUtc = DateTime.UtcNow,
                                DurationSeconds = stopwatch.Elapsed.TotalSeconds
                            };
                            ApplyHardwarePlan(skipped, executionPlan);
                            progressMessage?.Invoke($"Skipped {modelName} because run-scoped settings revision {runtimeLease.Revision} disabled this member before its request started.");
                            return skipped;
                        }

                        maxOutputTokens = executionPlan.EffectiveMaxOutputTokens;
                        maxContextTokens = executionPlan.EffectiveMaxContextTokens;
                        ollamaNumGpu = executionPlan.OllamaNumGpu;
                        var currentRunConfiguration = runConfigurations.Get(activeRunId);
                        if (currentRunConfiguration is { IsRunning: true })
                            modelTimeoutSeconds = Math.Clamp(currentRunConfiguration.ModelTimeoutSeconds, 30, 1800);
                        var accelerationSummary = providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                            ? $"Ollama num_gpu={(ollamaNumGpu?.ToString() ?? "auto")}"
                            : $"{providerModel.ProviderName} provider route";
                        progressMessage?.Invoke(
                            $"Starting {modelName}: {phase} / {role} on {executionPlan.LaneKey} at {executionPlan.EffectiveLoadPercent}% of its run-scoped road. " +
                            $"Settings revision {runtimeLease.Revision}; {accelerationSummary}; output={maxOutputTokens}; context={maxContextTokens}.");
                    }

                    // DurationSeconds is provider execution time, not time spent waiting for this host/lane lease.
                    // This keeps small-model timing comparable in large sequential-per-host Councils.
                    started = DateTime.UtcNow;
                    stopwatch.Restart();
                    participantRequestStarted = true;
                    using var client = providerModels.CreateChatClient(
                        providerModel,
                        keepAlive,
                        maxContextTokens,
                        TimeSpan.FromSeconds(modelTimeoutSeconds + 15),
                        providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                            ? ollamaNumGpu
                            : null,
                        enableAutomaticTools: enableAutomaticTools,
                        automaticFunctionAllowList: automaticFunctionAllowList);

                    using var participantCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, roundSkipToken);
                    participantCts.CancelAfter(TimeSpan.FromSeconds(modelTimeoutSeconds));

                    var participantBootstrap = bootstrap;
                    var observedContributionIds = new HashSet<Guid>();
                    if (councilRunId is Guid heartbeatRunId)
                    {
                        // A direct message queued after the phase heartbeat but before this participant
                        // starts belongs to the shared Council context. Include it from the beginning
                        // without claiming/restarting this model. Only a model that was already streaming
                        // may atomically claim the immediate interrupt path below.
                        var queuedHeartbeatMessages = (await humanCollaboration
                            .ReadQueuedContributionsAsync(heartbeatRunId, round, cancellationToken)
                            .ConfigureAwait(false))
                            .Where(item => item.HumanRole.Equals("Direct user message", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (queuedHeartbeatMessages.Count > 0)
                        {
                            foreach (var contribution in queuedHeartbeatMessages)
                                observedContributionIds.Add(contribution.Id);
                            participantBootstrap = MultiModelCouncilServiceAppendPromptSection(
                                participantBootstrap,
                                "Queued direct user messages for the current Council heartbeat",
                                BuildHumanContributionBriefing(queuedHeartbeatMessages),
                                logger);
                            progressMessage?.Invoke(
                                $"Included {queuedHeartbeatMessages.Count} queued direct user heartbeat message(s) in {modelName}'s initial context without restarting the model.");
                        }
                    }

                    var messages = new List<ChatMessage>();
                    if (!string.IsNullOrWhiteSpace(participantBootstrap))
                        messages.Add(new ChatMessage(ChatRole.System, participantBootstrap));
                    messages.Add(new ChatMessage(ChatRole.System, councilText.MultiModelCouncilServiceCreateCouncilSystemPrompt(modelName, councilMembers, logger)));
                    messages.Add(new ChatMessage(ChatRole.User, prompt));

                    var allContent = new StringBuilder();
                    var finalAttemptContent = string.Empty;
                    var liveInputRestarts = 0;
                    const int maximumLiveInputRestarts = 12;

                    ArgumentNullException.ThrowIfNull(client);
                    ArgumentNullException.ThrowIfNull(messages);

                    while (true)
                    {
                        var streamId = Guid.NewGuid().ToString("N");
                        var streamPanelOpened = streamUpdate is not null;
                        var continuationLabel = liveInputRestarts == 0 ? "live output" : $"live continuation {liveInputRestarts}";
                        streamUpdate?.Invoke($"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} / {role} {continuationLabel}")}</summary>\n\n");
                        if (liveInputRestarts == 0)
                        {
                            streamUpdate?.Invoke(
                                $"> **Knowledge & capability state:** automatic/native tools are **{(enableAutomaticTools ? automaticFunctionAllowList is { Count: > 0 } ? $"restricted to {string.Join(", ", automaticFunctionAllowList)}" : "available when policy allows" : "disabled for this workflow step")}**. " +
                                "Local/project/role evidence already supplied in the prompt is passive context and does not create a function-call event. Any active DX/native function call is rendered in this same provider stream.\n\n");
                        }

                        var attemptBuilder = new StringBuilder();
                        var repetitionWatchdog = new ProviderStreamRepetitionWatchdog(logger);
                        using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(participantCts.Token);
                        using var monitorCts = CancellationTokenSource.CreateLinkedTokenSource(participantCts.Token);
                        var liveInputSignal = new TaskCompletionSource<IReadOnlyList<HumanCouncilContribution>>(
                            TaskCreationOptions.RunContinuationsAsynchronously);
                        var monitorTask = councilRunId is Guid monitoredRunId
                            ? MonitorLiveCouncilInputAsync(
                                monitoredRunId,
                                round,
                                BuildLiveInputConsumerKey(modelName, phase, role),
                                observedContributionIds,
                                liveInputSignal,
                                streamCts,
                                monitorCts.Token)
                            : Task.CompletedTask;

                        IReadOnlyList<HumanCouncilContribution>? liveContributions = null;
                        var streamCompletedWithoutLiveInput = false;
                        try
                        {
                            await foreach (var update in client.GetStreamingResponseAsync(
                                messages,
                                new ChatOptions
                                {
                                    MaxOutputTokens = Math.Clamp(maxOutputTokens, catalog.MinOutputTokens, catalog.MaxOutputTokens),
                                    Temperature = 0.2f
                                },
                                streamCts.Token).WithCancellation(streamCts.Token).ConfigureAwait(false))
                            {
                                streamUpdate?.Invoke(update.Text);
                                if (!councilRuntime.IsLocalGptStreamingStatusUpdate(update.Text, logger))
                                {
                                    attemptBuilder.Append(update.Text);
                                    allContent.Append(update.Text);
                                    var repetitionFailure = repetitionWatchdog.Observe(update.Text);
                                    if (repetitionFailure is not null)
                                    {
                                        progressMessage?.Invoke(
                                            $"Repetition watchdog stopped runaway generation from {modelName} during {phase}; existing member recovery will now handle this failed attempt.");
                                        streamUpdate?.Invoke(
                                            $"\n\n> **LocalGPT repetition watchdog:** sustained repeated generation was detected and only this provider request is being stopped. " +
                                            $"The partial stream remains evidence; configured same-member and round-member recovery remain authoritative. {WebUtility.HtmlEncode(repetitionFailure.Message)}\n\n");
                                        streamCts.Cancel();
                                        throw repetitionFailure;
                                    }
                                }

                                foreach (var providerTrace in councilRuntime.BuildUserVisibleProviderTrace(update, logger))
                                {
                                    streamUpdate?.Invoke(providerTrace);
                                    attemptBuilder.Append(providerTrace);
                                    allContent.Append(providerTrace);
                                }
                            }

                            // A user message can arrive after Ollama emitted its last token but before
                            // LocalGPT has finalized the participant. Give the synchronous service
                            // notification a short grace window and honor it instead of silently
                            // accepting the old answer.
                            if (!liveInputSignal.Task.IsCompleted)
                            {
                                await Task.WhenAny(
                                    liveInputSignal.Task,
                                    Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken.None))
                                    .ConfigureAwait(false);
                            }

                            if (liveInputSignal.Task.IsCompletedSuccessfully)
                                liveContributions = await liveInputSignal.Task.ConfigureAwait(false);
                            else
                                streamCompletedWithoutLiveInput = true;
                        }
                        catch (OperationCanceledException) when (
                            liveInputSignal.Task.IsCompletedSuccessfully &&
                            !participantCts.IsCancellationRequested &&
                            !cancellationToken.IsCancellationRequested)
                        {
                            liveContributions = await liveInputSignal.Task.ConfigureAwait(false);
                        }
                        finally
                        {
                            monitorCts.Cancel();
                            try
                            {
                                await monitorTask.ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (monitorCts.IsCancellationRequested || participantCts.IsCancellationRequested)
                            {
                            }

                            if (streamPanelOpened)
                                streamUpdate?.Invoke($"\n\n</details><!--localgpt-council-stream-complete:{streamId}-->\n\n");
                        }

                        if (streamCompletedWithoutLiveInput || liveContributions is null || liveContributions.Count == 0)
                        {
                            finalAttemptContent = attemptBuilder.ToString();
                            break;
                        }

                        foreach (var contribution in liveContributions)
                            observedContributionIds.Add(contribution.Id);

                        liveInputRestarts++;
                        if (liveInputRestarts > maximumLiveInputRestarts)
                        {
                            logger.LogWarning(
                                "Stopped live-input restarts for Council model {ModelName} after {RestartCount} interruptions in {Phase}.",
                                modelName,
                                maximumLiveInputRestarts,
                                phase);
                            finalAttemptContent = attemptBuilder.ToString();
                            break;
                        }

                        var partial = attemptBuilder.ToString();
                        if (!string.IsNullOrWhiteSpace(partial))
                        {
                            messages.Add(new ChatMessage(
                                ChatRole.Assistant,
                                "Partial response produced before the user interrupted this model:\n\n" +
                                LimitLiveCouncilContext(partial, 24_000)));
                        }

                        messages.Add(new ChatMessage(
                            ChatRole.User,
                            BuildLiveCouncilInterruptionPrompt(liveContributions)));

                        var deliveredMessageCount = liveContributions.Count;
                        streamUpdate?.Invoke(
                            $"> **Live user input delivered to {WebUtility.HtmlEncode(modelName)}.** " +
                            $"LocalGPT atomically assigned {deliveredMessageCount} new direct user message(s) to this active model, added them to its prompt and restarted only this model. " +
                            "The same message remains shared Council heartbeat context for later participants/rounds without restarting every active stream. " +
                            "The following continuation is generated with that input present.\n\n");
                        logger.LogInformation(
                            "Restarting Council model {ModelName} in phase {Phase} after receiving {ContributionCount} live user message(s).",
                            modelName,
                            phase,
                            liveContributions.Count);
                    }

                    var content = allContent.ToString();
                    var thinking = councilText.MultiModelCouncilServiceExtractThinking(content, logger);
                    var visibleContent = councilText.MultiModelCouncilServiceStripThinking(
                        string.IsNullOrWhiteSpace(finalAttemptContent) ? content : finalAttemptContent,
                        logger);
                    if (string.IsNullOrWhiteSpace(visibleContent) && !string.IsNullOrWhiteSpace(thinking))
                        visibleContent = $"_{modelName} returned thinking during {phase}, but no final visible answer. Increase max output tokens or ask for a shorter final answer._";

                    visibleContent = await modelSelfAssessment
                        .CaptureAndStripAsync(modelName, visibleContent, participantCts.Token)
                        .ConfigureAwait(false);

                    string? finalAnswerError = null;
                    var roleComplianceFailureDetected = MultiModelCouncilServiceLooksLikeGenericRoleRefusal(visibleContent, logger);
                    var roleComplianceSucceeded = !roleComplianceFailureDetected;
                    var remainingRoleComplianceRetries = Math.Clamp(roleComplianceRetryCount, 0, 3);
                    while (remainingRoleComplianceRetries > 0 && !roleComplianceSucceeded)
                    {
                        var retryNumber = Math.Clamp(roleComplianceRetryCount, 0, 3) - remainingRoleComplianceRetries + 1;
                        progressMessage?.Invoke($"{modelName} did not perform its assigned {role} task. LocalGPT is issuing corrective role retry {retryNumber}/{Math.Clamp(roleComplianceRetryCount, 0, 3)} to the same member and role.");
                        streamUpdate?.Invoke($"<p class=\"localgpt-stream-status\"><em>LocalGPT detected generic role non-performance. Corrective same-member retry {retryNumber}/{Math.Clamp(roleComplianceRetryCount, 0, 3)} is starting; this is additional model work, not delayed UI rendering.</em></p>\n\n");
                        var complianceRecovery = await MultiModelCouncilServiceRunRoleComplianceRecoveryAsync(
                            client,
                            modelName,
                            phase,
                            role,
                            messages,
                            Math.Clamp(Math.Max(maxOutputTokens, catalog.MinOutputTokens), catalog.MinOutputTokens, catalog.MaxOutputTokens),
                            streamUpdate,
                            participantCts.Token,
                            logger).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(complianceRecovery.Content))
                            content = $"{content}{Environment.NewLine}{Environment.NewLine}{complianceRecovery.Content}";
                        if (!string.IsNullOrWhiteSpace(complianceRecovery.Thinking))
                            thinking = string.Join(Environment.NewLine, new[] { thinking, complianceRecovery.Thinking }.Where(text => !string.IsNullOrWhiteSpace(text)));
                        if (MultiModelCouncilServiceIsSubstantiveCouncilContent(complianceRecovery.VisibleContent, logger) &&
                            !MultiModelCouncilServiceLooksLikeGenericRoleRefusal(complianceRecovery.VisibleContent, logger))
                        {
                            visibleContent = complianceRecovery.VisibleContent;
                            roleComplianceSucceeded = true;
                            break;
                        }

                        visibleContent = complianceRecovery.VisibleContent;
                        remainingRoleComplianceRetries--;
                    }

                    if (roleComplianceFailureDetected && !roleComplianceSucceeded)
                    {
                        var configuredRetryCount = Math.Clamp(roleComplianceRetryCount, 0, 3);
                        finalAnswerError = configuredRetryCount == 0
                            ? $"{modelName} declined or ignored its assigned {role} task and role-compliance retry is disabled for this workflow step."
                            : $"{modelName} declined or ignored its assigned {role} task after {configuredRetryCount} configured corrective retry attempt(s).";
                        visibleContent = $"_{finalAnswerError}_";
                        logger.LogWarning("Council model {ModelName} failed role compliance for {Role} in phase {Phase} after {RetryCount} configured retry attempt(s).", modelName, role, phase, configuredRetryCount);
                    }

                    if (finalAnswerError is null && finalAnswerRecoveryEnabled && MultiModelCouncilServiceIsThinkingOnlyCouncilContent(visibleContent, logger))
                    {
                        progressMessage?.Invoke($"{modelName} returned provider thinking/non-final content without a substantive final answer. LocalGPT is starting the configured final-answer recovery pass.");
                        streamUpdate?.Invoke("<p class=\"localgpt-stream-status\"><em>Provider stream ended without a substantive final answer. The configured final-answer recovery pass is starting now; this is additional model work and does not wait for frontend rendering.</em></p>\n\n");
                        var recovery = await MultiModelCouncilServiceRunFinalOnlyRecoveryAsync(
                            client,
                            modelName,
                            phase,
                            messages,
                            Math.Clamp(Math.Min(Math.Max(maxOutputTokens, 128), Math.Clamp(finalAnswerRecoveryMaxOutputTokens, 128, 32768)), catalog.MinOutputTokens, catalog.MaxOutputTokens),
                            streamUpdate,
                            participantCts.Token,
                            logger).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(recovery.Content))
                            content = $"{content}{Environment.NewLine}{Environment.NewLine}{recovery.Content}";
                        if (!string.IsNullOrWhiteSpace(recovery.Thinking))
                            thinking = string.Join(Environment.NewLine, new[] { thinking, recovery.Thinking }.Where(text => !string.IsNullOrWhiteSpace(text)));
                        if (MultiModelCouncilServiceIsSubstantiveCouncilContent(recovery.VisibleContent, logger))
                        {
                            visibleContent = recovery.VisibleContent;
                        }
                        else
                        {
                            finalAnswerError = $"{modelName} did not emit a substantive final answer during {phase}, including the bounded final-answer recovery.";
                            visibleContent = $"_{finalAnswerError}_";
                            logger.LogWarning(
                                "Council model {ModelName} did not emit a substantive final answer during {Phase} after bounded recovery.",
                                modelName,
                                phase);
                        }
                    }

                    if (finalAnswerError is null && !finalAnswerRecoveryEnabled && MultiModelCouncilServiceIsThinkingOnlyCouncilContent(visibleContent, logger))
                    {
                        finalAnswerError = $"{modelName} did not emit a substantive final answer during {phase}; final-answer recovery is disabled for this workflow step.";
                        visibleContent = $"_{finalAnswerError}_";
                    }

                    stopwatch.Stop();
                    var completedStep = new MultiModelCouncilStep
                    {
                        Round = round,
                        Phase = phase,
                        ModelName = modelName,
                        ProviderName = providerModel.ProviderName,
                        ProviderEndpoint = providerModel.Endpoint,
                        ProviderModelName = providerModel.ModelName,
                        CouncilMembers = councilMembers.ToList(),
                        Role = role,
                        Content = content,
                        VisibleContent = visibleContent,
                        Thinking = thinking,
                        StartedAtUtc = started,
                        CompletedAtUtc = DateTime.UtcNow,
                        DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                        Error = finalAnswerError
                    };
                    ApplyHardwarePlan(completedStep, executionPlan);
                    return completedStep;
                }
                catch (OperationCanceledException) when (
                    roundSkipToken.IsCancellationRequested &&
                    !cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Council participant {ModelName} stopped because the user skipped round {Round}, phase {Phase}.",
                        modelName,
                        round,
                        phase);
                    return CreateRoundSkippedStep(modelName, councilMembers, round, phase, role, executionPlan);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    stopwatch.Stop();
                    var message = $"{modelName} exceeded the {modelTimeoutSeconds}s council timeout during {phase}.";
                    logger.LogWarning("{Message}", message);
                    runtimeLease?.Dispose();
                    runtimeLease = null;
                    if (allowRecovery)
                    {
                        var recovered = await RetryParticipantWithSafeLimitsAsync(
                            baseUri, modelName, councilMembers, round, phase, role, prompt, bootstrap,
                            maxOutputTokens, keepAlive, maxContextTokens, modelTimeoutSeconds,
                            streamUpdate, cancellationToken, message).ConfigureAwait(false);
                        if (recovered is not null)
                            return recovered;
                    }
                    var timeoutStep = new MultiModelCouncilStep
                    {
                        Round = round,
                        Phase = phase,
                        ModelName = modelName,
                        ProviderName = providerModel.ProviderName,
                        ProviderEndpoint = providerModel.Endpoint,
                        ProviderModelName = providerModel.ModelName,
                        CouncilMembers = councilMembers.ToList(),
                        Role = role,
                        Content = $"**{message}**",
                        VisibleContent = $"**{message}**",
                        StartedAtUtc = started,
                        CompletedAtUtc = DateTime.UtcNow,
                        DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                        Error = message
                    };
                    ApplyHardwarePlan(timeoutStep, executionPlan);
                    return timeoutStep;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    logger.LogWarning(ex, "Council participant {ModelName} failed in {Phase}.", modelName, phase);
                    runtimeLease?.Dispose();
                    runtimeLease = null;
                    if (allowRecovery)
                    {
                        var recovered = await RetryParticipantWithSafeLimitsAsync(
                            baseUri, modelName, councilMembers, round, phase, role, prompt, bootstrap,
                            maxOutputTokens, keepAlive, maxContextTokens, modelTimeoutSeconds,
                            streamUpdate, cancellationToken, ex.Message).ConfigureAwait(false);
                        if (recovered is not null)
                            return recovered;
                    }
                    var failedStep = new MultiModelCouncilStep
                    {
                        Round = round,
                        Phase = phase,
                        ModelName = modelName,
                        ProviderName = providerModel.ProviderName,
                        ProviderEndpoint = providerModel.Endpoint,
                        ProviderModelName = providerModel.ModelName,
                        CouncilMembers = councilMembers.ToList(),
                        Role = role,
                        Content = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                        VisibleContent = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                        StartedAtUtc = started,
                        CompletedAtUtc = DateTime.UtcNow,
                        DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                        Error = ex.Message
                    };
                    ApplyHardwarePlan(failedStep, executionPlan);
                    return failedStep;
                }
                finally
                {
                    runtimeLease?.Dispose();
                    if (participantRequestStarted
                        && providerModel.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                        && MultiModelCouncilServiceShouldUnloadAfterParticipant(keepAlive, logger))
                    {
                        // The originating Ollama request already carries keep_alive=0s. Do not issue a second
                        // blocking unload HTTP request on the host-queue critical path after the model has finished.
                        logger.LogDebug("Ollama model {ModelName} completed with keep_alive={KeepAlive}; the runtime request itself owns unload semantics.", providerModel.ModelName, keepAlive);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council participant failed for model {ModelName}, round {Round}, phase {Phase}, role {Role}, max output {MaxOutputTokens}, max context {MaxContextTokens}, timeout {TimeoutSeconds}s.", modelName, round, phase, role, maxOutputTokens, maxContextTokens, modelTimeoutSeconds);
                var failedStep = new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    CouncilMembers = councilMembers.ToList(),
                    Role = role,
                    Content = $"**{modelName} failed before its {phase} request could complete.**{Environment.NewLine}{ex.Message}",
                    VisibleContent = $"**{modelName} failed before its {phase} request could complete.**{Environment.NewLine}{ex.Message}",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = 0,
                    Error = ex.Message
                };
                // Provider resolution failed before a trustworthy transport identity existed. Do not
                // mislabel an unknown/cloud route as an Ollama CPU road merely because the global legacy
                // Ollama override was zero.
                ApplyHardwarePlan(failedStep, fallbackPlan ?? new CouncilHardwareRoadPlan(
                    modelName,
                    OneWireHardwareKind.Auto,
                    -1,
                    "Automatic provider route",
                    $"auto:{modelName}",
                    100,
                    maxOutputTokens,
                    maxContextTokens,
                    null,
                    1));
                return failedStep;
            }
        }

        /// <summary>
        /// Performs monitor live council input as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
        /// <param name="currentRound">Current round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="consumerKey">Consumer key value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="observedContributionIds">Guid dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="signal">Signal value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="streamCancellation">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task MonitorLiveCouncilInputAsync(
            Guid councilRunId,
            int currentRound,
            string consumerKey,
            IReadOnlySet<Guid> observedContributionIds,
            TaskCompletionSource<IReadOnlyList<HumanCouncilContribution>> signal,
            CancellationTokenSource streamCancellation,
            CancellationToken cancellationToken)
        {
            void Deliver(HumanCouncilContribution contribution)
            {
                if (contribution.CouncilRunId != councilRunId ||
                    contribution.EarliestCouncilRound > currentRound ||
                    observedContributionIds.Contains(contribution.Id))
                {
                    return;
                }

                // Direct user input is shared Council heartbeat context, but exactly one currently
                // running model may claim the immediate interrupt/restart. Without this atomic
                // claim every parallel participant subscribed to the same event and restarted.
                if (!humanCollaboration.TryClaimDirectUserMessage(contribution.Id, councilRunId, consumerKey))
                    return;

                if (signal.TrySetResult([contribution]))
                    streamCancellation.Cancel();
            }

            humanCollaboration.DirectUserMessageQueued += Deliver;
            try
            {
                // Catch a message persisted immediately before this model subscribed. This is
                // the only database read performed by the active-stream monitor; subsequent
                // delivery is event-driven and does not compete with Ollama or Blazor rendering.
                var queued = await humanCollaboration
                    .ReadQueuedContributionsAsync(councilRunId, currentRound, cancellationToken)
                    .ConfigureAwait(false);
                var claimedDirectMessage = queued
                    .Where(item =>
                        item.HumanRole.Equals("Direct user message", StringComparison.OrdinalIgnoreCase) &&
                        !observedContributionIds.Contains(item.Id))
                    .FirstOrDefault(item =>
                        humanCollaboration.TryClaimDirectUserMessage(item.Id, councilRunId, consumerKey));
                if (claimedDirectMessage is not null)
                {
                    if (signal.TrySetResult([claimedDirectMessage]))
                        streamCancellation.Cancel();
                    return;
                }

                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not monitor live Council user messages for run {CouncilRunId}; the active model stream will continue.", councilRunId);
            }
            finally
            {
                humanCollaboration.DirectUserMessageQueued -= Deliver;
            }
        }

    }
}
