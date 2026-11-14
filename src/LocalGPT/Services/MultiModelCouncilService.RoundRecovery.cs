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
        /// Repairs failed required member slots in one configured Social Team round without discarding the original failure evidence.
        /// </summary>
        /// <remarks>
        /// The participant's low-level safe retry has already run before this method is reached. Round recovery is a separate,
        /// persisted workflow policy. Each attempt is decided by goal execution's recovery helper (<see cref="IGoalRecoveryHelperService"/>):
        /// the role's configured members come first, then other members of the same run when they are suitable. No member, provider
        /// or AI host outside the current run is ever used; paired-role substitution waits for the local user's approval.
        /// </remarks>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="executionMode">Execution mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="baseUri">Base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="roleAssignment">Role assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleAssignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="rolePairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="roleParticipants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatIndex">Repeat index value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatCount">Repeat count value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopGroup">Loop group value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopIteration">Loop iteration value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="loopMaximumIterations">Loop maximum iterations value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="visiblePreviousStep">Visible previous step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="heartbeatBootstrap">Heartbeat bootstrap value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelRoutes">Council hardware road plan dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="allowDxFunctions">Value indicating whether allow DevExpress functions should apply to this operation.</param>
        /// <param name="automaticFunctionPolicy">Automatic function policy value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="workflowRevision">Workflow revision value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="xRoundCause">X round cause value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilStep>> RecoverConfiguredRoundMemberFailuresAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition definition,
            string executionMode,
            string baseUri,
            IReadOnlyList<string> participants,
            CouncilRoleRuntimeAssignment roleAssignment,
            IDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            IReadOnlyList<string> roleParticipants,
            int round,
            string phase,
            int repeatIndex,
            int repeatCount,
            string loopGroup,
            int loopIteration,
            int loopMaximumIterations,
            string visiblePreviousStep,
            string heartbeatBootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            bool allowDxFunctions,
            CouncilAutomaticFunctionPolicyResolution automaticFunctionPolicy,
            int workflowRevision,
            string xRoundCause,
            CancellationToken cancellationToken)
        {
            try
            {
                if (definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.Disabled ||
                    definition.MemberFailureRecoveryAttempts <= 0 ||
                    roleParticipants.Count == 0 ||
                    string.Equals(executionMode, "SystemBenchmarkCalibration", StringComparison.Ordinal) ||
                    string.Equals(executionMode, "SystemGameEngineResolution", StringComparison.Ordinal))
                {
                    return [];
                }

                var primarySteps = result.Steps
                    .Where(step =>
                        step.Round == round &&
                        string.Equals(step.Phase, phase, StringComparison.Ordinal) &&
                        roleParticipants.Contains(step.ModelName, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                // An explicit human "skip current round" is authoritative and must never be undone by automatic recovery.
                if (primarySteps.Any(IsRoundSkippedStep))
                    return [];

                var allMemberExecution = executionMode is "AllMembersParallel" or "AllMembersSequentialOnEachAIHostParallel" or "AllMembersSequential";
                var failedSlots = new List<string>();
                if (allMemberExecution)
                {
                    foreach (var intendedModel in roleParticipants)
                    {
                        var matching = primarySteps.FirstOrDefault(step =>
                            string.Equals(step.ModelName, intendedModel, StringComparison.OrdinalIgnoreCase));
                        if (!IsUsableConfiguredRoundStep(matching))
                            failedSlots.Add(intendedModel);
                    }
                }
                else if (!primarySteps.Any(IsUsableConfiguredRoundStep))
                {
                    var failedModel = primarySteps.LastOrDefault()?.ModelName;
                    failedSlots.Add(string.IsNullOrWhiteSpace(failedModel) ? roleParticipants[0] : failedModel);
                }

                if (failedSlots.Count == 0)
                    return [];

                var recoverySteps = new List<MultiModelCouncilStep>();
                var recoveryPool = BuildConfiguredRoundRecoveryPool(definition, executionMode, participants, roleAssignment, roleAssignments);
                var maximumAttempts = Math.Clamp(
                    definition.MemberFailureRecoveryAttempts,
                    0,
                    runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson").MaximumMemberRecoveryAttempts);
                var sameMemberOnly =
                    definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.RetrySameMember ||
                    string.Equals(executionMode, "AssignedModelSingle", StringComparison.Ordinal);
                var isPairedRole =
                    !string.IsNullOrWhiteSpace(roleAssignment.Definition?.PairedRole) ||
                    team.Roles.Any(role => string.Equals(role.PairedRole, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase)) ||
                    rolePairings.Any(pairing => string.Equals(pairing.RoleName, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase));
                for (var slotIndex = 0; slotIndex < failedSlots.Count; slotIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var failedModel = failedSlots[slotIndex];
                    var failedStep = primarySteps.LastOrDefault(step =>
                        string.Equals(step.ModelName, failedModel, StringComparison.OrdinalIgnoreCase));
                    var failureKind = failedStep is null
                        ? GoalMemberFailureKind.NoStep
                        : failedStep.HostUnreachable
                            ? GoalMemberFailureKind.HostUnreachable
                            : failedStep.RepetitionWatchdogStopped
                                ? GoalMemberFailureKind.RepetitionWatchdog
                                : string.IsNullOrWhiteSpace(failedStep.Error)
                                    ? GoalMemberFailureKind.EmptyAnswer
                                    : GoalMemberFailureKind.ProviderFailure;
                    var attemptedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    // RunParticipantAsync already exhausted its same-member safe fallback, except after a repetition-watchdog stop:
                    // that path never retries, so the configured "same member, then role pool" order starts with the same member.
                    // A member whose AI host is unavailable in this run is not retried on that host.
                    if ((definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool
                         && failureKind != GoalMemberFailureKind.RepetitionWatchdog)
                        || failureKind == GoalMemberFailureKind.HostUnreachable)
                        attemptedModels.Add(failedModel);
                    var hostKeys = participants
                        .Append(failedModel)
                        .Where(member => !string.IsNullOrWhiteSpace(member))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(member => member, member => GetCouncilExecutionHostKey(member), StringComparer.OrdinalIgnoreCase);
                    if (failureKind == GoalMemberFailureKind.HostUnreachable
                        && hostKeys.TryGetValue(failedModel, out var unreachableHost))
                    {
                        var hostWarning = goalRecovery.BuildHostDownWarning(unreachableHost);
                        if (!string.IsNullOrWhiteSpace(hostWarning) && !result.Warnings.Contains(hostWarning))
                            result.Warnings.Add(hostWarning);
                    }

                    var failedPairing = rolePairings.FirstOrDefault(pairing =>
                        string.Equals(pairing.RoleName, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pairing.Participant, failedModel, StringComparison.OrdinalIgnoreCase));
                    logger.LogInformation(
                        "Council round recovery for run {RunId}, step {StepKey}, round {Round}: member {FailedModel} has no usable answer ({FailureKind}); policy {RecoveryMode}, up to {Attempts} attempt(s), paired role {IsPairedRole}.",
                        result.RunId,
                        definition.Key,
                        round,
                        failedModel,
                        failureKind,
                        definition.MemberFailureRecoveryMode,
                        maximumAttempts,
                        isPairedRole);
                    var recovered = false;
                    var leftEmptyByDecision = false;
                    // The tools (automaticFunctionPolicy) and budget (context, timeout) are identical for every attempt of this slot,
                    // so only the model can differ. Repeating an empty answer with the same model changes nothing.
                    var previousAttemptModel = failedModel;
                    var previousAttemptEmpty = failureKind == GoalMemberFailureKind.EmptyAnswer;
                    var identicalRetries = 0;

                    for (var attemptIndex = 0; attemptIndex < maximumAttempts; attemptIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var decision = await goalRecovery.DecideMemberRecoveryAsync(new GoalMemberRecoveryContext
                        {
                            RunId = result.RunId,
                            TeamKey = team.Key,
                            WorkflowStepKey = definition.Key,
                            RoleName = roleAssignment.RoleName,
                            Round = round,
                            FailedModel = failedModel,
                            FailureKind = failureKind,
                            RecoveryMode = definition.MemberFailureRecoveryMode,
                            SameMemberOnly = sameMemberOnly,
                            AttemptNumber = attemptIndex + 1,
                            RunParticipants = participants,
                            RolePool = OrderParticipantsByObservedHealth(result, recoveryPool.Pool).ToList(),
                            ReservedByOtherRoles = recoveryPool.Reserved,
                            AttemptedModels = attemptedModels.ToList(),
                            HostKeys = hostKeys,
                            IsPairedRole = isPairedRole,
                            PairedRoleName = failedPairing?.PairedRoleName ?? roleAssignment.Definition?.PairedRole ?? string.Empty,
                            PairedParticipant = failedPairing?.PairedParticipant ?? string.Empty,
                            RunSteps = result.Steps.ToList()
                        }, cancellationToken).ConfigureAwait(false);

                        if (decision.Action == GoalMemberRecoveryAction.SkipSlot || string.IsNullOrWhiteSpace(decision.ModelName))
                        {
                            leftEmptyByDecision = true;
                            result.Warnings.Add($"Configured round '{definition.DisplayName}': {decision.Reason}.");
                            request.ProgressMessage?.Invoke($"Configured round '{definition.DisplayName}': {decision.Reason}.");
                            break;
                        }

                        var recoveryModel = decision.ModelName;
                        if (previousAttemptEmpty && string.Equals(recoveryModel, previousAttemptModel, StringComparison.OrdinalIgnoreCase))
                        {
                            if (identicalRetries >= 1)
                            {
                                logger.LogWarning(
                                    "Council round recovery for run {RunId}, step {StepKey}: member {FailedModel} returned an empty answer again and the next attempt would use the same model with the same tools and budget; nothing differs between attempts, so recovery stops after {Retries} identical retry.",
                                    result.RunId, definition.Key, failedModel, identicalRetries);
                                result.Warnings.Add($"Configured round '{definition.DisplayName}': recovery stopped because the next attempt would repeat the same model, tools and budget that already returned an empty answer.");
                                break;
                            }
                            identicalRetries++;
                        }
                        attemptedModels.Add(recoveryModel);
                        var attemptPairings = decision.SubstitutesPairedSlot
                            ? BuildSubstitutedRolePairings(rolePairings, roleAssignment.RoleName, failedModel, recoveryModel)
                            : rolePairings;

                        var recoveryPhase = $"{phase} · automatic member recovery {slotIndex + 1}.{attemptIndex + 1}";
                        request.ProgressMessage?.Invoke(
                            $"Configured round '{definition.DisplayName}' is repairing failed required member work from {failedModel} with {recoveryModel} (recovery {attemptIndex + 1}/{maximumAttempts}): {decision.Reason}. The failed attempt remains preserved in Council evidence.");

                        var transcript = BuildConfiguredWorkflowTranscript(result, team, definition, roleAssignment, round);
                        await RunConfiguredParticipantAsync(
                            result,
                            request,
                            definition,
                            team,
                            baseUri,
                            participants,
                            roleAssignment,
                            attemptPairings,
                            recoveryModel,
                            round,
                            recoveryPhase,
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
                            allowDxFunctions,
                            automaticFunctionPolicy,
                            cancellationToken).ConfigureAwait(false);

                        var recoveryStep = result.Steps.LastOrDefault(step =>
                            step.Round == round &&
                            string.Equals(step.Phase, recoveryPhase, StringComparison.Ordinal) &&
                            string.Equals(step.ModelName, recoveryModel, StringComparison.OrdinalIgnoreCase));
                        if (recoveryStep is null)
                        {
                            result.Warnings.Add(
                                $"Configured round '{definition.DisplayName}' recovery turn {attemptIndex + 1} for {recoveryModel} did not create a Council evidence step. The round remains unresolved and LocalGPT will continue the configured recovery policy.");
                            continue;
                        }

                        recoveryStep.WorkflowStepKey = definition.Key;
                        recoveryStep.RecoveryTargetModelName = failedModel;
                        recoveryStep.WorkflowRevision = Math.Max(1, workflowRevision);
                        recoveryStep.XRoundCause = xRoundCause ?? string.Empty;
                        recoverySteps.Add(recoveryStep);

                        logger.LogInformation(
                            "Council round recovery for run {RunId}, step {StepKey}: attempt {Attempt} for {FailedModel} used {RecoveryModel} ({Action}, other role {FromOtherRole}, paired substitute {PairedSubstitute}) and {Outcome}.",
                            result.RunId,
                            definition.Key,
                            attemptIndex + 1,
                            failedModel,
                            recoveryModel,
                            decision.Action,
                            decision.FromOtherRole,
                            decision.SubstitutesPairedSlot,
                            IsUsableConfiguredRoundStep(recoveryStep) ? "recovered the slot" : "did not produce a usable answer");
                        previousAttemptModel = recoveryModel;
                        previousAttemptEmpty = string.IsNullOrWhiteSpace(recoveryStep.Error) && string.IsNullOrWhiteSpace(recoveryStep.VisibleContent);
                        if (IsUsableConfiguredRoundStep(recoveryStep))
                        {
                            recovered = true;
                            request.ProgressMessage?.Invoke(
                                $"Configured round '{definition.DisplayName}' recovered the failed member slot through {recoveryModel}; the original failed attempt remains in the audit trail.");
                            break;
                        }
                    }

                    if (!recovered && !leftEmptyByDecision)
                    {
                        result.Warnings.Add(
                            $"Configured round '{definition.DisplayName}' could not recover required member work originally assigned to {failedModel} after {maximumAttempts} round-level recovery turn(s). The failure remains explicit; LocalGPT did not silently drop or fabricate this member result.");
                        // This round has already consumed its existing recovery policy. Report
                        // the observed stall to the shared service; do not schedule another
                        // Council retry, change its tool permissions, or conceal the failure.
                        try
                        {
                            var identity = $"{result.RunId:N}:{definition.Key}:{round}:{failedModel}";
                            var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
                            var taskId = $"council:{result.RunId:N}:{digest}";
                            var taskDescription = $"Council run {result.RunId:N}, workflow step {definition.Key}, role {roleAssignment.RoleName}";
                            var attemptedAction = $"Recover failed Council participant {failedModel} in phase {phase}";
                            var helpDecision = await aiHelpEscalator.ReportAttemptAsync(new AiHelpAttemptReport
                            {
                                TaskId = taskId,
                                AttemptId = new Guid(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("attempt:" + identity))[..16]),
                                Task = taskDescription[..Math.Min(2000, taskDescription.Length)],
                                Action = attemptedAction[..Math.Min(500, attemptedAction.Length)],
                                Status = "no_progress",
                                CouncilRunId = result.RunId,
                                FailedModelSelectionKey = failedModel[..Math.Min(160, failedModel.Length)],
                                Error = new AiHelpFailureEvidence
                                {
                                    Code = "CouncilMemberRecoveryExhausted",
                                    Message = "The configured round member recovery attempts were exhausted.",
                                    Source = "validator"
                                }
                            }, cancellationToken).ConfigureAwait(false);
                            logger.LogInformation("Council member recovery exhausted for run {RunId}, step {StepKey}, model {ModelName}; central help decision {DecisionType}, request {HumanRequestId}.",
                                result.RunId, definition.Key, failedModel, helpDecision.Type, helpDecision.HumanRequestId);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                        catch (Exception exception)
                        {
                            logger.LogWarning(exception, "Central AI help escalation unavailable for Council run {RunId}, step {StepKey}; original failure remains visible.", result.RunId, definition.Key);
                        }
                    }
                }

                return recoverySteps;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(
                    "Configured Council round recovery was cancelled by the caller for run {RunId}, step {WorkflowStepKey}.",
                    result.RunId,
                    definition.Key);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Configured Council round recovery failed for run {RunId}, step {WorkflowStepKey}, phase {Phase}.",
                    result.RunId,
                    definition.Key,
                    phase);
                throw;
            }
        }

        /// <summary>Returns whether one Council step contains usable visible model work for round-completion accounting.</summary>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool IsUsableConfiguredRoundStep(MultiModelCouncilStep? step)
        {
            try
            {
                return step is not null &&
                    string.IsNullOrWhiteSpace(step.Error) &&
                    !IsRoundSkippedStep(step) &&
                    !string.IsNullOrWhiteSpace(step.VisibleContent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Configured Council round step usability evaluation failed.");
                throw;
            }
        }

        /// <summary>
        /// Returns the configured members of the failed role in this run and the members that other roles of the same
        /// distinct-assignment group reserve. Reserved members are not used for this role unless the role allows distinct fallback.
        /// </summary>
        /// <param name="definition">Configured workflow step.</param>
        /// <param name="executionMode">Execution mode of the step.</param>
        /// <param name="participants">Active members of the run.</param>
        /// <param name="roleAssignment">Runtime assignment of the failed role.</param>
        /// <param name="roleAssignments">Runtime assignments of all roles of the run.</param>
        /// <returns>The role pool (members of the run only) and the reserved members.</returns>
        private (IReadOnlyList<string> Pool, IReadOnlyList<string> Reserved) BuildConfiguredRoundRecoveryPool(
            CouncilWorkflowStepDefinition definition,
            string executionMode,
            IReadOnlyList<string> participants,
            CouncilRoleRuntimeAssignment roleAssignment,
            IDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments)
        {
            try
            {
                var roleDefinition = roleAssignment.Definition;
                IEnumerable<string> configuredPool;
                if (roleDefinition is not null &&
                    roleDefinition.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                {
                    configuredPool = roleDefinition.AssignedModelKeys
                        .Where(key => participants.Contains(key, StringComparer.OrdinalIgnoreCase));
                }
                else if (roleAssignment.AiParticipants.Count > 0)
                {
                    configuredPool = roleAssignment.AiParticipants
                        .Where(member => participants.Contains(member, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    configuredPool = participants;
                }

                IReadOnlyList<string> reserved = [];
                if (roleDefinition is not null &&
                    !string.IsNullOrWhiteSpace(roleDefinition.DistinctAiAssignmentGroup) &&
                    !roleDefinition.AllowDistinctAiAssignmentFallback)
                {
                    reserved = roleAssignments.Values
                        .Where(assignment =>
                            !string.Equals(assignment.RoleName, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase) &&
                            assignment.Definition is not null &&
                            string.Equals(
                                assignment.Definition.DistinctAiAssignmentGroup,
                                roleDefinition.DistinctAiAssignmentGroup,
                                StringComparison.OrdinalIgnoreCase))
                        .SelectMany(assignment => assignment.AiParticipants)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var reservedSet = reserved.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var pool = configuredPool
                    .Where(member => !reservedSet.Contains(member))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return (pool, reserved);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to build the configured round recovery pool for step {StepKey}, role {RoleName}, mode {ExecutionMode}.",
                    definition.Key,
                    roleAssignment.RoleName,
                    executionMode);
                throw;
            }
        }

        /// <summary>
        /// Lets an approved substitute inherit the failed member's pairing for one recovery turn: the failed member's pairing is
        /// re-addressed to the substitute and the substitute's own pairing in that role is left out of this turn.
        /// </summary>
        /// <param name="rolePairings">Pairings of the run.</param>
        /// <param name="roleName">Paired role of the failed slot.</param>
        /// <param name="failedModel">Member originally assigned to the slot.</param>
        /// <param name="substituteModel">Member answering for the slot.</param>
        /// <returns>The pairings used for the substitute's recovery turn.</returns>
        private IReadOnlyList<CouncilParticipantPairing> BuildSubstitutedRolePairings(
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            string roleName,
            string failedModel,
            string substituteModel)
        {
            try
            {
                return rolePairings
                    .Where(pairing => !(
                        string.Equals(pairing.RoleName, roleName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pairing.Participant, substituteModel, StringComparison.OrdinalIgnoreCase)))
                    .Select(pairing =>
                        string.Equals(pairing.RoleName, roleName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pairing.Participant, failedModel, StringComparison.OrdinalIgnoreCase)
                            ? pairing with { Participant = substituteModel }
                            : pairing)
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to re-address the pairing of {FailedModel} to {SubstituteModel} in role {RoleName}.", failedModel, substituteModel, roleName);
                throw;
            }
        }

        /// <summary>Identifies the base configured phase and its automatic member-recovery child phases.</summary>
        /// <param name="candidatePhase">Candidate phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool IsConfiguredRoundPrimaryOrRecoveryPhase(string candidatePhase, string phase)
        {
            try
            {
                return string.Equals(candidatePhase, phase, StringComparison.Ordinal) ||
                    candidatePhase.StartsWith($"{phase} · automatic member recovery ", StringComparison.Ordinal) ||
                    candidatePhase.StartsWith($"{phase} · tool continuation ", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Configured Council round recovery phase comparison failed for phase {Phase}.", phase);
                throw;
            }
        }
    }
}
