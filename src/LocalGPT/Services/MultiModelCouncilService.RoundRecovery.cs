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
        /// persisted workflow policy. Alternate members are selected only from the role's existing provider-qualified Social Team
        /// pool and therefore never create a hidden model-selection policy outside the saved team configuration.
        /// </remarks>
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
                    string.Equals(executionMode, "SystemBenchmarkCalibration", StringComparison.Ordinal))
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
                for (var slotIndex = 0; slotIndex < failedSlots.Count; slotIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var failedModel = failedSlots[slotIndex];
                    var attemptedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool)
                        attemptedModels.Add(failedModel); // RunParticipantAsync already exhausted its same-member safe fallback.
                    var recovered = false;

                    for (var attemptIndex = 0; attemptIndex < Math.Clamp(definition.MemberFailureRecoveryAttempts, 0, 8); attemptIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var recoveryModel = SelectConfiguredRoundRecoveryParticipant(
                            result,
                            team,
                            definition,
                            executionMode,
                            participants,
                            roleAssignment,
                            roleAssignments,
                            failedModel,
                            attemptedModels);
                        attemptedModels.Add(recoveryModel);

                        var recoveryPhase = $"{phase} · automatic member recovery {slotIndex + 1}.{attemptIndex + 1}";
                        request.ProgressMessage?.Invoke(
                            $"Configured round '{definition.DisplayName}' is repairing failed required member work from {failedModel} with {recoveryModel} (recovery {attemptIndex + 1}/{definition.MemberFailureRecoveryAttempts}). The failed attempt remains preserved in Council evidence.");

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
                        recoveryStep.WorkflowRevision = Math.Max(1, workflowRevision);
                        recoveryStep.XRoundCause = xRoundCause ?? string.Empty;
                        recoverySteps.Add(recoveryStep);

                        if (IsUsableConfiguredRoundStep(recoveryStep))
                        {
                            recovered = true;
                            request.ProgressMessage?.Invoke(
                                $"Configured round '{definition.DisplayName}' recovered the failed member slot through {recoveryModel}; the original failed attempt remains in the audit trail.");
                            break;
                        }
                    }

                    if (!recovered)
                    {
                        result.Warnings.Add(
                            $"Configured round '{definition.DisplayName}' could not recover required member work originally assigned to {failedModel} after {definition.MemberFailureRecoveryAttempts} round-level recovery turn(s). The failure remains explicit; LocalGPT did not silently drop or fabricate this member result.");
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

        /// <summary>Selects the same member first and, when permitted, a healthy alternate from the persisted role pool.</summary>
        private string SelectConfiguredRoundRecoveryParticipant(
            MultiModelCouncilResult result,
            OrganicCouncilTeamDefinition team,
            CouncilWorkflowStepDefinition definition,
            string executionMode,
            IReadOnlyList<string> participants,
            CouncilRoleRuntimeAssignment roleAssignment,
            IDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments,
            string failedModel,
            IReadOnlySet<string> attemptedModels)
        {
            try
            {
                var mustRetrySame =
                    definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.RetrySameMember ||
                    string.Equals(executionMode, "AssignedModelSingle", StringComparison.Ordinal);
                if (mustRetrySame)
                    return failedModel;

                var roleDefinition = roleAssignment.Definition;
                IEnumerable<string> configuredPool;
                if (roleDefinition is not null &&
                    roleDefinition.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                {
                    configuredPool = roleDefinition.AssignedModelKeys
                        .Where(key => participants.Contains(key, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    configuredPool = participants;
                }

                if (roleDefinition is not null && !string.IsNullOrWhiteSpace(roleDefinition.DistinctAiAssignmentGroup))
                {
                    var reservedByOtherRoles = roleAssignments.Values
                        .Where(assignment =>
                            !string.Equals(assignment.RoleName, roleAssignment.RoleName, StringComparison.OrdinalIgnoreCase) &&
                            assignment.Definition is not null &&
                            string.Equals(
                                assignment.Definition.DistinctAiAssignmentGroup,
                                roleDefinition.DistinctAiAssignmentGroup,
                                StringComparison.OrdinalIgnoreCase))
                        .SelectMany(assignment => assignment.AiParticipants)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    configuredPool = configuredPool.Where(model => !reservedByOtherRoles.Contains(model));
                }

                var candidates = configuredPool
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(model => !attemptedModels.Contains(model))
                    .ToList();
                if (candidates.Count == 0)
                    return failedModel;

                // Prefer escaping the provider/host road that just failed, then use LocalGPT's existing observed-health order.
                var failedHost = GetCouncilExecutionHostKey(failedModel);
                var healthyOrder = OrderParticipantsByObservedHealth(result, candidates).ToList();
                var alternate = healthyOrder
                    .OrderBy(model => string.Equals(GetCouncilExecutionHostKey(model), failedHost, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenBy(model => healthyOrder.IndexOf(model))
                    .FirstOrDefault();
                return alternate ?? failedModel;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to select a configured round recovery participant for team {TeamKey}, role {RoleName}, failed model {FailedModel}.",
                    team.Key,
                    roleAssignment.RoleName,
                    failedModel);
                throw;
            }
        }

        /// <summary>Identifies the base configured phase and its automatic member-recovery child phases.</summary>
        private bool IsConfiguredRoundPrimaryOrRecoveryPhase(string candidatePhase, string phase)
        {
            try
            {
                return string.Equals(candidatePhase, phase, StringComparison.Ordinal) ||
                    candidatePhase.StartsWith($"{phase} · automatic member recovery ", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Configured Council round recovery phase comparison failed for phase {Phase}.", phase);
                throw;
            }
        }
    }
}
