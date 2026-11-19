using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Collections.Concurrent;
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
        /// Performs run phase as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="promptFactory">Prompt factory value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxOutputTokens">Max output tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxParallelModels">Max parallel models value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="progressMessage">Progress message value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="stepCompleted">Step completed value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="allowParallelHardwareRoads">Value indicating whether allow parallel hardware roads should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="allowDxFunctions">Value indicating whether allow DevExpress functions should apply to this operation.</param>
        /// <param name="councilMembers">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="sequentialPerHost">Value indicating whether sequential per host should apply to this operation.</param>
        /// <param name="automaticFunctionAllowList">Optional exact automatic-function names permitted for this workflow step.</param>
        /// <param name="roleComplianceRetryCount">Number of bounded same-member corrective retries permitted when a role member refuses or ignores its assigned work.</param>
        /// <param name="finalAnswerRecoveryEnabled">Whether the workflow may issue a separate provider turn to recover a missing final answer.</param>
        /// <param name="finalAnswerRecoveryMaxOutputTokens">Maximum output-token budget for one configured final-answer recovery turn.</param>
        /// <param name="toolResultContinuationMode">Policy controlling whether text-gateway function evidence is returned to the same member.</param>
        /// <param name="toolResultContinuationRounds">Maximum bounded same-member function-result continuation turns.</param>
        /// <param name="strictAutomaticFunctions">Whether the current configured step selected an exclusive tool list, independently of function permissions.</param>
        /// <param name="allowStartupHostTakeover">Whether the configured, unpaired role permits a different selected provider-qualified member to take over a request that never received response headers.</param>
        /// <param name="exactAssignments">Preserves every explicitly reserved route rather than dropping one of its unique file work orders.</param>
        /// <param name="requestOutputCeiling">Optional run-local task output ceiling preserved across live route acquisition.</param>
        /// <param name="onParticipantCompletedAsync">Optional owned persistence callback for an individual completed assignment, before the phase barrier.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task RunPhaseAsync(
            MultiModelCouncilResult result,
            string baseUri,
            IReadOnlyList<string> participants,
            int round,
            string phase,
            string role,
            Func<string, string> promptFactory,
            string bootstrap,
            int maxOutputTokens,
            int maxParallelModels,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            Action<string>? progressMessage,
            Action<string>? streamUpdate,
            Action<MultiModelCouncilStep>? stepCompleted,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            bool allowParallelHardwareRoads,
            CancellationToken cancellationToken,
            bool allowDxFunctions = true,
            IReadOnlyList<string>? councilMembers = null,
            bool sequentialPerHost = false,
            IReadOnlyCollection<string>? automaticFunctionAllowList = null,
            int roleComplianceRetryCount = 1,
            bool finalAnswerRecoveryEnabled = true,
            int finalAnswerRecoveryMaxOutputTokens = 8192,
            CouncilToolResultContinuationMode toolResultContinuationMode = CouncilToolResultContinuationMode.Disabled,
            int toolResultContinuationRounds = 0,
            bool strictAutomaticFunctions = false,
            bool allowStartupHostTakeover = false,
            bool exactAssignments = false,
            Func<MultiModelCouncilStep, Task>? onParticipantCompletedAsync = null,
            int? requestOutputCeiling = null)
        {
            try
            {
                using var councilScope = ambientContext.PushCouncil(result.RunId, round, phase);
                var runConfiguration = runConfigurations.Get(result.RunId);
                if (runConfiguration is { IsRunning: true })
                {
                    allowParallelHardwareRoads = runConfiguration.AllowParallelHardwareRoads;
                    maxParallelModels = Math.Max(1, runConfiguration.MaxParallelModels);
                    modelTimeoutSeconds = Math.Clamp(runConfiguration.ModelTimeoutSeconds, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson").MinimumModelTimeoutSeconds, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson").MaximumModelTimeoutSeconds);
                }
                var failedModels = result.Steps
                    .Where(step => !string.IsNullOrWhiteSpace(step.Error) && !step.IsContextBudgetFailure)
                    .Select(step => step.ModelName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var phaseParticipants = OrderParticipantsByObservedHealth(
                    result,
                    participants.Where(model => exactAssignments || !failedModels.Contains(model))).ToList();
                if (phaseParticipants.Count == 0)
                    phaseParticipants.Add(SelectHealthyParticipant(result, participants));
                if (phaseParticipants.Count < participants.Count)
                {
                    var excluded = participants.Where(model => !phaseParticipants.Contains(model, StringComparer.OrdinalIgnoreCase));
                    progressMessage?.Invoke($"Council recovery omitted previously failed provider routes from {phase}: {string.Join(", ", excluded)}. The decision is scoped to this run.");
                }
                var hostCount = phaseParticipants
                    .Select(GetCouncilExecutionHostKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                var concurrencyDescription = sequentialPerHost
                    ? "one model at a time on each AI host, with AI hosts running in parallel"
                    : allowParallelHardwareRoads
                        ? $"up to {maxParallelModels} model request(s) per AI host"
                        : "one model request per AI host; additional hardware-road parallelism is disabled";
                progressMessage?.Invoke(
                    $"Starting council phase: round {round}, {phase}, role {role}; {phaseParticipants.Count} member(s) across {hostCount} AI host(s), {concurrencyDescription}.");

                // AI hosts are independent compute machines and are never collapsed into one global gate.
                // AllowParallelHardwareRoads controls additional concurrency inside each host. The dedicated
                // sequential-per-host workflow mode keeps one deterministic queue per host while all host
                // queues advance concurrently. DXAIChat still presents one complete member stream at a time
                // so provider thinking/tool markup cannot be interleaved into another member's text.
                var hostGates = new Dictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
                foreach (var modelName in phaseParticipants)
                {
                    var hostKey = GetCouncilExecutionHostKey(modelName);
                    if (!hostGates.ContainsKey(hostKey))
                    {
                        var capacity = !sequentialPerHost && allowParallelHardwareRoads ? maxParallelModels : 1;
                        hostGates[hostKey] = new SemaphoreSlim(capacity, capacity);
                    }
                }

                var participantStreams = streamUpdate is null
                    ? null
                    : phaseParticipants.ToDictionary(
                        modelName => modelName,
                        _ => Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                        {
                            SingleReader = true,
                            SingleWriter = true,
                            AllowSynchronousContinuations = false
                        }),
                        StringComparer.OrdinalIgnoreCase);
                // Parallel lanes transport their own live fragments. Publish closed streams in completion
                // order so an earlier slow host cannot block the shared chat. A serial phase retains
                // its foreground stream and incremental presentation.
                var publishCompletedFirst = hostCount > 1 ||
                    (!sequentialPerHost && allowParallelHardwareRoads && maxParallelModels > 1);
                var presentationOrder = participantStreams is null
                    ? null
                    : Channel.CreateUnbounded<string>(new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = !publishCompletedFirst,
                        AllowSynchronousContinuations = false
                    });
                if (presentationOrder is not null && !publishCompletedFirst)
                {
                    foreach (var modelName in phaseParticipants)
                        presentationOrder.Writer.TryWrite(modelName);
                    presentationOrder.Writer.TryComplete();
                }

                // Publish every selected member to the live board before host execution starts.
                // This makes remote/queued members visible immediately and keeps every provider-qualified
                // Council member equivalent even when an earlier ordered stream is still being presented.
                foreach (var modelName in phaseParticipants)
                {
                    var plannedRoad = modelRoutes.TryGetValue(modelName, out var configuredPlan)
                        ? configuredPlan
                        : new CouncilHardwareRoadPlan(modelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{modelName}", 100, maxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                    var queuedActivityKey = BuildCouncilParticipantActivityKey(result.RunId, round, phase, role, modelName);
                    var queuedRouteLabel = $"{GetCouncilExecutionHostKey(modelName)} · {plannedRoad.LaneKey}";
                    liveCouncilSessions.BeginParticipantActivity((result.ChatRunId ?? result.RunId), queuedActivityKey, modelName, phase, role, queuedRouteLabel);
                    liveCouncilSessions.SetParticipantActivityStatus((result.ChatRunId ?? result.RunId), queuedActivityKey, $"Queued for {queuedRouteLabel}; waiting for this member's one Council turn.");
                }

                var participantBootstrap = bootstrap;
                if (streamUpdate is not null)
                {
                    participantBootstrap = await PrepareLiveHumanInputAsync(
                        result,
                        round,
                        phase,
                        participantBootstrap,
                        progressMessage,
                        stepCompleted,
                        cancellationToken).ConfigureAwait(false);
                }

                var roundSkipToken = runConfigurations.GetRoundCancellationToken(result.RunId, round, phase);
                var presentationTask = participantStreams is null
                    ? Task.CompletedTask
                    : PumpCouncilParticipantStreamsAsync(
                        result.RunId,
                        phase,
                        role,
                        presentationOrder!.Reader,
                        participantStreams,
                        streamUpdate!,
                        publishCompletedFirst);
                // Phase workers publish only to the concurrent completion queues; the coordinator
                // remains the sole writer of result.Steps and of ordered DX function dispatch.
                var completedPrimarySteps = new ConcurrentQueue<MultiModelCouncilStep>();
                var completedTakeoverSteps = new ConcurrentQueue<MultiModelCouncilStep>();
                var hostKeys = phaseParticipants.ToDictionary(member => member, GetCouncilExecutionHostKey, StringComparer.OrdinalIgnoreCase);
                var previousPhaseSteps = result.Steps.ToArray();
                try
                {
                    async Task<MultiModelCouncilStep> ExecuteParticipantAsync(string modelName, SemaphoreSlim? hostGate)
                    {
                        var fallbackPlan = modelRoutes.TryGetValue(modelName, out var configuredPlan)
                            ? configuredPlan
                            : new CouncilHardwareRoadPlan(modelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{modelName}", 100, maxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                        var gateAcquired = false;
                        var participantStream = participantStreams is null ? null : participantStreams[modelName];
                        var activityKey = BuildCouncilParticipantActivityKey(result.RunId, round, phase, role, modelName);
                        var routeLabel = $"{GetCouncilExecutionHostKey(modelName)} · {fallbackPlan.LaneKey}";
                        Action<string>? participantStreamUpdate = participantStream is null
                            ? null
                            : text =>
                            {
                                if (!string.IsNullOrEmpty(text))
                                {
                                    participantStream.Writer.TryWrite(text);
                                    // Each lane remains live while the shared transcript presents intact blocks.
                                    liveCouncilSessions.AppendParticipantActivity((result.ChatRunId ?? result.RunId), activityKey, text);
                                }
                            };
                        try
                        {
                            if (hostGate is not null)
                            {
                                using var gateCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, roundSkipToken);
                                liveCouncilSessions.SetParticipantActivityStatus((result.ChatRunId ?? result.RunId), activityKey, $"Waiting for AI host road {routeLabel}.");
                                await hostGate.WaitAsync(gateCancellation.Token).ConfigureAwait(false);
                                gateAcquired = true;
                            }

                            liveCouncilSessions.SetParticipantActivityStatus((result.ChatRunId ?? result.RunId), activityKey, $"Running on {routeLabel}.");
                            var participantPrompt = promptFactory(modelName);
                            if (exactAssignments)
                                participantStreamUpdate?.Invoke("\n\n<details class=\"council-step\"><summary>" + WebUtility.HtmlEncode($"{modelName} — {phase} / {role} · {runtimePolicy.GetString("CouncilFileWorkInstructionLabel")}") +
                                    "</summary>\n\n<pre><code>" + WebUtility.HtmlEncode(participantPrompt) + "</code></pre>\n\n</details>\n\n");
                            var step = await RunParticipantAsync(
                                baseUri, modelName, councilMembers ?? participants, round, phase, role, participantPrompt, participantBootstrap,
                                fallbackPlan.EffectiveMaxOutputTokens, keepAlive, fallbackPlan.OllamaNumGpu, fallbackPlan.EffectiveMaxContextTokens,
                                modelTimeoutSeconds, participantStreamUpdate, cancellationToken,
                                fallbackPlan: fallbackPlan,
                                progressMessage: progressMessage,
                                enableAutomaticTools: allowDxFunctions,
                                automaticFunctionAllowList: automaticFunctionAllowList,
                                roleComplianceRetryCount: roleComplianceRetryCount,
                                finalAnswerRecoveryEnabled: finalAnswerRecoveryEnabled,
                                finalAnswerRecoveryMaxOutputTokens: finalAnswerRecoveryMaxOutputTokens,
                                requestOutputCeiling: requestOutputCeiling).ConfigureAwait(false);
                            ArgumentNullException.ThrowIfNull(step);
                            if (exactAssignments)
                                step.FileWorkInstruction = participantPrompt;
                            if (onParticipantCompletedAsync is not null)
                                await onParticipantCompletedAsync(step).ConfigureAwait(false);
                            liveCouncilSessions.SetParticipantActivityResult(
                                (result.ChatRunId ?? result.RunId),
                                activityKey,
                                step.VisibleContent);
                            liveCouncilSessions.CompleteParticipantActivity(
                                (result.ChatRunId ?? result.RunId),
                                activityKey,
                                string.IsNullOrWhiteSpace(step.Error)
                                    ? "Model completed. Its live result is available in this lane and queued for transcript integration."
                                    : step.IsProviderStartupTimeout ? step.VisibleContent : $"Model completed with an error: {step.Error}");
                            completedPrimarySteps.Enqueue(step);

                            // Only a first-response timeout is safe to hand off without reconciling
                            // tool receipts. Preserve the failed provider and the replacement as
                            // separate evidence; never change the identity of the original step.
                            if (allowStartupHostTakeover && publishCompletedFirst && step.IsProviderStartupTimeout &&
                                !roundSkipToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                            {
                                if (gateAcquired)
                                {
                                    hostGate!.Release();
                                    gateAcquired = false;
                                }
                                try
                                {
                                    var finished = completedPrimarySteps.ToArray();
                                    var failedHost = hostKeys[modelName];
                                    var providerIdentity = new ProviderModelIdentity();
                                    if (providerIdentity.TryParseSelectionKey(modelName, out var failedReference))
                                    {
                                        // Only candidates whose original phase work is done may volunteer.
                                        // Prefer the quickest observed successful copy of the same model,
                                        // not a hard-coded main computer or a parameter-count ranking.
                                        var candidates = finished.Where(candidate =>
                                            IsUsableConfiguredRoundStep(candidate) && candidate.DurationSeconds > 0 &&
                                            !candidate.VisibleContent.Contains("was disabled for this running Council session", StringComparison.OrdinalIgnoreCase) &&
                                            hostKeys.TryGetValue(candidate.ModelName, out var candidateHost) &&
                                            !string.Equals(candidateHost, failedHost, StringComparison.OrdinalIgnoreCase) &&
                                            modelRoutes.ContainsKey(candidate.ModelName) &&
                                            providerIdentity.TryParseSelectionKey(candidate.ModelName, out var candidateReference) &&
                                            string.Equals(candidateReference.ProviderKind, failedReference.ProviderKind, StringComparison.OrdinalIgnoreCase) &&
                                            string.Equals(candidateReference.ModelName, failedReference.ModelName, StringComparison.OrdinalIgnoreCase) &&
                                            phaseParticipants.Where(member => string.Equals(hostKeys[member], candidateHost, StringComparison.OrdinalIgnoreCase))
                                                .All(member => finished.Any(done => string.Equals(done.ModelName, member, StringComparison.OrdinalIgnoreCase))))
                                            .OrderBy(candidate => candidate.DurationSeconds)
                                            .Select(candidate => candidate.ModelName)
                                            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                        if (candidates.Count > 0)
                                        {
                                            var decision = await goalRecovery.DecideMemberRecoveryAsync(new GoalMemberRecoveryContext
                                            {
                                                RunId = result.RunId,
                                                TeamKey = result.CouncilTeamKey,
                                                WorkflowStepKey = phase,
                                                RoleName = role,
                                                Round = round,
                                                FailedModel = modelName,
                                                FailureKind = GoalMemberFailureKind.ProviderStartupTimeout,
                                                RecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool,
                                                AttemptNumber = 1,
                                                RunParticipants = candidates.Append(modelName).ToList(),
                                                RolePool = candidates,
                                                AttemptedModels = [modelName],
                                                HostKeys = hostKeys,
                                                RunSteps = previousPhaseSteps.Concat(finished).Concat(completedTakeoverSteps.ToArray()).ToList()
                                            }, cancellationToken).ConfigureAwait(false);
                                            var replacementModel = decision.ModelName;
                                            if (decision.Action == GoalMemberRecoveryAction.UseOtherMember && candidates.Contains(replacementModel, StringComparer.OrdinalIgnoreCase))
                                            {
                                                var replacementGate = hostGates[hostKeys[replacementModel]];
                                                // Never wait for a second host while holding the primary gate.
                                                // If another handoff acquired it, normal configured recovery can decide later.
                                                var takeoverPhase = $"{phase} · automatic member recovery startup takeover {phaseParticipants.IndexOf(modelName) + 1}";
                                                var replacementPlan = modelRoutes[replacementModel];
                                                var replacementActivityKey = BuildCouncilParticipantActivityKey(result.RunId, round, takeoverPhase, role, replacementModel);
                                                var chatRunId = result.ChatRunId ?? result.RunId;
                                                var notice = runtimePolicy.GetString("CouncilProviderStartupTakeoverNotice")
                                                    .Replace("{Model}", modelName, StringComparison.Ordinal)
                                                    .Replace("{Candidate}", replacementModel, StringComparison.Ordinal);
                                                if (await replacementGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                                                {
                                                    try
                                                    {
                                                        using var takeoverCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, roundSkipToken);
                                                        takeoverCancellation.Token.ThrowIfCancellationRequested();
                                                        using var takeoverScope = ambientContext.PushCouncil(result.RunId, round, takeoverPhase);
                                                        liveCouncilSessions.BeginParticipantActivity(chatRunId, replacementActivityKey, replacementModel, takeoverPhase, role, $"{hostKeys[replacementModel]} · {replacementPlan.LaneKey}");
                                                        liveCouncilSessions.SetParticipantActivityStatus(chatRunId, replacementActivityKey, notice);
                                                        progressMessage?.Invoke(notice);
                                                        var takeoverNotice = Environment.NewLine + Environment.NewLine + "> " + WebUtility.HtmlEncode(notice) + Environment.NewLine + Environment.NewLine;
                                                        participantStream?.Writer.TryWrite(takeoverNotice);
                                                        liveCouncilSessions.AppendParticipantActivity(chatRunId, replacementActivityKey, takeoverNotice);
                                                        Action<string> replacementStreamUpdate = text =>
                                                        {
                                                            if (string.IsNullOrEmpty(text))
                                                                return;
                                                            participantStream?.Writer.TryWrite(text);
                                                            liveCouncilSessions.AppendParticipantActivity(chatRunId, replacementActivityKey, text);
                                                        };
                                                        var replacement = await RunParticipantAsync(
                                                            baseUri, replacementModel, councilMembers ?? participants, round, takeoverPhase, role,
                                                            promptFactory(replacementModel), participantBootstrap, replacementPlan.EffectiveMaxOutputTokens,
                                                            keepAlive, replacementPlan.OllamaNumGpu, replacementPlan.EffectiveMaxContextTokens,
                                                            modelTimeoutSeconds, replacementStreamUpdate, takeoverCancellation.Token,
                                                            allowRecovery: false, fallbackPlan: replacementPlan, progressMessage: progressMessage,
                                                            enableAutomaticTools: allowDxFunctions, automaticFunctionAllowList: automaticFunctionAllowList,
                                                            roleComplianceRetryCount: roleComplianceRetryCount, finalAnswerRecoveryEnabled: finalAnswerRecoveryEnabled,
                                                            finalAnswerRecoveryMaxOutputTokens: finalAnswerRecoveryMaxOutputTokens,
                                requestOutputCeiling: requestOutputCeiling).ConfigureAwait(false);
                                                        ArgumentNullException.ThrowIfNull(replacement);
                                                        replacement.RecoveryTargetModelName = modelName;
                                                        if (replacement.VisibleContent.Contains("was disabled for this running Council session", StringComparison.OrdinalIgnoreCase))
                                                            replacement.Error = $"CouncilRouteDisabled;{replacement.VisibleContent}";
                                                        completedTakeoverSteps.Enqueue(replacement);
                                                        liveCouncilSessions.SetParticipantActivityResult(chatRunId, replacementActivityKey, replacement.VisibleContent);
                                                        liveCouncilSessions.CompleteParticipantActivity(chatRunId, replacementActivityKey,
                                                            IsUsableConfiguredRoundStep(replacement) ? notice : replacement.Error ?? notice);
                                                    }
                                                    catch (OperationCanceledException)
                                                    {
                                                        liveCouncilSessions.CompleteParticipantActivity(chatRunId, replacementActivityKey,
                                                            runtimePolicy.GetString("CouncilPhaseCancellationPreservedNotice"));
                                                        if (cancellationToken.IsCancellationRequested)
                                                            throw;
                                                        if (!roundSkipToken.IsCancellationRequested)
                                                            throw;
                                                        var skippedReplacement = CreateRoundSkippedStep(replacementModel, councilMembers ?? participants, round, takeoverPhase, role, replacementPlan);
                                                        skippedReplacement.RecoveryTargetModelName = modelName;
                                                        completedTakeoverSteps.Enqueue(skippedReplacement);
                                                    }
                                                    catch (Exception exception)
                                                    {
                                                        // A replacement might already have invoked a native function.
                                                        // Record the actual failure and let the configured round retain it
                                                        // rather than starting another potentially consequential attempt.
                                                        logger.LogError(exception, "Council startup handoff to {Replacement} for {Original} failed.", replacementModel, modelName);
                                                        var now = DateTime.UtcNow;
                                                        var replacementFailure = new MultiModelCouncilStep
                                                        {
                                                            ModelName = replacementModel, RecoveryTargetModelName = modelName,
                                                            Round = round, Phase = takeoverPhase, Role = role,
                                                            CouncilMembers = (councilMembers ?? participants).ToList(),
                                                            Error = exception.Message, StartedAtUtc = now, CompletedAtUtc = now
                                                        };
                                                        ApplyHardwarePlan(replacementFailure, replacementPlan);
                                                        completedTakeoverSteps.Enqueue(replacementFailure);
                                                        liveCouncilSessions.CompleteParticipantActivity(chatRunId, replacementActivityKey, exception.Message);
                                                    }
                                                    finally
                                                    {
                                                        replacementGate.Release();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                                {
                                    throw;
                                }
                                catch (Exception exception)
                                {
                                    logger.LogWarning(exception, "Council startup handoff could not be scheduled for {Model}; normal configured recovery retains the original failure.", modelName);
                                }
                            }
                            return step;
                        }
                        catch (OperationCanceledException) when (
                            roundSkipToken.IsCancellationRequested &&
                            !cancellationToken.IsCancellationRequested)
                        {
                            liveCouncilSessions.CompleteParticipantActivity((result.ChatRunId ?? result.RunId), activityKey, "Participant was skipped because the current Council phase was cancelled.");
                            var skippedStep = CreateRoundSkippedStep(
                                modelName,
                                councilMembers ?? participants,
                                round,
                                phase,
                                role,
                                fallbackPlan);
                            completedPrimarySteps.Enqueue(skippedStep);
                            return skippedStep;
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            liveCouncilSessions.CompleteParticipantActivity(
                                (result.ChatRunId ?? result.RunId),
                                activityKey,
                                "Participant stopped because the Council run was cancelled by its caller.");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "Council participant infrastructure failed for {ModelName} in round {Round}, phase {Phase}. The failure is converted into explicit step evidence so the host queue and configured round recovery can continue.",
                                modelName,
                                round,
                                phase);
                            liveCouncilSessions.CompleteParticipantActivity(
                                (result.ChatRunId ?? result.RunId),
                                activityKey,
                                $"Participant infrastructure failed: {ex.Message}");
                            var failedAtUtc = DateTime.UtcNow;
                            var failedStep = new MultiModelCouncilStep
                            {
                                Round = round,
                                Phase = phase,
                                ModelName = modelName,
                                CouncilMembers = (councilMembers ?? participants).ToList(),
                                Role = role,
                                Content = string.Empty,
                                VisibleContent = string.Empty,
                                Error = $"{ex.GetType().Name}: {ex.Message}",
                                StartedAtUtc = failedAtUtc,
                                CompletedAtUtc = failedAtUtc,
                                DurationSeconds = 0d
                            };
                            ApplyHardwarePlan(failedStep, fallbackPlan);
                            completedPrimarySteps.Enqueue(failedStep);
                            return failedStep;
                        }
                        finally
                        {
                            participantStream?.Writer.TryComplete();
                            if (publishCompletedFirst)
                                presentationOrder?.Writer.TryWrite(modelName);
                            if (gateAcquired)
                                hostGate!.Release();
                        }
                    }

                    var steps = new List<MultiModelCouncilStep>();
                    if (sequentialPerHost)
                    {
                        var hostQueues = phaseParticipants
                            .GroupBy(GetCouncilExecutionHostKey, StringComparer.OrdinalIgnoreCase)
                            .Select(group => group.ToList())
                            .ToList();
                        progressMessage?.Invoke(
                            $"Council host-queue scheduler created {hostQueues.Count} parallel AI-host queue(s); every queue executes its assigned members sequentially.");

                        var hostTasks = hostQueues
                            .Select(async queue =>
                            {
                                var hostSteps = new List<MultiModelCouncilStep>();
                                foreach (var modelName in queue)
                                    hostSteps.Add(await ExecuteParticipantAsync(modelName, hostGates[hostKeys[modelName]]).ConfigureAwait(false));
                                return hostSteps;
                            })
                            .ToList();

                        var hostResults = await Task.WhenAll(hostTasks).ConfigureAwait(false);
                        foreach (var hostSteps in hostResults)
                            steps.AddRange(hostSteps);
                    }
                    else
                    {
                        var tasks = phaseParticipants
                            .Select(modelName =>
                            {
                                var hostKey = GetCouncilExecutionHostKey(modelName);
                                return ExecuteParticipantAsync(modelName, hostGates[hostKey]);
                            })
                            .ToList();

                        // Await every producer before closing presentation or disposing host gates,
                        // including when an individual participant observes cancellation.
                        steps.AddRange(await Task.WhenAll(tasks).ConfigureAwait(false));
                    }

                    steps.AddRange(completedTakeoverSteps.ToArray());
                    presentationOrder?.Writer.TryComplete();
                    await presentationTask.ConfigureAwait(false);

                    var participantOrder = phaseParticipants
                        .Select((modelName, index) => new { modelName, index })
                        .ToDictionary(item => item.modelName, item => item.index, StringComparer.OrdinalIgnoreCase);

                    foreach (var step in steps.OrderBy(step => participantOrder.TryGetValue(
                        string.IsNullOrWhiteSpace(step.RecoveryTargetModelName) ? step.ModelName : step.RecoveryTargetModelName,
                        out var index) ? index : int.MaxValue).ThenBy(step => string.IsNullOrWhiteSpace(step.RecoveryTargetModelName) ? 0 : 1))
                    {
                        var strictFunctionNames = strictAutomaticFunctions ? automaticFunctionAllowList : null;
                        var functionSteps = await AddCouncilStepAsync(result, step, stepCompleted, progressMessage,
                            allowDxFunctions && !step.IsProviderStartupTimeout, cancellationToken, strictFunctionNames).ConfigureAwait(false);
                        if (allowDxFunctions && functionSteps.Count > 0)
                        {
                            var continuationPlan = modelRoutes.TryGetValue(step.ModelName, out var configuredContinuationPlan)
                                ? configuredContinuationPlan
                                : new CouncilHardwareRoadPlan(step.ModelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{step.ModelName}", 100, maxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                            await ContinueAfterDxFunctionResultsAsync(
                                result,
                                step,
                                functionSteps,
                                baseUri,
                                councilMembers ?? participants,
                                promptFactory(step.ModelName),
                                participantBootstrap,
                                continuationPlan,
                                keepAlive,
                                modelTimeoutSeconds,
                                progressMessage,
                                streamUpdate,
                                stepCompleted,
                                automaticFunctionAllowList,
                                roleComplianceRetryCount,
                                finalAnswerRecoveryEnabled,
                                finalAnswerRecoveryMaxOutputTokens,
                                toolResultContinuationMode,
                                toolResultContinuationRounds,
                                cancellationToken,
                                strictFunctionNames).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Task.WhenAll has joined the producers before throwing. Retain their completed
                    // evidence, but do not dispatch any deferred text function calls on Stop.
                    var retained = completedPrimarySteps.ToArray().Concat(completedTakeoverSteps.ToArray()).ToList();
                    foreach (var step in retained.Where(step => !result.Steps.Contains(step))
                        .OrderBy(step => phaseParticipants.IndexOf(string.IsNullOrWhiteSpace(step.RecoveryTargetModelName) ? step.ModelName : step.RecoveryTargetModelName))
                        .ThenBy(step => string.IsNullOrWhiteSpace(step.RecoveryTargetModelName) ? 0 : 1))
                    {
                        MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                        stepCompleted?.Invoke(step);
                    }
                    if (retained.Count > 0)
                        result.Warnings.Add(runtimePolicy.GetString("CouncilPhaseCancellationPreservedNotice"));
                    throw;
                }
                finally
                {
                    if (participantStreams is not null)
                    {
                        foreach (var stream in participantStreams.Values)
                            stream.Writer.TryComplete();
                    }
                    presentationOrder?.Writer.TryComplete();
                    try
                    {
                        await presentationTask.ConfigureAwait(false);
                    }
                    finally
                    {
                        foreach (var gate in hostGates.Values)
                            gate.Dispose();
                    }
                }


            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council phase infrastructure failed for round {Round}, role {Role}, participant count {ParticipantCount}, " +
                    "max output {MaxOutputTokens}, max parallel {MaxParallelModels}, max context {MaxContextTokens}, timeout {TimeoutSeconds}s. " +
                    "The exception is rethrown so configured round recovery or the run-level failure boundary can preserve the failure instead of silently dropping the round.",
                    round,
                    role,
                    participants.Count,
                    maxOutputTokens,
                    maxParallelModels,
                    maxContextTokens,
                    modelTimeoutSeconds);
                throw;
            }
        }

        /// <summary>Builds the execution-qualified key used for a participant card in the shared chat.</summary>
        /// <param name="runId">Actual Council execution identifier, distinct from the root chat identity.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildCouncilParticipantActivityKey(Guid runId, int round, string phase, string role, string modelName)
        {
            try
            {
                return $"{runId:N}:{round}:{phase}:{role}:{modelName}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Building the Council participant live-activity key failed for round {Round}, phase {Phase}.", round, phase);
                throw;
            }
        }

        /// <summary>Builds the stable consumer identity used to route an immediate user heartbeat to the participant currently visible in ordered presentation.</summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildLiveInputConsumerKey(string modelName, string phase, string role)
        {
            try
            {
                return $"{modelName}|{phase}|{role}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Building the Council live-input consumer key failed for model {ModelName}, phase {Phase}.", modelName, phase);
                throw;
            }
        }

        /// <summary>
        /// Retrieves council execution host key as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string GetCouncilExecutionHostKey(string modelName)
        {
            try
            {
                var identity = new ProviderModelIdentity();
                if (identity.TryParseSelectionKey(modelName, out var reference))
                {
                    var runtimeKey = identity.GetRuntimeAuthorityKey(reference.Endpoint);
                    return string.IsNullOrWhiteSpace(runtimeKey) ? "provider:unknown-runtime" : runtimeKey;
                }

                return "legacy-or-unqualified-host";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not resolve AI host identity for Council member {ModelName}; using the legacy host gate.", modelName);
                return "legacy-or-unqualified-host";
            }
        }

        /// <summary>
        /// Publishes intact participant streams to the shared chat without making parallel hosts wait for selection-order presentation.
        /// </summary>
        /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="presentationOrder">Queue of participant identities, in completion order for parallel phases or selection order for serial phases.</param>
        /// <param name="participantStreams">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="publishCompletedFirst">Whether streams are already closed when queued and no foreground participant owns the direct-message claim.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task PumpCouncilParticipantStreamsAsync(
            Guid councilRunId,
            string phase,
            string role,
            ChannelReader<string> presentationOrder,
            IReadOnlyDictionary<string, Channel<string>> participantStreams,
            Action<string> streamUpdate,
            bool publishCompletedFirst)
        {
            try
            {
                // Read to producer-owned completion even when the Council is stopped, so completed
                // answers and closed partial streams are not discarded from the shared chat.
                await foreach (var modelName in presentationOrder.ReadAllAsync().ConfigureAwait(false))
                {
                    if (!participantStreams.TryGetValue(modelName, out var stream))
                        continue;

                    var consumerKey = BuildLiveInputConsumerKey(modelName, phase, role);
                    if (!publishCompletedFirst)
                        humanCollaboration.SetPreferredDirectUserMessageConsumer(councilRunId, consumerKey);
                    try
                    {
                        // Rich participant activities are updated immediately on the producer side. The shared
                        // transcript is presentation-only, so coalesce its many tiny provider fragments into bounded
                        // chunks. This prevents a completed member from spending minutes replaying thousands of
                        // fragments through DXAIChat before later ordered presentation catches up.
                        var orderedPresentationBuffer = new StringBuilder();
                        await foreach (var text in stream.Reader.ReadAllAsync().ConfigureAwait(false))
                        {
                            if (string.IsNullOrEmpty(text))
                                continue;
                            orderedPresentationBuffer.Append(text);
                            if (orderedPresentationBuffer.Length < 8192)
                                continue;
                            streamUpdate(orderedPresentationBuffer.ToString());
                            orderedPresentationBuffer.Clear();
                        }
                        if (orderedPresentationBuffer.Length > 0)
                            streamUpdate(orderedPresentationBuffer.ToString());
                    }
                    finally
                    {
                        if (!publishCompletedFirst)
                            humanCollaboration.ClearPreferredDirectUserMessageConsumer(councilRunId, consumerKey);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council participant stream presentation failed; model execution may still have completed in its host lane.");
                throw;
            }
        }

        /// <summary>
        /// Creates round skipped step as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="councilMembers">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="plan">Plan value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The multi model council step produced by the operation.</returns>
        private MultiModelCouncilStep CreateRoundSkippedStep(
            string modelName,
            IReadOnlyList<string> councilMembers,
            int round,
            string phase,
            string role,
            CouncilHardwareRoadPlan plan)
        {
    try
    {
                var now = DateTime.UtcNow;
                var step = new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    CouncilMembers = councilMembers.ToList(),
                    Role = role,
                    Content = $"_{modelName} was skipped because the user advanced the running Council beyond {phase}._",
                    VisibleContent = $"_{modelName} was skipped because the user advanced the running Council beyond {phase}._",
                    StartedAtUtc = now,
                    CompletedAtUtc = now,
                    DurationSeconds = 0
                };
                ApplyHardwarePlan(step, plan);
                return step;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateRoundSkippedStep)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateRoundSkippedStep)} failed.");
        throw;
    }
}

    }
}
