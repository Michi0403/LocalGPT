using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Returns text-gateway function evidence to the same Council member for a bounded number of continuation turns.
        /// Native provider tools already continue inside their provider turn; this path covers recovered textual DXFunction calls.
        /// </summary>
        /// <param name="result">Council run that owns the continuation evidence.</param>
        /// <param name="sourceStep">Model step that requested the first textual function call.</param>
        /// <param name="functionSteps">Intermediate function-result evidence produced for the source step.</param>
        /// <param name="baseUri">Configured provider base URI.</param>
        /// <param name="councilMembers">Provider-qualified members visible to this Council turn.</param>
        /// <param name="originalPrompt">Original substantive task prompt for the configured step.</param>
        /// <param name="bootstrap">Current Council bootstrap context.</param>
        /// <param name="fallbackPlan">Hardware-road plan already selected for this member.</param>
        /// <param name="keepAlive">Provider keep-alive policy.</param>
        /// <param name="modelTimeoutSeconds">Bounded provider timeout.</param>
        /// <param name="progressMessage">Optional visible progress callback.</param>
        /// <param name="streamUpdate">Optional visible streaming callback.</param>
        /// <param name="stepCompleted">Optional completed-step callback.</param>
        /// <param name="automaticFunctionAllowList">Effective registered-function allow-list.</param>
        /// <param name="roleComplianceRetryCount">Configured same-member role retry count.</param>
        /// <param name="finalAnswerRecoveryEnabled">Whether final-answer recovery is enabled.</param>
        /// <param name="finalAnswerRecoveryMaxOutputTokens">Maximum output tokens for final-answer recovery.</param>
        /// <param name="continuationMode">Configured function-result continuation mode.</param>
        /// <param name="maximumContinuationRounds">Maximum bounded continuation turns.</param>
        /// <param name="cancellationToken">Cancellation token for the Council run.</param>
        /// <returns>A task that completes after the bounded continuation has stopped.</returns>
        private async Task ContinueAfterDxFunctionResultsAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilStep sourceStep,
            IReadOnlyList<MultiModelCouncilStep> functionSteps,
            string baseUri,
            IReadOnlyList<string> councilMembers,
            string originalPrompt,
            string bootstrap,
            CouncilHardwareRoadPlan fallbackPlan,
            string keepAlive,
            int modelTimeoutSeconds,
            Action<string>? progressMessage,
            Action<string>? streamUpdate,
            Action<MultiModelCouncilStep>? stepCompleted,
            IReadOnlyCollection<string>? automaticFunctionAllowList,
            int roleComplianceRetryCount,
            bool finalAnswerRecoveryEnabled,
            int finalAnswerRecoveryMaxOutputTokens,
            CouncilToolResultContinuationMode continuationMode,
            int maximumContinuationRounds,
            CancellationToken cancellationToken)
        {
            try
            {
                if (continuationMode != CouncilToolResultContinuationMode.SameMemberBounded ||
                    maximumContinuationRounds <= 0 ||
                    functionSteps.Count == 0 ||
                    string.IsNullOrWhiteSpace(sourceStep.ModelName))
                {
                    return;
                }

                var pendingFunctionSteps = functionSteps;
                var boundedRounds = Math.Clamp(maximumContinuationRounds, 1, 6);
                for (var continuationIndex = 0; continuationIndex < boundedRounds && pendingFunctionSteps.Count > 0; continuationIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var continuationNumber = continuationIndex + 1;
                    var continuationPhase = $"{sourceStep.Phase} · tool continuation {continuationNumber}";
                    var evidence = BuildToolResultContinuationEvidence(pendingFunctionSteps);
                    progressMessage?.Invoke(
                        $"Council is returning {pendingFunctionSteps.Count} intermediate function result(s) to {sourceStep.ModelName} for bounded continuation {continuationNumber}/{boundedRounds}.");

                    var continuationPrompt = $"""
Continue the exact original task. LocalGPT executed the registered function request(s) from your preceding turn and returned the intermediate evidence below.

The function evidence is data for your reasoning, not the final user-facing answer. Analyze it and continue the original task. If more current facts or another registered function are genuinely required, request the next function call. Otherwise produce the substantive answer, artifact plan, or next workflow result instead of returning only a raw function payload.

If evidence reports HumanApprovalPending, the exact approval card has already been queued. Do not claim the consequential action ran, do not fabricate userConfirmed, and do not ask the human to type permission again. Continue any independent work that does not depend on the pending action.

Original task for this workflow step:
{originalPrompt}

Intermediate LocalGPT function evidence:
{evidence}
""";

                    var continuationStep = await RunParticipantAsync(
                        baseUri,
                        sourceStep.ModelName,
                        councilMembers,
                        sourceStep.Round,
                        continuationPhase,
                        sourceStep.Role,
                        continuationPrompt,
                        bootstrap,
                        fallbackPlan.EffectiveMaxOutputTokens,
                        keepAlive,
                        fallbackPlan.OllamaNumGpu,
                        fallbackPlan.EffectiveMaxContextTokens,
                        modelTimeoutSeconds,
                        streamUpdate,
                        cancellationToken,
                        fallbackPlan: fallbackPlan,
                        progressMessage: progressMessage,
                        enableAutomaticTools: true,
                        automaticFunctionAllowList: automaticFunctionAllowList,
                        roleComplianceRetryCount: roleComplianceRetryCount,
                        finalAnswerRecoveryEnabled: finalAnswerRecoveryEnabled,
                        finalAnswerRecoveryMaxOutputTokens: finalAnswerRecoveryMaxOutputTokens).ConfigureAwait(false);

                    ArgumentNullException.ThrowIfNull(continuationStep);
                    pendingFunctionSteps = await AddCouncilStepAsync(
                        result,
                        continuationStep,
                        stepCompleted,
                        progressMessage,
                        allowDxFunctions: true,
                        cancellationToken).ConfigureAwait(false);
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
                    "Council function-result continuation failed for {ModelName} in round {Round}, phase {Phase}.",
                    sourceStep.ModelName,
                    sourceStep.Round,
                    sourceStep.Phase);
                throw;
            }
        }

        /// <summary>Builds bounded, explicitly untrusted function-result evidence for one continuation prompt.</summary>
        /// <param name="functionSteps">Function result steps returned by the registered gateway.</param>
        /// <returns>A bounded evidence block suitable for the next model turn.</returns>
        private string BuildToolResultContinuationEvidence(IReadOnlyList<MultiModelCouncilStep> functionSteps)
        {
            try
            {
                const int maximumEvidenceCharacters = 24000;
                var evidence = string.Join(
                    $"{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}",
                    functionSteps.Select(step =>
                        $"{step.Role}{Environment.NewLine}{(string.IsNullOrWhiteSpace(step.VisibleContent) ? step.Content : step.VisibleContent)}"));
                return evidence.Length <= maximumEvidenceCharacters
                    ? evidence
                    : evidence[..maximumEvidenceCharacters] + $"{Environment.NewLine}[Function evidence truncated by LocalGPT continuation boundary.]";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council function-result evidence could not be prepared for continuation.");
                throw;
            }
        }
    }
}
