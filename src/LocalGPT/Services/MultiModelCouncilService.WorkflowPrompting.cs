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
        /// Builds configured workflow previous step.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleAssignment">Role assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logicalRound">Logical round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="fullCouncilPreviousStep">Full council previous step value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredWorkflowPreviousStep(
            MultiModelCouncilResult result,
            CouncilWorkflowStepDefinition definition,
            CouncilRoleRuntimeAssignment roleAssignment,
            int logicalRound,
            string fullCouncilPreviousStep)
        {
            try
            {
                if (definition.TranscriptVisibility == CouncilTranscriptVisibilityMode.FullCouncil)
                    return fullCouncilPreviousStep;
                if (definition.TranscriptVisibility == CouncilTranscriptVisibilityMode.None)
                    return string.Empty;

                IEnumerable<MultiModelCouncilStep> visibleSteps = result.Steps;
                if (definition.TranscriptVisibility is CouncilTranscriptVisibilityMode.SameRole or CouncilTranscriptVisibilityMode.SameRoleCurrentRound)
                {
                    visibleSteps = visibleSteps.Where(step =>
                        string.Equals(step.Role, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase));
                }
                if (definition.TranscriptVisibility is CouncilTranscriptVisibilityMode.CurrentRound or CouncilTranscriptVisibilityMode.SameRoleCurrentRound)
                    visibleSteps = visibleSteps.Where(step => step.Round == logicalRound);

                return visibleSteps
                    .Where(step => string.IsNullOrWhiteSpace(step.Error) && !string.IsNullOrWhiteSpace(step.VisibleContent))
                    .OrderByDescending(step => step.SortOrder)
                    .Select(step => step.VisibleContent.Trim())
                    .FirstOrDefault() ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} could not resolve previous-step visibility {Visibility} for role {RoleName} and logical round {Round}.",
                    result.RunId,
                    definition.TranscriptVisibility,
                    roleAssignment.RoleName,
                    logicalRound);
                throw;
            }
        }

        /// <summary>
        /// Builds configured workflow transcript.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="team">Configured social team that controls whether explicit preflight evidence is visible to later model prompts.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleAssignment">Role assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logicalRound">Logical round value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredWorkflowTranscript(
            MultiModelCouncilResult result,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition definition,
            CouncilRoleRuntimeAssignment roleAssignment,
            int logicalRound)
        {
            try
            {
                if (!definition.IncludePriorTranscript || definition.TranscriptVisibility == CouncilTranscriptVisibilityMode.None)
                    return string.Empty;

                IEnumerable<MultiModelCouncilStep> visibleSteps = GetCouncilWorkflowContextSteps(result.Steps, team);
                visibleSteps = definition.TranscriptVisibility switch
                {
                    CouncilTranscriptVisibilityMode.SameRole => visibleSteps.Where(step =>
                        string.Equals(step.Role, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase)),
                    CouncilTranscriptVisibilityMode.CurrentRound => visibleSteps.Where(step => step.Round == logicalRound),
                    CouncilTranscriptVisibilityMode.SameRoleCurrentRound => visibleSteps.Where(step =>
                        step.Round == logicalRound &&
                        string.Equals(step.Role, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase)),
                    _ => visibleSteps
                };

                return councilText.MultiModelCouncilServiceBuildTranscript(visibleSteps.ToList(), logger);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} could not build transcript visibility {Visibility} for workflow role {RoleName} and logical round {Round}.",
                    result.RunId,
                    definition.TranscriptVisibility,
                    roleAssignment.RoleName,
                    logicalRound);
                throw;
            }
        }

        /// <summary>
        /// Performs contains configured loop completion marker as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="steps">Multi model council step dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="completionMarker">Completion marker value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool ContainsConfiguredLoopCompletionMarker(
            IEnumerable<MultiModelCouncilStep> steps,
            string completionMarker)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(completionMarker))
                    return false;
                return steps.Any(step =>
                    (!string.IsNullOrWhiteSpace(step.VisibleContent) && step.VisibleContent.Contains(completionMarker, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(step.Content) && step.Content.Contains(completionMarker, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to inspect configured workflow output for completion marker {CompletionMarker}.", completionMarker);
                throw;
            }
        }

        /// <summary>
        /// Performs run configured participant as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="roleAssignment">Role assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="rolePairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatIndex">Repeat index value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatCount">Repeat count value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopGroup">Loop group value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopIteration">Loop iteration value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopMaximumIterations">Loop maximum iterations value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="transcript">Transcript value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="previousStep">Previous step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="bootstrap">Bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="allowDxFunctions">Value indicating whether automatic/native functions may be exposed for this configured participant execution.</param>
        /// <param name="automaticFunctionPolicy">Resolved user-owned function policy containing the effective enabled state and optional exact allow-list.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task RunConfiguredParticipantAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            OrganicCouncilTeamDefinition team,
            string baseUri,
            IReadOnlyList<string> participants,
            CouncilRoleRuntimeAssignment roleAssignment,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            string modelName,
            int round,
            string phase,
            int repeatIndex,
            int repeatCount,
            string loopGroup,
            int loopIteration,
            int loopMaximumIterations,
            string transcript,
            string previousStep,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            bool allowDxFunctions,
            CouncilAutomaticFunctionPolicyResolution automaticFunctionPolicy,
            CancellationToken cancellationToken)
        {
            var activityKey = BuildCouncilParticipantActivityKey(round, phase, definition.Role, modelName);
            try
            {
                var plan = modelRoutes.TryGetValue(modelName, out var configuredPlan)
                    ? configuredPlan
                    : new CouncilHardwareRoadPlan(
                        modelName,
                        OneWireHardwareKind.Auto,
                        -1,
                        "Automatic",
                        $"auto:{modelName}",
                        request.ResourceLoadPercent,
                        request.MaxOutputTokens,
                        maxContextTokens,
                        ollamaNumGpu,
                        1);
                var routeLabel = $"{GetCouncilExecutionHostKey(modelName)} · {plan.LaneKey}";
                liveCouncilSessions.BeginParticipantActivity(result.RunId, activityKey, modelName, phase, definition.Role, routeLabel);
                liveCouncilSessions.SetParticipantActivityStatus(result.RunId, activityKey, $"Running on {routeLabel}.");

                // Configured single/sequential workflow steps must expose the same rich producer-side lane as
                // parallel Council phases. Keep that lane current on every provider fragment, but coalesce the
                // ordered DXAIChat copy so browser presentation can never become a prerequisite for model progress.
                var orderedPresentationBuffer = request.StreamUpdate is null ? null : new StringBuilder();
                Action<string> participantStreamUpdate = text =>
                {
                    if (string.IsNullOrEmpty(text))
                        return;

                    liveCouncilSessions.AppendParticipantActivity(result.RunId, activityKey, text);
                    if (orderedPresentationBuffer is null)
                        return;

                    orderedPresentationBuffer.Append(text);
                    if (orderedPresentationBuffer.Length < 8192)
                        return;

                    request.StreamUpdate!(orderedPresentationBuffer.ToString());
                    orderedPresentationBuffer.Clear();
                };

                MultiModelCouncilStep? participantStep;
                var roundSkipToken = runConfigurations.GetRoundCancellationToken(result.RunId, round, phase);
                try
                {
                    using var participantCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, roundSkipToken);
                    using (ambientContext.PushCouncil(result.RunId, round, phase))
                    {
                        participantStep = await RunParticipantAsync(
                            baseUri,
                            modelName,
                            participants,
                            round,
                            phase,
                            definition.Role,
                            RenderConfiguredWorkflowPrompt(
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
                                previousStep,
                                automaticFunctionPolicy),
                            bootstrap,
                            plan.EffectiveMaxOutputTokens,
                            keepAlive,
                            plan.OllamaNumGpu,
                            plan.EffectiveMaxContextTokens,
                            modelTimeoutSeconds,
                            participantStreamUpdate,
                            participantCancellation.Token,
                            fallbackPlan: plan,
                            progressMessage: request.ProgressMessage,
                            enableAutomaticTools: allowDxFunctions,
                            automaticFunctionAllowList: automaticFunctionPolicy.AutomaticFunctionAllowList,
                            roleComplianceRetryCount: definition.RoleComplianceRetryCount,
                            finalAnswerRecoveryEnabled: definition.FinalAnswerRecoveryEnabled,
                            finalAnswerRecoveryMaxOutputTokens: definition.FinalAnswerRecoveryMaxOutputTokens).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (roundSkipToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    participantStep = CreateRoundSkippedStep(modelName, participants, round, phase, definition.Role, plan);
                    liveCouncilSessions.CompleteParticipantActivity(
                        result.RunId,
                        activityKey,
                        "Participant was skipped because the current Council phase was cancelled.");
                }

                if (orderedPresentationBuffer is { Length: > 0 })
                {
                    request.StreamUpdate!(orderedPresentationBuffer.ToString());
                    orderedPresentationBuffer.Clear();
                }

                ArgumentNullException.ThrowIfNull(participantStep);
                liveCouncilSessions.SetParticipantActivityResult(result.RunId, activityKey, participantStep.VisibleContent);
                if (!roundSkipToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    liveCouncilSessions.CompleteParticipantActivity(
                        result.RunId,
                        activityKey,
                        string.IsNullOrWhiteSpace(participantStep.Error)
                            ? "Model completed. Its thinking, function activity and answer are already available in this live lane; the ordered transcript copy is synchronized separately."
                            : $"Model completed with an error: {participantStep.Error}");
                }

                await AddCouncilStepAsync(
                    result,
                    participantStep,
                    request.StepCompleted,
                    request.ProgressMessage,
                    allowDxFunctions,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                {
                    liveCouncilSessions.CompleteParticipantActivity(
                        result.RunId,
                        activityKey,
                        "Configured participant was cancelled before its Council step completed.");
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RunConfiguredParticipantAsync)} was canceled.");
                }
                else
                {
                    liveCouncilSessions.CompleteParticipantActivity(
                        result.RunId,
                        activityKey,
                        $"Configured participant failed before its Council step could complete: {__serviceMethodException.Message}");
                    logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RunConfiguredParticipantAsync)} failed.");
                }
                throw;
            }
        }

        /// <summary>
        /// Performs select configured workflow participant as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="executionMode">Execution mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="leaderModel">Leader model value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="expandedStepIndex">Expanded step index value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string SelectConfiguredWorkflowParticipant(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            string executionMode,
            IReadOnlyList<string> participants,
            string leaderModel,
            int expandedStepIndex)
        {
    try
    {
                if (executionMode == "RoundRobinSingle")
                {
                    var preferred = participants[expandedStepIndex % participants.Count];
                    return SelectHealthyParticipant(result, participants, preferred);
                }

                if (executionMode == "AssignedModelSingle")
                {
                    var assigned = participants.FirstOrDefault(model => string.Equals(model, definition.AssignedModelName, StringComparison.OrdinalIgnoreCase));
                    if (assigned is null)
                    {
                        throw new InvalidOperationException(
                            $"Configured round '{definition.DisplayName}' requires provider-qualified model '{definition.AssignedModelName}', but that exact model is not assigned to role '{definition.Role}' in this run. LocalGPT will not substitute another model or host.");
                    }

                    return SelectHealthyParticipant(result, participants, assigned);
                }

                var requestedLeader = participants.FirstOrDefault(model => string.Equals(model, request.CouncilLeaderModelName, StringComparison.OrdinalIgnoreCase));
                var roleLeader = participants.FirstOrDefault(model => string.Equals(model, leaderModel, StringComparison.OrdinalIgnoreCase));
                return SelectHealthyParticipant(result, participants, requestedLeader ?? roleLeader);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(SelectConfiguredWorkflowParticipant)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(SelectConfiguredWorkflowParticipant)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs render configured workflow prompt as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="roleAssignment">Role assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="rolePairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatIndex">Repeat index value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatCount">Repeat count value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopGroup">Loop group value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopIteration">Loop iteration value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopMaximumIterations">Loop maximum iterations value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="transcript">Transcript value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="previousStep">Previous step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="automaticFunctionPolicy">Resolved user-editable function exposure summarized to the model for this exact workflow step.</param>
        /// <returns>The string produced by the operation.</returns>
        private string RenderConfiguredWorkflowPrompt(
            CouncilWorkflowStepDefinition definition,
            OrganicCouncilTeamDefinition team,
            MultiModelCouncilRequest request,
            string modelName,
            IReadOnlyList<string> participants,
            CouncilRoleRuntimeAssignment roleAssignment,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            int round,
            int repeatIndex,
            int repeatCount,
            string loopGroup,
            int loopIteration,
            int loopMaximumIterations,
            string transcript,
            string previousStep,
            CouncilAutomaticFunctionPolicyResolution automaticFunctionPolicy)
        {
    try
    {
                var template = string.IsNullOrWhiteSpace(definition.PromptTemplate)
                    ? "Perform the current workflow step for {{TeamName}} as {{Role}} during {{Phase}}. Your assigned responsibility is: {{RoleResponsibility}}. Produce only the result required by this role and phase. Use the overall user request only as background context unless this role explicitly owns the final answer."
                    : definition.PromptTemplate;
                var hasUserPromptPlaceholder = template.Contains("{{UserPrompt}}", StringComparison.Ordinal);
                var hasTranscriptPlaceholder = template.Contains("{{Transcript}}", StringComparison.Ordinal);
                var roleSummary = string.Join(
                    Environment.NewLine,
                    team.Roles.Select(role =>
                        $"- {role.Role}: {role.Expertise}. Responsibility: {role.Responsibility}. " +
                        $"AI assignment: {DescribeConfiguredRoleAiPolicy(role)}. Human participation: {role.HumanParticipationMode}. " +
                        $"Performance: {role.PerformanceMode}. Boundary: {role.BoundaryMode}. Language: {role.LanguageMode}. " +
                        $"Runtime classes: {(role.RuntimeClassKeys.Count == 0 ? "none" : string.Join(", ", role.RuntimeClassKeys))}."));
                var boundedTranscript = transcript.Length <= 160000 ? transcript : transcript[^160000..];
                var boundedPreviousStep = previousStep.Length <= 80000 ? previousStep : previousStep[^80000..];
                var roleMembers = roleAssignment.AiParticipants.Count == 0
                    ? "No AI members; the role is performed by the human participant."
                    : string.Join(", ", roleAssignment.AiParticipants);
                var rolePeerMembers = roleAssignment.AiParticipants
                    .Where(participant => !string.Equals(participant, modelName, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var rolePeerMembersText = rolePeerMembers.Count == 0
                    ? "No other AI role members are assigned for this step."
                    : string.Join(", ", rolePeerMembers);
                var roleExpertise = roleAssignment.Definition?.Expertise ?? string.Empty;
                var roleResponsibility = roleAssignment.Definition?.Responsibility ?? string.Empty;
                var runtimeClasses = roleAssignment.Definition?.RuntimeClassKeys is { Count: > 0 } keys
                    ? string.Join(", ", keys)
                    : "No runtime classes are assigned to this role.";
                var performanceMode = roleAssignment.Definition?.PerformanceMode ?? CouncilRolePerformanceMode.TaskSpecialist;
                var boundaryMode = roleAssignment.Definition?.BoundaryMode ?? CouncilRoleBoundaryMode.Bounded;
                var languageMode = roleAssignment.Definition?.LanguageMode ?? CouncilRoleLanguageMode.ModelChoice;
                var performanceInstruction = BuildConfiguredRolePerformanceInstruction(performanceMode, modelName, roleAssignment.RoleName);
                var boundaryInstruction = BuildConfiguredRoleBoundaryInstruction(boundaryMode, roleAssignment.RoleName);
                var languageInstruction = BuildConfiguredRoleLanguageInstruction(languageMode);
                var humanParticipationInstruction = BuildConfiguredRoleHumanParticipationInstruction(roleAssignment.HumanParticipationMode);
                var participantPairings = rolePairings
                    .Where(pairing =>
                        string.Equals(pairing.RoleName, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pairing.Participant, modelName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var pairedParticipants = participantPairings.Count == 0
                    ? "No paired participant is assigned to this model."
                    : string.Join(", ", participantPairings.Select(pairing => $"{pairing.PairedRoleName}: {pairing.PairedParticipant}"));
                var pairedRole = participantPairings.Count == 0
                    ? roleAssignment.Definition?.PairedRole ?? string.Empty
                    : string.Join(", ", participantPairings.Select(pairing => pairing.PairedRoleName).Distinct(StringComparer.OrdinalIgnoreCase));
                var rolePairingSummary = BuildConfiguredRolePairingSummary(rolePairings);
                var rendered = template
                    .Replace("{{TeamName}}", team.DisplayName, StringComparison.Ordinal)
                    .Replace("{{TeamKey}}", team.Key, StringComparison.Ordinal)
                    .Replace("{{TeamPurpose}}", team.Purpose, StringComparison.Ordinal)
                    .Replace("{{Roles}}", roleSummary, StringComparison.Ordinal)
                    .Replace("{{UserPrompt}}", request.Prompt, StringComparison.Ordinal)
                    .Replace("{{ModelName}}", modelName, StringComparison.Ordinal)
                    .Replace("{{CouncilMembers}}", string.Join(", ", participants), StringComparison.Ordinal)
                    .Replace("{{RoleMembers}}", roleMembers, StringComparison.Ordinal)
                    .Replace("{{ExecutingRoleMember}}", modelName, StringComparison.Ordinal)
                    .Replace("{{RolePeerMembers}}", rolePeerMembersText, StringComparison.Ordinal)
                    .Replace("{{RoleAiSelection}}", roleAssignment.AiSelectionDescription, StringComparison.Ordinal)
                    .Replace("{{HumanParticipationMode}}", roleAssignment.HumanParticipationMode.ToString(), StringComparison.Ordinal)
                    .Replace("{{RolePerformanceMode}}", performanceMode.ToString(), StringComparison.Ordinal)
                    .Replace("{{RoleBoundaryMode}}", boundaryMode.ToString(), StringComparison.Ordinal)
                    .Replace("{{RoleLanguageMode}}", languageMode.ToString(), StringComparison.Ordinal)
                    .Replace("{{RolePerformanceInstruction}}", performanceInstruction, StringComparison.Ordinal)
                    .Replace("{{RoleBoundaryInstruction}}", boundaryInstruction, StringComparison.Ordinal)
                    .Replace("{{RoleLanguageInstruction}}", languageInstruction, StringComparison.Ordinal)
                    .Replace("{{HumanParticipationInstruction}}", humanParticipationInstruction, StringComparison.Ordinal)
                    .Replace("{{RoleExpertise}}", roleExpertise, StringComparison.Ordinal)
                    .Replace("{{RoleResponsibility}}", roleResponsibility, StringComparison.Ordinal)
                    .Replace("{{RuntimeClasses}}", runtimeClasses, StringComparison.Ordinal)
                    .Replace("{{PairedParticipant}}", pairedParticipants, StringComparison.Ordinal)
                    .Replace("{{PairedRole}}", pairedRole, StringComparison.Ordinal)
                    .Replace("{{RolePairings}}", rolePairingSummary, StringComparison.Ordinal)
                    .Replace("{{LoopGroup}}", loopGroup, StringComparison.Ordinal)
                    .Replace("{{LoopIteration}}", loopIteration.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{{LoopMaximumIterations}}", loopMaximumIterations.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{{Role}}", definition.Role, StringComparison.Ordinal)
                    .Replace("{{Phase}}", definition.Phase, StringComparison.Ordinal)
                    .Replace("{{RoundNumber}}", round.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{{RepeatIndex}}", (repeatIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{{RepeatCount}}", repeatCount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    .Replace("{{Transcript}}", boundedTranscript, StringComparison.Ordinal)
                    .Replace("{{PreviousStep}}", boundedPreviousStep, StringComparison.Ordinal)
                    .Replace("{{Preparation}}", boundedPreviousStep, StringComparison.Ordinal)
                    .Replace("{{ExternalProjectContextJson}}", request.ExternalProjectContextJson, StringComparison.Ordinal);

                var authoritativeRoleTask = rendered.Trim();
                var contextBuilder = new StringBuilder();
                if (!hasUserPromptPlaceholder)
                {
                    contextBuilder
                        .AppendLine("BACKGROUND USER REQUEST — CONTEXT ONLY")
                        .AppendLine("This explains the overall human goal. It is not your operative instruction for this turn. Do not solve, restart or redesign the overall request unless the CURRENT WORKFLOW ROLE TASK explicitly tells you to do so.")
                        .AppendLine(request.Prompt.Trim());
                }
                if (definition.IncludePriorTranscript && !hasTranscriptPlaceholder && !string.IsNullOrWhiteSpace(boundedTranscript))
                {
                    if (contextBuilder.Length > 0)
                        contextBuilder.AppendLine();
                    contextBuilder
                        .AppendLine("PRIOR COUNCIL EVIDENCE — INPUT ONLY")
                        .AppendLine("Use this only as evidence required by your current role task. Do not take over an earlier or later role because its text appears here.")
                        .AppendLine(boundedTranscript);
                }

                var assignmentBriefing = new StringBuilder()
                    .AppendLine("Runtime role assignment for this round:")
                    .Append("- Role: ").AppendLine(roleAssignment.RoleName)
                    .Append("- Executing provider-qualified model: ").AppendLine(modelName)
                    .Append("- Assigned AI role members: ").AppendLine(roleMembers)
                    .Append("- Other AI members in your current role: ").AppendLine(rolePeerMembersText)
                    .Append("- AI selection policy: ").AppendLine(roleAssignment.AiSelectionDescription)
                    .Append("- Human participation mode: ").AppendLine(roleAssignment.HumanParticipationMode.ToString())
                    .Append("- Role performance mode: ").AppendLine(performanceMode.ToString())
                    .Append("- Role boundary mode: ").AppendLine(boundaryMode.ToString())
                    .Append("- Role language mode: ").AppendLine(languageMode.ToString());
                if (!string.IsNullOrWhiteSpace(roleExpertise))
                    assignmentBriefing.Append("- Expertise/viewpoint: ").AppendLine(roleExpertise);
                if (!string.IsNullOrWhiteSpace(roleResponsibility))
                    assignmentBriefing.Append("- Responsibility: ").AppendLine(roleResponsibility);
                assignmentBriefing.Append("- Paired participant(s): ").AppendLine(pairedParticipants);
                assignmentBriefing.Append("- Runtime pairings: ").AppendLine(rolePairingSummary);
                if (!string.IsNullOrWhiteSpace(loopGroup))
                    assignmentBriefing.Append("- Loop: ").Append(loopGroup).Append(' ').Append(loopIteration).Append('/').AppendLine(loopMaximumIterations.ToString(System.Globalization.CultureInfo.InvariantCulture));
                assignmentBriefing.AppendLine("Treat the role assignment as workflow structure, not as proof that any participant's answer is correct.");
                assignmentBriefing.AppendLine("The executing model-to-role binding is authoritative for this workflow step. Do not switch identity, impersonate another role, or substitute another model/host.");
                assignmentBriefing.AppendLine("Model names mentioned in the user request, benchmark targets, prior transcript, tool arguments, or another role's output are task data unless they are also listed above under Assigned AI role members. Do not mistake those names for your current role teammates.");
                assignmentBriefing.AppendLine("When discussing another current role member, use its provider-qualified identity from Assigned AI role members so same-name models on different hosts stay distinct.");
                assignmentBriefing.Append("- Performance instruction: ").AppendLine(performanceInstruction);
                assignmentBriefing.Append("- Boundary instruction: ").AppendLine(boundaryInstruction);
                assignmentBriefing.Append("- Language instruction: ").AppendLine(languageInstruction);
                assignmentBriefing.Append("- Human-turn instruction: ").AppendLine(humanParticipationInstruction);
                assignmentBriefing.Append("- Knowledge grounding: ").AppendLine(
                    !string.IsNullOrWhiteSpace(request.ExternalProjectContextJson) || team.PreferredCapabilities.Any(item => item.Contains("knowledge", StringComparison.OrdinalIgnoreCase))
                        ? "LocalGPT knowledge/project context is relevant to this team. Consult authoritative supplied/retrieved local evidence when it materially improves correctness; do not make ceremonial retrieval calls when the assigned task is already self-contained."
                        : "Use supplied local/project evidence when present. Pretrained knowledge is allowed, but authoritative local evidence wins when the two conflict.");
                assignmentBriefing.Append("- Organic/DX tool availability for this step: ")
                    .Append(automaticFunctionPolicy.Description)
                    .AppendLine(". Call an available tool only when it materially improves grounding or is required to complete this role task.");
                if (team.PreferredCapabilities.Count > 0)
                    assignmentBriefing.Append("- Team preferred capabilities: ").AppendLine(string.Join(", ", team.PreferredCapabilities));
                if (!string.IsNullOrWhiteSpace(request.ExternalProjectContextJson))
                    assignmentBriefing.AppendLine("- External project knowledge/context: supplied for this request; prefer authoritative local project evidence over conflicting pretrained assumptions.");
                if (definition.XFunctionsEnabled)
                {
                    assignmentBriefing.AppendLine("- X-Round control: this step may use only the X actions explicitly enabled in Council Teams, and every control request must state a concrete reason.");
                    assignmentBriefing.Append("- X revisit: ").AppendLine(definition.XCanRevisit ? "enabled through council.x.revisit; reconsider is reasoning-only while reexecute deliberately permits the target step's normal function policy." : "disabled.");
                    assignmentBriefing.Append("- X text return: ").AppendLine(definition.XCanReturnText ? "enabled through council.x.return_text." : "disabled.");
                    assignmentBriefing.Append("- X single model: ").AppendLine(definition.XCanStartSingleModel ? "enabled through council.x.start_single_model; the requested model must already belong to this Council." : "disabled.");
                    assignmentBriefing.Append("- X child Council: ").AppendLine(definition.XCanStartCouncil ? "enabled through council.x.start_council; the child keeps an independent run identity." : "disabled.");
                    assignmentBriefing.Append("- X transition budget: ").Append(Math.Max(1, definition.XMaximumTransitions)).AppendLine(" accepted request(s) from this source step per run.");
                    if (definition.XRequiresHumanApproval)
                        assignmentBriefing.AppendLine("- X human gate: every accepted X control transition pauses for explicit local-human approval before control flow changes.");
                    if (!string.IsNullOrWhiteSpace(definition.XDefaultTargetStepKey))
                        assignmentBriefing.Append("- Default X revisit target: ").AppendLine(definition.XDefaultTargetStepKey);
                    assignmentBriefing.AppendLine("- X history contract: never pretend an earlier round disappeared. Revisited steps create a new revision while prior outputs remain immutable evidence.");
                }
                if (definition.ProducesFinalAnswer)
                    assignmentBriefing.AppendLine("- Final-output contract: answer the human in normal prose/Markdown. Do not make raw JSON, work-order metadata, or tool parameters the final answer unless the human explicitly asked for JSON.");
                if (councilRuntime.MultiModelCouncilServiceHasExplicitArtifactIntent(request.Prompt, logger))
                    assignmentBriefing.AppendLine("- Coding-output contract: the visible answer must include concrete source/code, file paths, or an actual approved artifact result appropriate to the request. Internal machine-readable JSON may follow only as LocalGPT metadata and must not replace the requested source.");

                var finalPrompt = new StringBuilder()
                    .AppendLine(assignmentBriefing.ToString().Trim())
                    .AppendLine()
                    .AppendLine("CURRENT WORKFLOW ROLE TASK — AUTHORITATIVE")
                    .Append("Step: ").Append(definition.DisplayName).Append(" / ").AppendLine(definition.Phase)
                    .Append("Role: ").AppendLine(roleAssignment.RoleName)
                    .AppendLine(authoritativeRoleTask);
                if (contextBuilder.Length > 0)
                    finalPrompt.AppendLine().AppendLine(contextBuilder.ToString().Trim());
                finalPrompt
                    .AppendLine()
                    .AppendLine("EXECUTION PRIORITY")
                    .AppendLine("Perform only the CURRENT WORKFLOW ROLE TASK now. The original user request and prior Council text are background evidence unless this role task explicitly asks you to act on them. Do not perform another role's task, do not redesign the workflow, and do not answer the overall user request in place of your assigned role output.")
                    .AppendLine("ROLE COMPLIANCE: being an AI model is not a reason to decline an assigned reasoning/text/code-analysis role. Make the best bounded attempt with the information and capabilities actually available. Ask for human input only when a genuinely missing decision/fact is required by this role; do not use a question or capability disclaimer as a substitute for doing the assigned work.");
                return finalPrompt.ToString().Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RenderConfiguredWorkflowPrompt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RenderConfiguredWorkflowPrompt)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds bounded provider-qualified evidence for one configured role coordination turn.
        /// </summary>
        /// <param name="steps">Council steps whose visible outputs should be supplied as role evidence.</param>
        /// <returns>Provider-qualified role evidence suitable for a peer-review or synthesis prompt.</returns>
        private string BuildConfiguredRoleEvidence(IReadOnlyList<MultiModelCouncilStep> steps)
        {
            try
            {
                var usable = steps
                    .Where(step =>
                        string.IsNullOrWhiteSpace(step.Error) &&
                        !string.IsNullOrWhiteSpace(step.VisibleContent) &&
                        !IsRoundSkippedStep(step))
                    .ToList();
                if (usable.Count == 0)
                    return string.Empty;

                const int perMemberLimit = 48000;
                const int totalLimit = 160000;
                var builder = new StringBuilder();
                foreach (var step in usable)
                {
                    var content = step.VisibleContent.Trim();
                    if (content.Length > perMemberLimit)
                        content = content[^perMemberLimit..];
                    builder
                        .Append("### ").Append(step.ModelName).Append(" — ").AppendLine(step.Role)
                        .AppendLine(content)
                        .AppendLine();
                    if (builder.Length >= totalLimit)
                        break;
                }

                var evidence = builder.ToString().Trim();
                return evidence.Length <= totalLimit ? evidence : evidence[^totalLimit..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build configured role coordination evidence.");
                throw;
            }
        }

        /// <summary>
        /// Builds the optional same-role peer usefulness and voting prompt without authorizing function execution.
        /// </summary>
        /// <param name="team">Configured team that owns the workflow step.</param>
        /// <param name="request">Council request containing the original user goal.</param>
        /// <param name="definition">Workflow step whose role members are being reviewed.</param>
        /// <param name="roleAssignment">Runtime role assignment for the current run.</param>
        /// <param name="reviewerModelName">Provider-qualified role member performing this peer review.</param>
        /// <param name="primaryRoleSteps">Usable primary AI answers produced by the current role.</param>
        /// <param name="roleEvidence">Bounded primary role-member evidence.</param>
        /// <returns>A prompt that requires explicit provider-qualified peer usefulness feedback and one role vote.</returns>
        private string BuildConfiguredRolePeerReviewPrompt(
            OrganicCouncilTeamDefinition team,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            CouncilRoleRuntimeAssignment roleAssignment,
            string reviewerModelName,
            IReadOnlyList<MultiModelCouncilStep> primaryRoleSteps,
            string roleEvidence)
        {
            try
            {
                var peers = primaryRoleSteps
                    .Select(step => step.ModelName)
                    .Where(model => !string.Equals(model, reviewerModelName, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var peerList = peers.Count == 0 ? "none" : string.Join(Environment.NewLine, peers.Select(peer => $"- {peer}"));
                var expertise = roleAssignment.Definition?.Expertise ?? string.Empty;
                var responsibility = roleAssignment.Definition?.Responsibility ?? string.Empty;

                return $"""
                    You are {reviewerModelName}, one provider-qualified member of role "{roleAssignment.RoleName}" in Council team "{team.DisplayName}".
                    This is an optional SAME-ROLE coordination turn after the normal role-member answers. Do not call functions or repeat side effects. Do not redo another role's work.

                    Original user request:
                    {request.Prompt}

                    Current workflow step: {definition.DisplayName} / {definition.Phase}
                    Current role: {roleAssignment.RoleName}
                    Role expertise: {expertise}
                    Role responsibility: {responsibility}
                    Your exact identity: {reviewerModelName}
                    Other AI members of THIS role that you must review:
                    {peerList}

                    Identity rule:
                    Names appearing in the user request, benchmark candidate lists, tool arguments, earlier-role outputs, or the transcript are task SUBJECTS unless they exactly match the provider-qualified role members listed above. Never call a benchmark target or another role's model your teammate merely because its name appears in the evidence.

                    Primary role-member results:
                    {roleEvidence}

                    Review every OTHER role member, not yourself. For each peer, output exactly one concise line in this shape:
                    Peer usefulness — <exact provider-qualified peer identity>: <0-100>% — useful: <what materially helped> — correction: <what is wrong, missing, risky, or "none">

                    Then output exactly one vote line choosing the strongest CURRENT-ROLE result:
                    Role vote: <exact provider-qualified role member identity>

                    Base the percentage and vote on correctness, relevance to this role, evidence, complementarity, and usefulness for the next workflow step. Disagreement is allowed. Do not invent peer identities.
                    """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build configured role peer-review prompt for role {RoleName} and reviewer {ReviewerModelName}.", roleAssignment.RoleName, reviewerModelName);
                throw;
            }
        }

    }
}
