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
        /// Executes configured workflow definition as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="maxParallelModels">Max parallel models value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="leaderModel">Leader model value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleAssignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="rolePairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="state">State value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopGroup">Loop group value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopIteration">Loop iteration value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopMaximumIterations">Loop maximum iterations value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="workflowRevision">Workflow revision value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="xRoundCause">X round cause value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="suppressOrganicFunctions">Value indicating whether suppress organic functions should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The configured workflow execution state produced by the operation.</returns>
        private async Task<ConfiguredWorkflowExecutionState> ExecuteConfiguredWorkflowDefinitionAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition definition,
            string baseUri,
            IReadOnlyList<string> participants,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            int maxParallelModels,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            string leaderModel,
            IDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            ConfiguredWorkflowExecutionState state,
            string loopGroup,
            int loopIteration,
            int loopMaximumIterations,
            int workflowRevision,
            string xRoundCause,
            bool suppressOrganicFunctions,
            CancellationToken cancellationToken)
        {
            try
            {
                var nextAutomaticRound = state.Round;
                var expandedStepIndex = state.ExpandedStepIndex;
                var previousStep = state.PreviousStep;
                var fallbackAnswer = state.FallbackAnswer;
                var finalAnswer = state.FinalAnswer;
                CouncilXRoundDirective? xDirective = null;
                var repeatCount = Math.Clamp(definition.RepeatCount, 1, 100);
                var automaticFunctionPolicy = councilAutomaticFunctionPolicy.Resolve(team, definition, suppressOrganicFunctions);
                var effectiveAllowDxFunctions = automaticFunctionPolicy.Enabled;

                for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var round = definition.LogicalRoundNumber > 0
                        ? definition.LogicalRoundNumber - 1
                        : nextAutomaticRound;
                    var basePhase = string.IsNullOrWhiteSpace(definition.Phase) ? definition.DisplayName : definition.Phase;
                    var phaseParts = new List<string> { basePhase };
                    if (!string.IsNullOrWhiteSpace(loopGroup))
                        phaseParts.Add($"loop {loopIteration}/{loopMaximumIterations}");
                    if (repeatCount > 1)
                        phaseParts.Add($"repeat {repeatIndex + 1}/{repeatCount}");
                    if (workflowRevision > 1)
                        phaseParts.Add($"X revision {workflowRevision}");
                    var phase = string.Join(" · ", phaseParts);
                    var roleAssignment = GetConfiguredRoleAssignment(
                        result,
                        request,
                        team,
                        definition.Role,
                        participants,
                        roleAssignments);
                    var roleParticipants = roleAssignment.AiParticipants;
                    var visiblePreviousStep = BuildConfiguredWorkflowPreviousStep(
                        result,
                        definition,
                        roleAssignment,
                        round,
                        previousStep);

                    if (definition.RequiresHumanCheckpoint)
                    {
                        request.ProgressMessage?.Invoke($"Council workflow is checking the human collaboration gate before configured round {round}: {phase}.");
                        await WaitForHumanBoundaryAsync(
                            result,
                            request,
                            round,
                            phase,
                            HumanCollaborationBoundary.Round,
                            cancellationToken).ConfigureAwait(false);
                    }

                    if (roleAssignment.HumanParticipationMode is HumanParticipationMode.Required or HumanParticipationMode.HumanOnly)
                    {
                        await WaitForConfiguredRoleHumanParticipationAsync(
                            result,
                            request,
                            team,
                            roleAssignment,
                            round,
                            phase,
                            repeatIndex,
                            cancellationToken).ConfigureAwait(false);
                    }

                    var heartbeatBootstrap = await PrepareHumanHeartbeatAsync(
                        result,
                        request,
                        round,
                        phase,
                        bootstrap,
                        cancellationToken).ConfigureAwait(false);
                    var executionMode = NormalizeConfiguredExecutionMode(definition.ExecutionMode);
                    var hasRepeatedRoleParticipants = roleParticipants.Count != roleParticipants
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    if (hasRepeatedRoleParticipants &&
                        executionMode is "AllMembersParallel" or "AllMembersSequentialOnEachAIHostParallel")
                    {
                        executionMode = "AllMembersSequential";
                        request.ProgressMessage?.Invoke(
                            $"Configured role '{roleAssignment.RoleName}' contains repeated provider-bound invocations. LocalGPT executes those repeated turns sequentially so live-stream, heartbeat and activity identities remain unambiguous.");
                    }
                    var xRoundActive = definition.XFunctionsEnabled && effectiveAllowDxFunctions;
                    if (xRoundActive)
                    {
                        councilXRounds.Activate(new CouncilXRoundStepContext(
                            result.RunId,
                            round,
                            phase,
                            definition.Key,
                            definition.DisplayName,
                            definition.XCanRevisit,
                            definition.XCanReturnText,
                            definition.XCanStartSingleModel,
                            definition.XCanStartCouncil,
                            definition.XMaximumTransitions,
                            definition.XRequiresHumanApproval,
                            definition.XDefaultTargetStepKey,
                            definition.XChildCouncilTeamKey,
                            definition.XMaximumChildCouncilDepth,
                            definition.XChildModelName));
                    }
                    request.ProgressMessage?.Invoke(
                        $"Executing configured council round {round}: {definition.DisplayName} / {phase} / {definition.Role} using {executionMode}; " +
                        $"role assignment: {roleAssignment.AiSelectionDescription}; human mode {roleAssignment.HumanParticipationMode}; " +
                        $"organic functions {automaticFunctionPolicy.Description}; " +
                        $"X-Rounds {(definition.XFunctionsEnabled && effectiveAllowDxFunctions ? "active" : "inactive")}.");

                    if (roleParticipants.Count == 0)
                    {
                        request.ProgressMessage?.Invoke(
                            $"Configured role '{roleAssignment.RoleName}' is human-only. Its human response is the round contribution; no AI model is called.");
                    }
                    else
                    {
                        switch (executionMode)
                        {
                            case "SystemBenchmarkCalibration":
                                {
                                    var calibrationStartedAtUtc = DateTime.UtcNow;
                                    var exactBenchmarkTargets = result.ModelSelections
                                        .Where(model => model is not null && !string.IsNullOrWhiteSpace(model.ModelName))
                                        .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
                                        .Select(group => group.First())
                                        .ToList();
                                    if (exactBenchmarkTargets.Count == 0)
                                        throw new InvalidOperationException("The deterministic benchmark workflow has no provider-qualified Council targets.");

                                    request.ProgressMessage?.Invoke(
                                        $"Configured round {round} is entering one deterministic LocalGPT all-model measurement phase for all {exactBenchmarkTargets.Count} distinct provider-qualified Council member(s). Model-generated sampling, quartets and representative packs are ignored.");
                                    var calibration = await benchmarkCalibration.RunAsync(
                                        new CouncilBenchmarkCalibrationRequest
                                        {
                                            CouncilRunId = result.RunId,
                                            Targets = exactBenchmarkTargets,
                                            ProfileCount = 5,
                                            MinimumContextTokens = catalog.MinContextTokens,
                                            MinimumOutputTokens = catalog.MinOutputTokens,
                                            MaximumContextTokens = catalog.MaxContextTokens,
                                            MaximumOutputTokens = request.MaxOutputTokens,
                                            StopAfterConsecutiveProfileFailures = 0,
                                            MaxSecondsPerCall = modelTimeoutSeconds,
                                            TaskPackText = visiblePreviousStep,
                                            PresetBaseName = $"Initial calibration {DateTimeOffset.Now:yyyy-MM-dd HHmmss}",
                                            UserConfirmed = true
                                        },
                                        progress =>
                                        {
                                            request.ProgressMessage?.Invoke(progress);
                                            request.StreamUpdate?.Invoke(progress.EndsWith('\n') ? progress : progress + Environment.NewLine);
                                        },
                                        cancellationToken).ConfigureAwait(false);
                                    if (calibration.RequestedTargetCount != exactBenchmarkTargets.Count)
                                        throw new InvalidOperationException(
                                            $"The deterministic benchmark coverage contract changed from {exactBenchmarkTargets.Count} frozen Council target(s) to {calibration.RequestedTargetCount}; benchmark continuation was stopped rather than accepting silent sampling.");
                                    var systemStep = new MultiModelCouncilStep
                                    {
                                        SortOrder = result.Steps.Count + 1,
                                        Round = round,
                                        Phase = phase,
                                        ModelName = "LocalGPT Benchmark Engine",
                                        ProviderName = "LocalGPT",
                                        ProviderEndpoint = "in-process",
                                        ProviderModelName = "benchmark-calibration",
                                        CouncilMembers = participants.ToList(),
                                        Role = definition.Role,
                                        Content = calibration.SummaryMarkdown,
                                        VisibleContent = calibration.SummaryMarkdown,
                                        StartedAtUtc = calibrationStartedAtUtc,
                                        CompletedAtUtc = DateTime.UtcNow
                                    };
                                    systemStep.DurationSeconds = Math.Max(
                                        0d,
                                        (systemStep.CompletedAtUtc - systemStep.StartedAtUtc).TotalSeconds);
                                    MultiModelCouncilServiceAddOrderedStep(result, systemStep, logger);
                                    request.StepCompleted?.Invoke(systemStep);
                                    break;
                                }
                            case "AllMembersParallel":
                                {
                                    var transcript = BuildConfiguredWorkflowTranscript(result, team, definition, roleAssignment, round);
                                    await RunPhaseAsync(
                                        result,
                                        baseUri,
                                        roleParticipants,
                                        round,
                                        phase,
                                        definition.Role,
                                        modelName => RenderConfiguredWorkflowPrompt(
                                            definition,
                                            team,
                                            request,
                                            modelName,
                                            participants,
                                            roleAssignment,
                                            rolePairings,
                                            round,
                                            repeatIndex,
                                            repeatCount,
                                            loopGroup,
                                            loopIteration,
                                            loopMaximumIterations,
                                            transcript,
                                            visiblePreviousStep,
                                            automaticFunctionPolicy),
                                        heartbeatBootstrap,
                                        request.MaxOutputTokens,
                                        maxParallelModels,
                                        keepAlive,
                                        ollamaNumGpu,
                                        maxContextTokens,
                                        modelTimeoutSeconds,
                                        request.ProgressMessage,
                                        request.StreamUpdate,
                                        request.StepCompleted,
                                        modelRoutes,
                                        request.AllowParallelHardwareRoads,
                                        cancellationToken,
                                        allowDxFunctions: effectiveAllowDxFunctions,
                                        councilMembers: participants,
                                        automaticFunctionAllowList: automaticFunctionPolicy.AutomaticFunctionAllowList,
                                        roleComplianceRetryCount: definition.RoleComplianceRetryCount,
                                        finalAnswerRecoveryEnabled: definition.FinalAnswerRecoveryEnabled,
                                        finalAnswerRecoveryMaxOutputTokens: definition.FinalAnswerRecoveryMaxOutputTokens).ConfigureAwait(false);
                                    break;
                                }
                            case "AllMembersSequentialOnEachAIHostParallel":
                                {
                                    var transcript = BuildConfiguredWorkflowTranscript(result, team, definition, roleAssignment, round);
                                    await RunPhaseAsync(
                                        result,
                                        baseUri,
                                        roleParticipants,
                                        round,
                                        phase,
                                        definition.Role,
                                        modelName => RenderConfiguredWorkflowPrompt(
                                            definition,
                                            team,
                                            request,
                                            modelName,
                                            participants,
                                            roleAssignment,
                                            rolePairings,
                                            round,
                                            repeatIndex,
                                            repeatCount,
                                            loopGroup,
                                            loopIteration,
                                            loopMaximumIterations,
                                            transcript,
                                            visiblePreviousStep,
                                            automaticFunctionPolicy),
                                        heartbeatBootstrap,
                                        request.MaxOutputTokens,
                                        1,
                                        keepAlive,
                                        ollamaNumGpu,
                                        maxContextTokens,
                                        modelTimeoutSeconds,
                                        request.ProgressMessage,
                                        request.StreamUpdate,
                                        request.StepCompleted,
                                        modelRoutes,
                                        request.AllowParallelHardwareRoads,
                                        cancellationToken,
                                        allowDxFunctions: effectiveAllowDxFunctions,
                                        councilMembers: participants,
                                        sequentialPerHost: true,
                                        automaticFunctionAllowList: automaticFunctionPolicy.AutomaticFunctionAllowList,
                                        roleComplianceRetryCount: definition.RoleComplianceRetryCount,
                                        finalAnswerRecoveryEnabled: definition.FinalAnswerRecoveryEnabled,
                                        finalAnswerRecoveryMaxOutputTokens: definition.FinalAnswerRecoveryMaxOutputTokens).ConfigureAwait(false);
                                    break;
                                }
                            case "AllMembersSequential":
                                {
                                    foreach (var modelName in OrderParticipantsByObservedHealth(result, roleParticipants))
                                    {
                                        var transcript = BuildConfiguredWorkflowTranscript(result, team, definition, roleAssignment, round);
                                        await RunConfiguredParticipantAsync(
                                            result,
                                            request,
                                            definition,
                                            team,
                                            baseUri,
                                            participants,
                                            roleAssignment,
                                            rolePairings,
                                            modelName,
                                            round,
                                            phase,
                                            repeatIndex,
                                            repeatCount,
                                            loopGroup,
                                            loopIteration,
                                            loopMaximumIterations,
                                            transcript,
                                            visiblePreviousStep,
                                            heartbeatBootstrap,
                                            modelRoutes,
                                            keepAlive,
                                            ollamaNumGpu,
                                            maxContextTokens,
                                            modelTimeoutSeconds,
                                            effectiveAllowDxFunctions,
                                            automaticFunctionPolicy,
                                            cancellationToken).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            default:
                                {
                                    var modelName = SelectConfiguredWorkflowParticipant(
                                        result,
                                        request,
                                        definition,
                                        executionMode,
                                        roleParticipants,
                                        leaderModel,
                                        expandedStepIndex);
                                    var transcript = BuildConfiguredWorkflowTranscript(result, team, definition, roleAssignment, round);
                                    await RunConfiguredParticipantAsync(
                                        result,
                                        request,
                                        definition,
                                        team,
                                        baseUri,
                                        participants,
                                        roleAssignment,
                                        rolePairings,
                                        modelName,
                                        round,
                                        phase,
                                        repeatIndex,
                                        repeatCount,
                                        loopGroup,
                                        loopIteration,
                                        loopMaximumIterations,
                                        transcript,
                                        visiblePreviousStep,
                                        heartbeatBootstrap,
                                        modelRoutes,
                                        keepAlive,
                                        ollamaNumGpu,
                                        maxContextTokens,
                                        modelTimeoutSeconds,
                                        effectiveAllowDxFunctions,
                                        automaticFunctionPolicy,
                                        cancellationToken).ConfigureAwait(false);
                                    break;
                                }
                        }
                    }

                    var roundSteps = result.Steps
                        .Where(step =>
                            step.Round == round &&
                            string.Equals(step.Phase, phase, StringComparison.Ordinal) &&
                            (roleParticipants.Contains(step.ModelName, StringComparer.OrdinalIgnoreCase) ||
                             step.ModelName.StartsWith("Human:", StringComparison.OrdinalIgnoreCase) ||
                             (executionMode == "SystemBenchmarkCalibration" &&
                              string.Equals(step.ModelName, "LocalGPT Benchmark Engine", StringComparison.OrdinalIgnoreCase))))
                        .ToList();
                    foreach (var roundStep in roundSteps)
                    {
                        roundStep.WorkflowStepKey = definition.Key;
                        roundStep.WorkflowRevision = Math.Max(1, workflowRevision);
                        roundStep.XRoundCause = xRoundCause ?? string.Empty;
                    }

                    IReadOnlyList<CouncilXRoundDirective> emittedXDirectives = [];
                    if (xRoundActive)
                    {
                        emittedXDirectives = councilXRounds.Drain(result.RunId, round, phase);
                        councilXRounds.Deactivate(result.RunId, round, phase);
                    }

                    var stageAnswer = BuildConfiguredWorkflowStageAnswer(roundSteps);
                    var distinctAiRoleParticipants = roleParticipants
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var usablePrimaryAiSteps = roundSteps
                        .Where(step =>
                            distinctAiRoleParticipants.Contains(step.ModelName, StringComparer.OrdinalIgnoreCase) &&
                            string.IsNullOrWhiteSpace(step.Error) &&
                            !string.IsNullOrWhiteSpace(step.VisibleContent) &&
                            !IsRoundSkippedStep(step))
                        .GroupBy(step => step.ModelName, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.Last())
                        .ToList();

                    IReadOnlyList<MultiModelCouncilStep> rolePeerReviewSteps = [];
                    if (definition.EnableRolePeerReview && usablePrimaryAiSteps.Count >= 2)
                    {
                        var rolePeerReviewPhase = $"{phase} · role peer review {repeatIndex + 1}";
                        request.ProgressMessage?.Invoke(
                            $"Configured role '{roleAssignment.RoleName}' is running the optional peer usefulness/voting round across {usablePrimaryAiSteps.Count} role members.");
                        var roleEvidence = BuildConfiguredRoleEvidence(roundSteps);
                        await RunPhaseAsync(
                            result,
                            baseUri,
                            usablePrimaryAiSteps.Select(step => step.ModelName).ToList(),
                            round,
                            rolePeerReviewPhase,
                            $"{definition.Role} · Peer review",
                            reviewerModelName => BuildConfiguredRolePeerReviewPrompt(
                                team,
                                request,
                                definition,
                                roleAssignment,
                                reviewerModelName,
                                usablePrimaryAiSteps,
                                roleEvidence),
                            heartbeatBootstrap,
                            Math.Min(request.MaxOutputTokens, 4096),
                            maxParallelModels,
                            keepAlive,
                            ollamaNumGpu,
                            maxContextTokens,
                            modelTimeoutSeconds,
                            request.ProgressMessage,
                            request.StreamUpdate,
                            request.StepCompleted,
                            modelRoutes,
                            request.AllowParallelHardwareRoads,
                            cancellationToken,
                            allowDxFunctions: false,
                            councilMembers: participants).ConfigureAwait(false);

                        rolePeerReviewSteps = result.Steps
                            .Where(step =>
                                step.Round == round &&
                                string.Equals(step.Phase, rolePeerReviewPhase, StringComparison.Ordinal) &&
                                usablePrimaryAiSteps.Any(primary => string.Equals(primary.ModelName, step.ModelName, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                        foreach (var reviewStep in rolePeerReviewSteps)
                        {
                            reviewStep.WorkflowStepKey = definition.Key;
                            reviewStep.WorkflowRevision = Math.Max(1, workflowRevision);
                            reviewStep.XRoundCause = xRoundCause ?? string.Empty;
                        }
                    }

                    if (definition.SummarizeRoleResults && usablePrimaryAiSteps.Count >= 2)
                    {
                        var synthesisParticipant = SelectConfiguredRoleSynthesisParticipant(
                            result,
                            team,
                            definition,
                            roleAssignment,
                            usablePrimaryAiSteps.Select(step => step.ModelName).ToList(),
                            round,
                            repeatIndex);
                        var roleSynthesisPhase = $"{phase} · role synthesis {repeatIndex + 1}";
                        request.ProgressMessage?.Invoke(
                            $"Configured role '{roleAssignment.RoleName}' is consolidating {usablePrimaryAiSteps.Count} member results through {synthesisParticipant}.");
                        var roleEvidence = BuildConfiguredRoleEvidence(roundSteps);
                        var peerReviewEvidence = BuildConfiguredRoleEvidence(rolePeerReviewSteps);
                        await RunPhaseAsync(
                            result,
                            baseUri,
                            [synthesisParticipant],
                            round,
                            roleSynthesisPhase,
                            $"{definition.Role} · Role synthesis",
                            _ => BuildConfiguredRoleSynthesisPrompt(
                                team,
                                request,
                                definition,
                                roleAssignment,
                                synthesisParticipant,
                                roleEvidence,
                                peerReviewEvidence),
                            heartbeatBootstrap,
                            Math.Min(request.MaxOutputTokens, 4096),
                            1,
                            keepAlive,
                            ollamaNumGpu,
                            maxContextTokens,
                            modelTimeoutSeconds,
                            request.ProgressMessage,
                            request.StreamUpdate,
                            request.StepCompleted,
                            modelRoutes,
                            request.AllowParallelHardwareRoads,
                            cancellationToken,
                            allowDxFunctions: false,
                            councilMembers: participants).ConfigureAwait(false);

                        var synthesisSteps = result.Steps
                            .Where(step =>
                                step.Round == round &&
                                string.Equals(step.Phase, roleSynthesisPhase, StringComparison.Ordinal) &&
                                string.Equals(step.ModelName, synthesisParticipant, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        foreach (var synthesisStep in synthesisSteps)
                        {
                            synthesisStep.WorkflowStepKey = definition.Key;
                            synthesisStep.WorkflowRevision = Math.Max(1, workflowRevision);
                            synthesisStep.XRoundCause = xRoundCause ?? string.Empty;
                        }

                        var synthesizedAnswer = BuildConfiguredWorkflowStageAnswer(synthesisSteps);
                        if (!string.IsNullOrWhiteSpace(synthesizedAnswer))
                            stageAnswer = synthesizedAnswer;
                        else
                            result.Warnings.Add($"Configured role '{roleAssignment.RoleName}' requested role-result synthesis, but the synthesis turn did not produce a usable visible response. The original role-member results remain authoritative for this step.");
                    }
                    else if (definition.EnableRolePeerReview && rolePeerReviewSteps.Count > 0)
                    {
                        var peerReviewAnswer = BuildConfiguredWorkflowStageAnswer(rolePeerReviewSteps);
                        if (!string.IsNullOrWhiteSpace(peerReviewAnswer))
                        {
                            stageAnswer = string.IsNullOrWhiteSpace(stageAnswer)
                                ? $"## Role peer review{Environment.NewLine}{peerReviewAnswer}"
                                : $"{stageAnswer.Trim()}{Environment.NewLine}{Environment.NewLine}## Role peer review{Environment.NewLine}{peerReviewAnswer}";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(stageAnswer))
                    {
                        previousStep = stageAnswer;
                        fallbackAnswer = stageAnswer;
                        if (definition.ProducesFinalAnswer)
                            finalAnswer = stageAnswer;
                    }
                    else
                    {
                        result.Warnings.Add($"Configured council round '{definition.DisplayName}' did not produce a usable visible response.");
                    }

                    if (emittedXDirectives.Count > 0)
                    {
                        var candidate = emittedXDirectives[0];
                        if (emittedXDirectives.Count > 1)
                        {
                            result.Warnings.Add(
                                $"Configured X-Round step '{definition.DisplayName}' emitted {emittedXDirectives.Count} control requests. LocalGPT applies only the first request deterministically and preserves the others only in logs.");
                        }

                        if (!councilXRounds.TryConsumeTransitionBudget(
                                result.RunId,
                                definition.Key,
                                Math.Max(1, definition.XMaximumTransitions),
                                out var usedTransitions))
                        {
                            var warning =
                                $"X-Round transition from '{definition.DisplayName}' was ignored because its configured budget of {Math.Max(1, definition.XMaximumTransitions)} transition(s) is exhausted (request {usedTransitions}).";
                            result.Warnings.Add(warning);
                            request.ProgressMessage?.Invoke(warning);
                        }
                        else if (!definition.XRequiresHumanApproval ||
                                 await WaitForXRoundApprovalAsync(result, request, team, definition, candidate, cancellationToken).ConfigureAwait(false))
                        {
                            xDirective = candidate;
                            request.ProgressMessage?.Invoke(
                                $"Accepted X-Round action {candidate.Action} from '{definition.DisplayName}' ({usedTransitions}/{Math.Max(1, definition.XMaximumTransitions)} configured transition budget).");
                        }
                    }

                    if (definition.LogicalRoundNumber <= 0)
                        nextAutomaticRound = round + 1;
                    else
                        nextAutomaticRound = Math.Max(nextAutomaticRound, round + 1);
                    expandedStepIndex++;
                    if (xDirective is not null)
                        break;
                }

                return new ConfiguredWorkflowExecutionState(nextAutomaticRound, expandedStepIndex, previousStep, fallbackAnswer, finalAnswer, xDirective);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} failed while executing configured workflow definition {StepKey} for role {RoleName}.",
                    result.RunId,
                    definition.Key,
                    definition.Role);
                throw;
            }
        }

    }
}
