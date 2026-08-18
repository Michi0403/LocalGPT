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
            int finalAnswerRecoveryMaxOutputTokens = 8192)
        {
            try
            {
                using var councilScope = ambientContext.PushCouncil(result.RunId, round, phase);
                var runConfiguration = runConfigurations.Get(result.RunId);
                if (runConfiguration is { IsRunning: true })
                {
                    allowParallelHardwareRoads = runConfiguration.AllowParallelHardwareRoads;
                    maxParallelModels = Math.Max(1, runConfiguration.MaxParallelModels);
                    modelTimeoutSeconds = Math.Clamp(runConfiguration.ModelTimeoutSeconds, 30, 1800);
                }
                var failedModels = result.Steps
                    .Where(step => !string.IsNullOrWhiteSpace(step.Error))
                    .Select(step => step.ModelName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var phaseParticipants = OrderParticipantsByObservedHealth(
                    result,
                    participants.Where(model => !failedModels.Contains(model))).ToList();
                if (phaseParticipants.Count == 0)
                    phaseParticipants.Add(SelectHealthyParticipant(result, participants));
                if (phaseParticipants.Count < participants.Count)
                {
                    var excluded = participants.Where(model => !phaseParticipants.Contains(model, StringComparer.OrdinalIgnoreCase));
                    progressMessage?.Invoke($"Council health guard excluded {string.Join(", ", excluded)} from {phase} after recovery failed earlier in this run.");
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
                        var capacity = allowParallelHardwareRoads ? maxParallelModels : 1;
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
                var presentationTask = participantStreams is null
                    ? Task.CompletedTask
                    : PumpCouncilParticipantStreamsAsync(
                        result.RunId,
                        round,
                        phase,
                        role,
                        phaseParticipants,
                        participantStreams,
                        streamUpdate!,
                        cancellationToken);

                // Publish every selected member to the live board before host execution starts.
                // This makes remote/queued members visible immediately and keeps every provider-qualified
                // Council member equivalent even when an earlier ordered stream is still being presented.
                foreach (var modelName in phaseParticipants)
                {
                    var plannedRoad = modelRoutes.TryGetValue(modelName, out var configuredPlan)
                        ? configuredPlan
                        : new CouncilHardwareRoadPlan(modelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{modelName}", 100, maxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                    var queuedActivityKey = BuildCouncilParticipantActivityKey(round, phase, role, modelName);
                    var queuedRouteLabel = $"{GetCouncilExecutionHostKey(modelName)} · {plannedRoad.LaneKey}";
                    liveCouncilSessions.BeginParticipantActivity(result.RunId, queuedActivityKey, modelName, phase, role, queuedRouteLabel);
                    liveCouncilSessions.SetParticipantActivityStatus(result.RunId, queuedActivityKey, $"Queued for {queuedRouteLabel}; waiting for this member's one Council turn.");
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
                try
                {
                    async Task<MultiModelCouncilStep> ExecuteParticipantAsync(string modelName, SemaphoreSlim? hostGate)
                    {
                        var fallbackPlan = modelRoutes.TryGetValue(modelName, out var configuredPlan)
                            ? configuredPlan
                            : new CouncilHardwareRoadPlan(modelName, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{modelName}", 100, maxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                        var gateAcquired = false;
                        var participantStream = participantStreams is null ? null : participantStreams[modelName];
                        var activityKey = BuildCouncilParticipantActivityKey(round, phase, role, modelName);
                        var routeLabel = $"{GetCouncilExecutionHostKey(modelName)} · {fallbackPlan.LaneKey}";
                        Action<string>? participantStreamUpdate = participantStream is null
                            ? null
                            : text =>
                            {
                                if (!string.IsNullOrEmpty(text))
                                {
                                    participantStream.Writer.TryWrite(text);
                                    // The ordered transcript is still pumped member-by-member to avoid corrupting
                                    // provider HTML/thinking markup. This side channel makes every host/model visible
                                    // immediately while the host queues execute in parallel.
                                    liveCouncilSessions.AppendParticipantActivity(result.RunId, activityKey, text);
                                }
                            };
                        try
                        {
                            if (hostGate is not null)
                            {
                                using var gateCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, roundSkipToken);
                                liveCouncilSessions.SetParticipantActivityStatus(result.RunId, activityKey, $"Waiting for AI host road {routeLabel}.");
                                await hostGate.WaitAsync(gateCancellation.Token).ConfigureAwait(false);
                                gateAcquired = true;
                            }

                            liveCouncilSessions.SetParticipantActivityStatus(result.RunId, activityKey, $"Running on {routeLabel}.");
                            var step = await RunParticipantAsync(
                                baseUri, modelName, councilMembers ?? participants, round, phase, role, promptFactory(modelName), participantBootstrap,
                                fallbackPlan.EffectiveMaxOutputTokens, keepAlive, fallbackPlan.OllamaNumGpu, fallbackPlan.EffectiveMaxContextTokens,
                                modelTimeoutSeconds, participantStreamUpdate, cancellationToken,
                                fallbackPlan: fallbackPlan,
                                progressMessage: progressMessage,
                                enableAutomaticTools: allowDxFunctions,
                                automaticFunctionAllowList: automaticFunctionAllowList,
                                roleComplianceRetryCount: roleComplianceRetryCount,
                                finalAnswerRecoveryEnabled: finalAnswerRecoveryEnabled,
                                finalAnswerRecoveryMaxOutputTokens: finalAnswerRecoveryMaxOutputTokens).ConfigureAwait(false);
                            ArgumentNullException.ThrowIfNull(step);
                            liveCouncilSessions.SetParticipantActivityResult(
                                result.RunId,
                                activityKey,
                                step.VisibleContent);
                            liveCouncilSessions.CompleteParticipantActivity(
                                result.RunId,
                                activityKey,
                                string.IsNullOrWhiteSpace(step.Error)
                                    ? "Model completed. Its live result is available in this lane now; ordered transcript integration may follow later."
                                    : $"Model completed with an error: {step.Error}");
                            return step;
                        }
                        catch (OperationCanceledException) when (
                            roundSkipToken.IsCancellationRequested &&
                            !cancellationToken.IsCancellationRequested)
                        {
                            liveCouncilSessions.CompleteParticipantActivity(result.RunId, activityKey, "Participant was skipped because the current Council phase was cancelled.");
                            return CreateRoundSkippedStep(
                                modelName,
                                councilMembers ?? participants,
                                round,
                                phase,
                                role,
                                fallbackPlan);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            liveCouncilSessions.CompleteParticipantActivity(
                                result.RunId,
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
                                result.RunId,
                                activityKey,
                                $"Participant infrastructure failed: {ex.Message}");
                            var failedAtUtc = DateTime.UtcNow;
                            return new MultiModelCouncilStep
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
                        }
                        finally
                        {
                            participantStream?.Writer.TryComplete();
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
                                    hostSteps.Add(await ExecuteParticipantAsync(modelName, hostGate: null).ConfigureAwait(false));
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

                        var pending = tasks.ToList();
                        while (pending.Count > 0)
                        {
                            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
                            pending.Remove(completed);
                            var step = await completed.ConfigureAwait(false);
                            ArgumentNullException.ThrowIfNull(step);
                            steps.Add(step);
                        }
                    }

                    await presentationTask.ConfigureAwait(false);

                    var participantOrder = phaseParticipants
                        .Select((modelName, index) => new { modelName, index })
                        .ToDictionary(item => item.modelName, item => item.index, StringComparer.OrdinalIgnoreCase);

                    foreach (var step in steps.OrderBy(step => participantOrder.TryGetValue(step.ModelName, out var index) ? index : int.MaxValue))
                    {
                        await AddCouncilStepAsync(result, step, stepCompleted, progressMessage, allowDxFunctions, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (participantStreams is not null)
                    {
                        foreach (var stream in participantStreams.Values)
                            stream.Writer.TryComplete();
                    }
                    foreach (var gate in hostGates.Values)
                        gate.Dispose();
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

        /// <summary>Builds the stable run-local key used for one participant's live activity card.</summary>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildCouncilParticipantActivityKey(int round, string phase, string role, string modelName)
        {
            try
            {
                return $"{round}:{phase}:{role}:{modelName}";
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
                if (identity.TryParseSelectionKey(modelName, out var reference) &&
                    Uri.TryCreate(reference.Endpoint, UriKind.Absolute, out var endpoint))
                {
                    var host = string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                        ? "127.0.0.1"
                        : endpoint.Host;
                    return string.IsNullOrWhiteSpace(host) ? "provider:unknown-host" : host.Trim().ToLowerInvariant();
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
        /// Performs pump council participant streams as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participantOrder">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="participantStreams">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task PumpCouncilParticipantStreamsAsync(
            Guid councilRunId,
            int round,
            string phase,
            string role,
            IReadOnlyList<string> participantOrder,
            IReadOnlyDictionary<string, Channel<string>> participantStreams,
            Action<string> streamUpdate,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var modelName in participantOrder)
                {
                    if (!participantStreams.TryGetValue(modelName, out var stream))
                        continue;

                    var consumerKey = BuildLiveInputConsumerKey(modelName, phase, role);
                    humanCollaboration.SetPreferredDirectUserMessageConsumer(councilRunId, consumerKey);
                    try
                    {
                        // Rich participant activities are updated immediately on the producer side. The ordered
                        // transcript is presentation-only, so coalesce its many tiny provider fragments into bounded
                        // chunks. This prevents a completed member from spending minutes replaying thousands of
                        // fragments through DXAIChat before later ordered presentation catches up.
                        var orderedPresentationBuffer = new StringBuilder();
                        await foreach (var text in stream.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
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
                        humanCollaboration.ClearPreferredDirectUserMessageConsumer(councilRunId, consumerKey);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
