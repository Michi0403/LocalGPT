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
        /// Performs run configured workflow as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="maxParallelModels">Max parallel models value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        private async Task<string> RunConfiguredWorkflowAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            string baseUri,
            IReadOnlyList<string> participants,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            int maxParallelModels,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                var configuredSteps = team.WorkflowSteps
                    .Where(step => step.IsEnabled)
                    .OrderBy(step => step.SortOrder)
                    .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (configuredSteps.Count == 0)
                    throw new InvalidOperationException($"Council team '{team.DisplayName}' has no enabled workflow step.");

                var requestedLeader = participants.FirstOrDefault(model => string.Equals(model, request.CouncilLeaderModelName, StringComparison.OrdinalIgnoreCase));
                var leaderModel = SelectHealthyParticipant(result, participants, requestedLeader);
                var roleAssignments = BuildConfiguredRoleAssignments(result, request, team, participants);
                var rolePairings = BuildConfiguredRolePairings(result, request, team, roleAssignments);
                if (team.AllMembersReadinessPreflightMode == CouncilAllMembersReadinessPreflightMode.RoleAwareProbe)
                {
                    await RunConfiguredAllMembersReadinessPreflightAsync(
                        result,
                        request,
                        team,
                        baseUri,
                        participants,
                        bootstrap,
                        modelRoutes,
                        keepAlive,
                        ollamaNumGpu,
                        maxContextTokens,
                        modelTimeoutSeconds,
                        roleAssignments,
                        cancellationToken).ConfigureAwait(false);
                }
                var state = new ConfiguredWorkflowExecutionState(0, 0, string.Empty, string.Empty, string.Empty);
                var workflowRevisions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var nextExecutionIsReconsideration = false;
                var nextXRoundCause = string.Empty;

                for (var stepIndex = 0; stepIndex < configuredSteps.Count;)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var firstStep = configuredSteps[stepIndex];
                    if (string.IsNullOrWhiteSpace(firstStep.LoopGroup))
                    {
                        var revision = workflowRevisions.TryGetValue(firstStep.Key, out var previousRevision)
                            ? previousRevision + 1
                            : 1;
                        workflowRevisions[firstStep.Key] = revision;
                        state = await ExecuteConfiguredWorkflowDefinitionAsync(
                            result,
                            request,
                            team,
                            firstStep,
                            baseUri,
                            participants,
                            bootstrap,
                            modelRoutes,
                            maxParallelModels,
                            keepAlive,
                            ollamaNumGpu,
                            maxContextTokens,
                            modelTimeoutSeconds,
                            leaderModel,
                            roleAssignments,
                            rolePairings,
                            state,
                            loopGroup: string.Empty,
                            loopIteration: 1,
                            loopMaximumIterations: 1,
                            workflowRevision: revision,
                            xRoundCause: nextXRoundCause,
                            suppressOrganicFunctions: nextExecutionIsReconsideration,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        nextExecutionIsReconsideration = false;
                        nextXRoundCause = string.Empty;

                        var resolution = await ResolveConfiguredXDirectiveAsync(
                            result,
                            request,
                            team,
                            firstStep,
                            configuredSteps,
                            state,
                            baseUri,
                            participants,
                            bootstrap,
                            modelRoutes,
                            keepAlive,
                            ollamaNumGpu,
                            maxContextTokens,
                            modelTimeoutSeconds,
                            leaderModel,
                            cancellationToken).ConfigureAwait(false);
                        state = resolution.State;
                        if (resolution.StopWorkflow)
                            return state.FinalAnswer;
                        if (resolution.JumpStepIndex is int jumpIndex)
                        {
                            stepIndex = jumpIndex;
                            nextExecutionIsReconsideration = resolution.ReconsiderTarget;
                            nextXRoundCause = resolution.Cause;
                            continue;
                        }

                        stepIndex++;
                        continue;
                    }

                    var loopGroup = firstStep.LoopGroup.Trim();
                    var loopSteps = new List<CouncilWorkflowStepDefinition>();
                    while (stepIndex + loopSteps.Count < configuredSteps.Count &&
                           string.Equals(configuredSteps[stepIndex + loopSteps.Count].LoopGroup, loopGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        loopSteps.Add(configuredSteps[stepIndex + loopSteps.Count]);
                    }

                    var maximumIterations = loopSteps.Max(step => Math.Clamp(step.MaximumLoopIterations, 1, 100));
                    var completionMarker = loopSteps
                        .Select(step => step.LoopCompletionMarker?.Trim() ?? string.Empty)
                        .FirstOrDefault(marker => !string.IsNullOrWhiteSpace(marker)) ?? string.Empty;
                    var completed = false;
                    var xJumpedFromLoop = false;
                    request.ProgressMessage?.Invoke(
                        $"Starting bounded workflow loop '{loopGroup}' with {loopSteps.Count} step(s), up to {maximumIterations} iteration(s)" +
                        (string.IsNullOrWhiteSpace(completionMarker) ? "." : $", stopping when '{completionMarker}' appears."));

                    for (var loopIteration = 1; loopIteration <= maximumIterations && !xJumpedFromLoop; loopIteration++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var completionReportedThisIteration = false;
                        request.ProgressMessage?.Invoke($"Workflow loop '{loopGroup}' iteration {loopIteration}/{maximumIterations} started.");
                        foreach (var loopStep in loopSteps)
                        {
                            var firstLoopStepResultIndex = result.Steps.Count;
                            var revision = workflowRevisions.TryGetValue(loopStep.Key, out var previousRevision)
                                ? previousRevision + 1
                                : 1;
                            workflowRevisions[loopStep.Key] = revision;
                            state = await ExecuteConfiguredWorkflowDefinitionAsync(
                                result,
                                request,
                                team,
                                loopStep,
                                baseUri,
                                participants,
                                bootstrap,
                                modelRoutes,
                                maxParallelModels,
                                keepAlive,
                                ollamaNumGpu,
                                maxContextTokens,
                                modelTimeoutSeconds,
                                leaderModel,
                                roleAssignments,
                                rolePairings,
                                state,
                                loopGroup,
                                loopIteration,
                                maximumIterations,
                                revision,
                                nextXRoundCause,
                                nextExecutionIsReconsideration,
                                cancellationToken).ConfigureAwait(false);
                            nextExecutionIsReconsideration = false;
                            nextXRoundCause = string.Empty;

                            var resolution = await ResolveConfiguredXDirectiveAsync(
                                result,
                                request,
                                team,
                                loopStep,
                                configuredSteps,
                                state,
                                baseUri,
                                participants,
                                bootstrap,
                                modelRoutes,
                                keepAlive,
                                ollamaNumGpu,
                                maxContextTokens,
                                modelTimeoutSeconds,
                                leaderModel,
                                cancellationToken).ConfigureAwait(false);
                            state = resolution.State;
                            if (resolution.StopWorkflow)
                                return state.FinalAnswer;
                            if (resolution.JumpStepIndex is int jumpIndex)
                            {
                                stepIndex = jumpIndex;
                                nextExecutionIsReconsideration = resolution.ReconsiderTarget;
                                nextXRoundCause = resolution.Cause;
                                xJumpedFromLoop = true;
                                break;
                            }

                            var configuredStepMarker = loopStep.LoopCompletionMarker?.Trim() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(configuredStepMarker) &&
                                string.Equals(configuredStepMarker, completionMarker, StringComparison.OrdinalIgnoreCase) &&
                                ContainsConfiguredLoopCompletionMarker(result.Steps.Skip(firstLoopStepResultIndex), completionMarker))
                            {
                                completionReportedThisIteration = true;
                            }
                        }

                        if (xJumpedFromLoop)
                            break;

                        if (!string.IsNullOrWhiteSpace(completionMarker) && completionReportedThisIteration)
                        {
                            completed = true;
                            request.ProgressMessage?.Invoke(
                                $"Workflow loop '{loopGroup}' completed after iteration {loopIteration}/{maximumIterations}; marker '{completionMarker}' was reported by the configured workflow.");
                            logger.LogInformation(
                                "Council run {RunId} completed configured workflow loop {LoopGroup} after {LoopIteration} of {MaximumIterations} iterations using marker {CompletionMarker}.",
                                result.RunId,
                                loopGroup,
                                loopIteration,
                                maximumIterations,
                                completionMarker);
                            break;
                        }
                    }

                    if (xJumpedFromLoop)
                        continue;

                    if (!string.IsNullOrWhiteSpace(completionMarker) && !completed)
                    {
                        var warning = $"Workflow loop '{loopGroup}' reached its safety limit of {maximumIterations} iteration(s) without completion marker '{completionMarker}'. The run stopped the loop instead of continuing indefinitely.";
                        result.Warnings.Add(warning);
                        request.ProgressMessage?.Invoke(warning);
                        logger.LogWarning(
                            "Council run {RunId} reached the configured safety limit for loop {LoopGroup} without marker {CompletionMarker}.",
                            result.RunId,
                            loopGroup,
                            completionMarker);
                    }

                    stepIndex += loopSteps.Count;
                }

                var configuredAnswer = !string.IsNullOrWhiteSpace(state.FinalAnswer)
                    ? state.FinalAnswer
                    : !string.IsNullOrWhiteSpace(state.FallbackAnswer)
                        ? state.FallbackAnswer
                        : "The configured council workflow completed without a substantive visible answer. Review the round prompts, role policies, selected models and local logs.";
                if (team.Key.StartsWith("adaptive-model-benchmark", StringComparison.OrdinalIgnoreCase))
                {
                    var completionNotice =
                        $"Council benchmark workflow `{result.RunId:N}` completed normally after {state.ExpandedStepIndex} configured round execution(s). " +
                        "The deterministic measurement/coverage evidence above remains part of the saved transcript.";
                    request.ProgressMessage?.Invoke(completionNotice);
                    configuredAnswer = $"{configuredAnswer.Trim()}{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}_{completionNotice}_";
                }
                return configuredAnswer;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} failed while executing configured workflow for team {TeamKey}.",
                    result.RunId,
                    team.Key);
                throw;
            }
        }

        /// <summary>Resolves one accepted X-Round directive after a configured workflow step completes.</summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="sourceDefinition">Source definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="configuredSteps">Council workflow step definition dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="state">State value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="leaderModel">Leader model value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The configured workflow execution state state int jump step index bool stop workflow bool reconsider target string cause produced by the operation.</returns>
        private async Task<(ConfiguredWorkflowExecutionState State, int? JumpStepIndex, bool StopWorkflow, bool ReconsiderTarget, string Cause)> ResolveConfiguredXDirectiveAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition sourceDefinition,
            IReadOnlyList<CouncilWorkflowStepDefinition> configuredSteps,
            ConfiguredWorkflowExecutionState state,
            string baseUri,
            IReadOnlyList<string> participants,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            string leaderModel,
            CancellationToken cancellationToken)
        {
            try
            {
                var directive = state.XDirective;
                if (directive is null)
                    return (state, null, false, false, string.Empty);

                var clearedState = state with { XDirective = null };
                switch (directive.Action)
                {
                    case CouncilXRoundAction.ReturnText:
                    {
                        var returned = string.IsNullOrWhiteSpace(directive.Text)
                            ? clearedState.FallbackAnswer
                            : directive.Text.Trim();
                        if (string.IsNullOrWhiteSpace(returned))
                        {
                            result.Warnings.Add($"X-Round text return from '{sourceDefinition.DisplayName}' was empty and therefore did not stop the workflow.");
                            return (clearedState, null, false, false, string.Empty);
                        }

                        request.ProgressMessage?.Invoke(
                            $"X-Round from '{sourceDefinition.DisplayName}' returned an explicit parent result. Earlier round revisions remain immutable in the transcript.");
                        return (clearedState with { FinalAnswer = returned, FallbackAnswer = returned }, null, true, false, directive.Reason);
                    }
                    case CouncilXRoundAction.ReconsiderStep:
                    case CouncilXRoundAction.ReexecuteStep:
                    {
                        var targetIndex = configuredSteps
                            .Select((step, index) => new { step, index })
                            .Where(item => string.Equals(item.step.Key, directive.TargetStepKey, StringComparison.OrdinalIgnoreCase))
                            .Select(item => (int?)item.index)
                            .FirstOrDefault();
                        if (targetIndex is null)
                        {
                            var warning = $"X-Round requested unknown workflow step '{directive.TargetStepKey}'. The current workflow continues instead of guessing a target.";
                            result.Warnings.Add(warning);
                            request.ProgressMessage?.Invoke(warning);
                            return (clearedState, null, false, false, string.Empty);
                        }

                        var sourceIndex = configuredSteps
                            .Select((step, index) => new { step, index })
                            .Where(item => string.Equals(item.step.Key, sourceDefinition.Key, StringComparison.OrdinalIgnoreCase))
                            .Select(item => (int?)item.index)
                            .FirstOrDefault();
                        if (sourceIndex is null || targetIndex.Value > sourceIndex.Value)
                        {
                            var warning = $"X-Round revisit from '{sourceDefinition.Key}' to later step '{directive.TargetStepKey}' was rejected. Revisit control may re-enter the current or an earlier step, but it cannot jump forward across workflow gates.";
                            result.Warnings.Add(warning);
                            request.ProgressMessage?.Invoke(warning);
                            return (clearedState, null, false, false, string.Empty);
                        }

                        var reconsider = directive.Action == CouncilXRoundAction.ReconsiderStep;
                        var cause = string.IsNullOrWhiteSpace(directive.Reason)
                            ? $"{sourceDefinition.Key} requested {directive.Action}."
                            : directive.Reason.Trim();
                        request.ProgressMessage?.Invoke(
                            $"X-Round {directive.Action} routes the workflow from '{sourceDefinition.Key}' to '{configuredSteps[targetIndex.Value].Key}'. " +
                            (reconsider
                                ? "This revision is reasoning-only and cannot repeat DX/organic side effects."
                                : "This revision deliberately re-executes the target's normal configured function policy."));
                        return (clearedState, targetIndex, false, reconsider, cause);
                    }
                    case CouncilXRoundAction.StartSingleModel:
                    {
                        var subtaskText = await RunXRoundSingleModelAsync(
                            result, request, sourceDefinition, directive, baseUri, participants, bootstrap, modelRoutes,
                            keepAlive, ollamaNumGpu, maxContextTokens, modelTimeoutSeconds, leaderModel, cancellationToken).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(subtaskText))
                            return (clearedState, null, false, false, string.Empty);
                        return (clearedState with { PreviousStep = subtaskText, FallbackAnswer = subtaskText }, null, false, false, directive.Reason);
                    }
                    case CouncilXRoundAction.StartCouncil:
                    {
                        var subtaskText = await RunXRoundChildCouncilAsync(
                            result, request, team, sourceDefinition, directive, cancellationToken).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(subtaskText))
                            return (clearedState, null, false, false, string.Empty);
                        return (clearedState with { PreviousStep = subtaskText, FallbackAnswer = subtaskText }, null, false, false, directive.Reason);
                    }
                    default:
                        return (clearedState, null, false, false, string.Empty);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed while resolving X-Round control from step {StepKey}.", result.RunId, sourceDefinition.Key);
                throw;
            }
        }

        /// <summary>Runs one selected parent-Council model as a bounded X-Function subtask and records the returned text as a separate immutable step.</summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="sourceDefinition">Source definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="directive">Directive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="leaderModel">Leader model value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        private async Task<string> RunXRoundSingleModelAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition sourceDefinition,
            CouncilXRoundDirective directive,
            string baseUri,
            IReadOnlyList<string> participants,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            string leaderModel,
            CancellationToken cancellationToken)
        {
            try
            {
                var requestedModel = directive.ModelName?.Trim() ?? string.Empty;
                var selectedModel = string.IsNullOrWhiteSpace(requestedModel)
                    ? leaderModel
                    : participants.FirstOrDefault(model =>
                        string.Equals(model, requestedModel, StringComparison.OrdinalIgnoreCase) ||
                        model.Contains($"— {requestedModel} @", StringComparison.OrdinalIgnoreCase) ||
                        model.EndsWith($"— {requestedModel}", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(selectedModel))
                {
                    var warning = $"X-Function requested single model '{requestedModel}', but that model is not already a member of this Council run. No substitute was chosen.";
                    result.Warnings.Add(warning);
                    request.ProgressMessage?.Invoke(warning);
                    return string.Empty;
                }

                var phase = $"X Function · single model · {sourceDefinition.Key}";
                var plan = modelRoutes.TryGetValue(selectedModel, out var configuredPlan)
                    ? configuredPlan
                    : new CouncilHardwareRoadPlan(
                        selectedModel, OneWireHardwareKind.Auto, -1, "Automatic", $"auto:{selectedModel}",
                        request.ResourceLoadPercent, request.MaxOutputTokens, maxContextTokens, ollamaNumGpu, 1);
                MultiModelCouncilStep? step;
                using (ambientContext.PushCouncil(result.RunId, directive.Round, phase))
                {
                    step = await RunParticipantAsync(
                        baseUri,
                        selectedModel,
                        participants,
                        directive.Round,
                        phase,
                        "X-Function derived single-model subtask",
                        directive.Prompt,
                        bootstrap,
                        plan.EffectiveMaxOutputTokens,
                        keepAlive,
                        plan.OllamaNumGpu,
                        plan.EffectiveMaxContextTokens,
                        modelTimeoutSeconds,
                        request.StreamUpdate,
                        cancellationToken,
                        allowRecovery: true,
                        fallbackPlan: plan,
                        progressMessage: request.ProgressMessage).ConfigureAwait(false);
                }

                if (step is null)
                    return string.Empty;
                step.WorkflowStepKey = sourceDefinition.Key;
                step.WorkflowRevision = 1;
                step.XRoundCause = directive.Reason;
                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                request.StepCompleted?.Invoke(step);
                request.ProgressMessage?.Invoke($"X-Function single-model subtask returned from {selectedModel}.");
                return step.VisibleContent?.Trim() ?? string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed while running an X-Function single-model subtask.", result.RunId);
                throw;
            }
        }

        /// <summary>Runs another configured Council team as a bounded child X-Function and records the child run identity plus returned text in the parent transcript.</summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="parentTeam">Parent team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="sourceDefinition">Source definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="directive">Directive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        private async Task<string> RunXRoundChildCouncilAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition parentTeam,
            CouncilWorkflowStepDefinition sourceDefinition,
            CouncilXRoundDirective directive,
            CancellationToken cancellationToken)
        {
            try
            {
                var teamKey = directive.TeamKey?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(teamKey))
                {
                    var warning = $"X-Function from '{sourceDefinition.DisplayName}' requested a child Council without a team key. Configure a default child team or provide teamKey.";
                    result.Warnings.Add(warning);
                    request.ProgressMessage?.Invoke(warning);
                    return string.Empty;
                }

                var maximumDepth = Math.Clamp(sourceDefinition.XMaximumChildCouncilDepth, 1, 10);
                if (request.XRoundChildDepth >= maximumDepth)
                {
                    var warning = $"X-Function child Council '{teamKey}' was not started because step '{sourceDefinition.DisplayName}' allows at most {maximumDepth} nested child-Council level(s).";
                    result.Warnings.Add(warning);
                    request.ProgressMessage?.Invoke(warning);
                    return string.Empty;
                }

                var childPrompt = string.IsNullOrWhiteSpace(directive.Prompt)
                    ? $"Derived Council task requested by parent team '{parentTeam.DisplayName}'. Return a concise text result to the parent Council."
                    : directive.Prompt.Trim();
                var childRequest = new MultiModelCouncilRequest
                {
                    RunId = Guid.NewGuid(),
                    Prompt = childPrompt,
                    ModelNames = request.ModelNames.ToList(),
                    ModelSelections = request.ModelSelections.ToList(),
                    UnavailableModelSelections = request.UnavailableModelSelections.ToList(),
                    BaseUri = request.BaseUri,
                    MaxRounds = request.MaxRounds,
                    MaxOutputTokens = request.MaxOutputTokens,
                    MaxParallelModels = request.MaxParallelModels,
                    AllowParallelHardwareRoads = request.AllowParallelHardwareRoads,
                    ResourceLoadPercent = request.ResourceLoadPercent,
                    ModelRoutes = request.ModelRoutes.ToList(),
                    MaxContextTokens = request.MaxContextTokens,
                    ModelTimeoutSeconds = request.ModelTimeoutSeconds,
                    OllamaKeepAlive = request.OllamaKeepAlive,
                    OllamaNumGpu = request.OllamaNumGpu,
                    IncludeMemory = request.IncludeMemory,
                    SaveToMemory = false,
                    GenerateImplementationArtifact = false,
                    UseChangeReviewWorkflow = request.UseChangeReviewWorkflow,
                    ProjectId = request.ProjectId,
                    ProjectTopicId = request.ProjectTopicId,
                    ProjectRevisionId = request.ProjectRevisionId,
                    CreateProjectForRun = false,
                    UseOrganicCouncilWorkflow = true,
                    CouncilTeamKey = teamKey,
                    CouncilLeaderModelName = request.CouncilLeaderModelName,
                    RequestedOrganicCapabilities = request.RequestedOrganicCapabilities.ToList(),
                    ExternalProjectContextJson = request.ExternalProjectContextJson,
                    XRoundChildDepth = request.XRoundChildDepth + 1,
                    ProgressMessage = message => request.ProgressMessage?.Invoke($"X child Council {teamKey}: {message}"),
                    StreamUpdate = request.StreamUpdate
                };
                request.ProgressMessage?.Invoke(
                    $"X-Function starting child Council team '{teamKey}' at depth {childRequest.XRoundChildDepth}/{maximumDepth}. The child has its own run identity and normal approval boundaries.");
                var childResult = await RunAsync(childRequest, cancellationToken).ConfigureAwait(false);
                var returned = childResult.FinalAnswer?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(returned))
                    returned = "The child Council completed without a visible text result.";

                var visible = $"### X-Function child Council · {teamKey}{Environment.NewLine}" +
                    $"Child run: `{childResult.RunId}`{Environment.NewLine}{Environment.NewLine}{returned}";
                var parentStep = new MultiModelCouncilStep
                {
                    Round = directive.Round,
                    Phase = $"X Function · child Council · {teamKey}",
                    ModelName = $"Council: {teamKey}",
                    CouncilMembers = result.ModelNames.ToList(),
                    Role = "X-Function derived Council",
                    Content = visible,
                    VisibleContent = visible,
                    StartedAtUtc = childResult.StartedAtUtc,
                    CompletedAtUtc = childResult.CompletedAtUtc ?? DateTime.UtcNow,
                    DurationSeconds = Math.Max(0, ((childResult.CompletedAtUtc ?? DateTime.UtcNow) - childResult.StartedAtUtc).TotalSeconds),
                    WorkflowStepKey = sourceDefinition.Key,
                    WorkflowRevision = 1,
                    XRoundCause = directive.Reason
                };
                MultiModelCouncilServiceAddOrderedStep(result, parentStep, logger);
                request.StepCompleted?.Invoke(parentStep);
                return returned;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed while running an X-Function child Council.", result.RunId);
                throw;
            }
        }

        /// <summary>Waits for the local human when the configured source step requires explicit approval of an X-Round control transition.</summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="sourceDefinition">Source definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="directive">Directive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private async Task<bool> WaitForXRoundApprovalAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition sourceDefinition,
            CouncilXRoundDirective directive,
            CancellationToken cancellationToken)
        {
            try
            {
                var fingerprintSource = $"{result.RunId:N}|{sourceDefinition.Key}|{directive.Id:N}|{directive.Action}|{directive.TargetStepKey}|{directive.TeamKey}|{directive.ModelName}";
                var fingerprint = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource))).ToLowerInvariant();
                var spec = new HumanApprovalRequestSpec(
                    CorrelationId: $"council:x:{result.RunId:N}:{directive.Id:N}",
                    OperationKey: $"council.x.{directive.Action.ToString().ToLowerInvariant()}",
                    Title: $"Approve X-Round {directive.Action} — {sourceDefinition.DisplayName}",
                    Description:
                        $"Council team '{team.DisplayName}' requested X-Round action {directive.Action} from step '{sourceDefinition.Key}'. " +
                        $"Reason: {(string.IsNullOrWhiteSpace(directive.Reason) ? "No reason supplied." : directive.Reason)} " +
                        "Approval changes workflow control only; consequential child/tool actions retain their own normal approval boundaries.",
                    RiskLevel: directive.Action == CouncilXRoundAction.ReexecuteStep ? "Medium" : "Low",
                    Source: nameof(MultiModelCouncilService),
                    RequestedBy: directive.RequestedBy,
                    RequestedRole: "Local owner",
                    CouncilRunId: result.RunId,
                    EarliestCouncilRound: directive.Round,
                    RequiredBeforeCompletion: true,
                    IsSensitive: false,
                    RequestKind: vocabulary.Get().HumanRequestApproval,
                    SuggestedResponsesText: "Approve\nDecline",
                    ResponsePrompt: "Approve or decline this exact X-Round workflow transition.",
                    PrefillText: string.Empty,
                    AllowFreeText: false,
                    ParameterFingerprint: fingerprint,
                    QuestionScope: "Council",
                    GateMode: "Completion",
                    TargetMembersText: string.Empty,
                    RequestedCouncilRound: directive.Round,
                    RequestedCouncilPhase: directive.Phase);

                var waitingNoticeAdded = false;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(spec, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (gate.IsAuthorized)
                    {
                        humanCollaboration.UpdateCouncilRun(result.RunId, directive.Round, directive.Phase);
                        return true;
                    }
                    if (gate.IsDeclined)
                    {
                        var warning = $"Local human declined X-Round action {directive.Action} from '{sourceDefinition.DisplayName}'. The workflow continues normally.";
                        result.Warnings.Add(warning);
                        request.ProgressMessage?.Invoke(warning);
                        return false;
                    }

                    if (!waitingNoticeAdded)
                    {
                        var visible = $"Council paused for approval of X-Round action **{directive.Action}** from `{sourceDefinition.Key}`. Use Approvals & team to approve or decline the exact transition.";
                        var waitingStep = new MultiModelCouncilStep
                        {
                            Round = directive.Round,
                            Phase = directive.Phase,
                            ModelName = "LocalGPT: X-Round approval",
                            CouncilMembers = result.ModelNames.ToList(),
                            Role = "Human X-Round gate",
                            Content = visible,
                            VisibleContent = visible,
                            StartedAtUtc = DateTime.UtcNow,
                            CompletedAtUtc = DateTime.UtcNow,
                            WorkflowStepKey = sourceDefinition.Key,
                            XRoundCause = directive.Reason
                        };
                        MultiModelCouncilServiceAddOrderedStep(result, waitingStep, logger);
                        request.StepCompleted?.Invoke(waitingStep);
                        waitingNoticeAdded = true;
                    }

                    humanCollaboration.UpdateCouncilRun(result.RunId, directive.Round, $"Awaiting X-Round approval: {directive.Action}", true);
                    var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    void HandleChanged() => changed.TrySetResult(true);
                    humanCollaboration.Changed += HandleChanged;
                    try
                    {
                        var fallback = Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        await Task.WhenAny(changed.Task, fallback).ConfigureAwait(false);
                    }
                    finally
                    {
                        humanCollaboration.Changed -= HandleChanged;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed while waiting for X-Round human approval.", result.RunId);
                throw;
            }
        }

    }
}
