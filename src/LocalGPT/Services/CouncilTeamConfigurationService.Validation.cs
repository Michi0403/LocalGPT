using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council team configuration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTeamConfigurationService
    {
    /// <summary>
    /// Normalizes and validate user definition as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="team">Definition to validate in place.</param>
    private void NormalizeAndValidateUserDefinition(OrganicCouncilTeamDefinition team)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(team.Key);
            team.Key = team.Key.Trim().ToLowerInvariant();
            team.DisplayName = string.IsNullOrWhiteSpace(team.DisplayName) ? team.Key : team.DisplayName.Trim();
            team.Purpose = team.Purpose?.Trim() ?? string.Empty;
            team.ExpertPreparationPromptTemplate = team.ExpertPreparationPromptTemplate?.Trim() ?? string.Empty;
            team.LeaderSynthesisPromptTemplate = team.LeaderSynthesisPromptTemplate?.Trim() ?? string.Empty;
            team.MainRoundInstructionTemplate = team.MainRoundInstructionTemplate?.Trim() ?? string.Empty;
            if (!Enum.IsDefined(typeof(CouncilAllMembersReadinessPreflightMode), team.AllMembersReadinessPreflightMode))
                team.AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault;
            team.AllMembersReadinessPreflightMaxOutputTokens = Math.Clamp(team.AllMembersReadinessPreflightMaxOutputTokens, 32, 2048);
            team.AllMembersReadinessPreflightPromptTemplate = team.AllMembersReadinessPreflightPromptTemplate?.Trim() ?? string.Empty;
            team.Roles ??= [];
            team.WorkflowSteps ??= [];
            team.PreferredCapabilities ??= [];
            team.AllowedAutomaticFunctions ??= [];
            team.ArchitectureContracts ??= [];

            if (team.Roles.Count > MaxRoles)
                throw new InvalidOperationException($"A council team can contain at most {MaxRoles} role definitions.");
            if (team.WorkflowSteps.Count > MaxWorkflowSteps)
                throw new InvalidOperationException($"A council workflow can contain at most {MaxWorkflowSteps} saved steps.");
            if (team.Roles.Any(role => role is null))
                throw new InvalidOperationException("Role definitions cannot contain null entries.");
            if (team.WorkflowSteps.Any(step => step is null))
                throw new InvalidOperationException("Workflow definitions cannot contain null entries.");

            foreach (var role in team.Roles)
            {
                role.Role = role.Role?.Trim() ?? string.Empty;
                role.Expertise = role.Expertise?.Trim() ?? string.Empty;
                role.Responsibility = role.Responsibility?.Trim() ?? string.Empty;
                role.DistinctAiAssignmentGroup = role.DistinctAiAssignmentGroup?.Trim() ?? string.Empty;
                role.MatchAiParticipantCountToRole = role.MatchAiParticipantCountToRole?.Trim() ?? string.Empty;
                role.PairedRole = role.PairedRole?.Trim() ?? string.Empty;
                role.RuntimeClassKeys ??= [];
                role.RuntimeClassKeys = role.RuntimeClassKeys
                    .Select(value => value?.Trim().ToLowerInvariant() ?? string.Empty)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                role.AssignedModelKeys ??= [];
                role.AssignedModelKeys = role.AssignedModelKeys
                    .Select(value => value?.Trim() ?? string.Empty)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!Enum.IsDefined(typeof(CouncilRoleAiSelectionMode), role.AiSelectionMode))
                    role.AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected;
                if (!Enum.IsDefined(typeof(HumanParticipationMode), role.HumanParticipationMode))
                    role.HumanParticipationMode = HumanParticipationMode.None;
                if (!Enum.IsDefined(typeof(CouncilRolePerformanceMode), role.PerformanceMode))
                    role.PerformanceMode = CouncilRolePerformanceMode.TaskSpecialist;
                if (!Enum.IsDefined(typeof(CouncilRoleLanguageMode), role.LanguageMode))
                    role.LanguageMode = CouncilRoleLanguageMode.ModelChoice;
                if (!Enum.IsDefined(typeof(CouncilRoleBoundaryMode), role.BoundaryMode))
                    role.BoundaryMode = CouncilRoleBoundaryMode.Bounded;

                role.MinimumAiParticipants = Math.Max(1, role.MinimumAiParticipants);
                role.MaximumAiParticipants = Math.Max(1, role.MaximumAiParticipants);
                if ((role.AiSelectionMode is CouncilRoleAiSelectionMode.RandomRange or CouncilRoleAiSelectionMode.AssignedModelsRandomRange) &&
                    role.MinimumAiParticipants > role.MaximumAiParticipants)
                {
                    throw new InvalidOperationException(
                        $"Role '{role.Role}' has a minimum AI participant count greater than its maximum.");
                }
                if ((role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange) &&
                    role.HumanParticipationMode != HumanParticipationMode.HumanOnly &&
                    role.AssignedModelKeys.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Role '{role.Role}' uses a provider-bound AI pool but has no provider-qualified model selected.");
                }
            }

            var duplicateRoleNames = team.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Role))
                .GroupBy(role => role.Role, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateRoleNames.Count > 0)
                throw new InvalidOperationException($"Role names must be unique. Duplicate role(s): {string.Join(", ", duplicateRoleNames)}.");

            var rolesByName = team.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Role))
                .ToDictionary(role => role.Role, StringComparer.OrdinalIgnoreCase);
            foreach (var role in team.Roles)
            {
                if (!string.IsNullOrWhiteSpace(role.MatchAiParticipantCountToRole))
                {
                    if (string.Equals(role.Role, role.MatchAiParticipantCountToRole, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Role '{role.Role}' cannot match its AI participant count to itself.");
                    if (!rolesByName.ContainsKey(role.MatchAiParticipantCountToRole))
                        throw new InvalidOperationException($"Role '{role.Role}' matches its AI participant count to missing role '{role.MatchAiParticipantCountToRole}'.");
                }

                if (!string.IsNullOrWhiteSpace(role.PairedRole))
                {
                    if (string.Equals(role.Role, role.PairedRole, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Role '{role.Role}' cannot be paired with itself.");
                    if (!rolesByName.ContainsKey(role.PairedRole))
                        throw new InvalidOperationException($"Role '{role.Role}' references missing paired role '{role.PairedRole}'.");
                }
            }
            ValidateRoleCountReferenceCycles(team.Roles);
            ValidateDistinctAssignmentGroups(team.Roles);

            var duplicateKeys = team.WorkflowSteps
                .Where(step => !string.IsNullOrWhiteSpace(step.Key))
                .GroupBy(step => step.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateKeys.Count > 0)
                throw new InvalidOperationException($"Workflow step keys must be unique. Duplicate key(s): {string.Join(", ", duplicateKeys)}.");

            foreach (var step in team.WorkflowSteps)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(step.Key);
                step.Key = step.Key.Trim().ToLowerInvariant();
                step.DisplayName = string.IsNullOrWhiteSpace(step.DisplayName) ? step.Key : step.DisplayName.Trim();
                step.Phase = string.IsNullOrWhiteSpace(step.Phase) ? step.DisplayName : step.Phase.Trim();
                step.Role = string.IsNullOrWhiteSpace(step.Role) ? "Council participant" : step.Role.Trim();
                step.PromptTemplate = step.PromptTemplate?.Trim() ?? string.Empty;
                step.AssignedModelName = step.AssignedModelName?.Trim() ?? string.Empty;
                step.LogicalRoundNumber = Math.Clamp(step.LogicalRoundNumber, 0, MaxExpandedWorkflowSteps);
                if (!Enum.IsDefined(typeof(CouncilTranscriptVisibilityMode), step.TranscriptVisibility))
                    step.TranscriptVisibility = CouncilTranscriptVisibilityMode.FullCouncil;
                if (!Enum.IsDefined(typeof(CouncilRoleResultSynthesisMemberMode), step.RoleResultSynthesisMemberMode))
                    step.RoleResultSynthesisMemberMode = CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember;
                step.RoleResultSynthesisModelName = step.RoleResultSynthesisModelName?.Trim() ?? string.Empty;
                step.AllowedAutomaticFunctions ??= [];
                step.AllowedAutomaticFunctions = step.AllowedAutomaticFunctions
                    .Select(value => value?.Trim() ?? string.Empty)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                step.RepeatCount = Math.Clamp(step.RepeatCount, 1, MaxExpandedWorkflowSteps);
                step.ExecutionMode = NormalizeExecutionMode(step.ExecutionMode);
                step.LoopGroup = step.LoopGroup?.Trim() ?? string.Empty;
                step.MaximumLoopIterations = string.IsNullOrWhiteSpace(step.LoopGroup)
                    ? 1
                    : Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps);
                step.LoopCompletionMarker = step.LoopCompletionMarker?.Trim() ?? string.Empty;
                step.XMaximumTransitions = Math.Clamp(step.XMaximumTransitions, 1, MaxExpandedWorkflowSteps);
                step.XMaximumChildCouncilDepth = Math.Clamp(step.XMaximumChildCouncilDepth, 1, 10);
                step.XDefaultTargetStepKey = step.XDefaultTargetStepKey?.Trim().ToLowerInvariant() ?? string.Empty;
                step.XChildCouncilTeamKey = step.XChildCouncilTeamKey?.Trim().ToLowerInvariant() ?? string.Empty;
                step.XChildModelName = step.XChildModelName?.Trim() ?? string.Empty;
                step.AsciiFrameWidth = Math.Clamp(step.AsciiFrameWidth, 20, 240);
                step.AsciiFrameHeight = Math.Clamp(step.AsciiFrameHeight, 8, 120);
                step.WorldStepScale = Math.Clamp(step.WorldStepScale, 1, 1000);
                if (step.ProducesAsciiFrame && step.ExecutionMode is "AllMembersParallel" or "AllMembersSequentialOnEachAIHostParallel" or "AllMembersSequential")
                    throw new InvalidOperationException($"ASCII frame step '{step.DisplayName}' must use a single-member execution mode so one AI owns the complete frame.");
                if (string.IsNullOrWhiteSpace(step.LoopGroup) && !string.IsNullOrWhiteSpace(step.LoopCompletionMarker))
                    throw new InvalidOperationException($"Workflow step '{step.DisplayName}' defines a loop completion marker without a loop group.");
                if (step.XFunctionsEnabled && !step.CanUseOrganicFunctions)
                    throw new InvalidOperationException($"Workflow step '{step.DisplayName}' enables X-Round DXFunctions while DX/organic function requests are disabled. Enable both so X control can be invoked explicitly.");
                if (step.XFunctionsEnabled &&
                    !step.XCanRevisit &&
                    !step.XCanReturnText &&
                    !step.XCanStartSingleModel &&
                    !step.XCanStartCouncil)
                    throw new InvalidOperationException($"Workflow step '{step.DisplayName}' enables X-Rounds but grants no X action.");
                if (team.Roles.Count > 0 && !rolesByName.ContainsKey(step.Role))
                    throw new InvalidOperationException($"Workflow step '{step.DisplayName}' references role '{step.Role}', but that role is not defined in the team.");
                if (step.SummarizeRoleResults &&
                    step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember &&
                    string.IsNullOrWhiteSpace(step.RoleResultSynthesisModelName))
                {
                    throw new InvalidOperationException(
                        $"Workflow step '{step.DisplayName}' uses a selected role-result summarizer but no provider-qualified role member is selected.");
                }
                if (step.SummarizeRoleResults &&
                    step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember &&
                    rolesByName.TryGetValue(step.Role, out var synthesisRole) &&
                    synthesisRole.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels &&
                    !synthesisRole.AssignedModelKeys.Contains(step.RoleResultSynthesisModelName, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Workflow step '{step.DisplayName}' selects role-result summarizer '{step.RoleResultSynthesisModelName}', but that model is not bound to role '{step.Role}'.");
                }
                if (step.ExecutionMode == "AssignedModelSingle")
                {
                    if (string.IsNullOrWhiteSpace(step.AssignedModelName))
                        throw new InvalidOperationException($"Workflow step '{step.DisplayName}' uses AssignedModelSingle but has no provider-qualified assigned model.");
                    if (rolesByName.TryGetValue(step.Role, out var stepRole))
                    {
                        if (stepRole.HumanParticipationMode == HumanParticipationMode.HumanOnly)
                            throw new InvalidOperationException($"Workflow step '{step.DisplayName}' cannot use AssignedModelSingle because role '{step.Role}' is human-only.");
                        if (stepRole.AiSelectionMode is CouncilRoleAiSelectionMode.RandomRange or CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                        {
                            throw new InvalidOperationException(
                                $"Workflow step '{step.DisplayName}' uses AssignedModelSingle, but role '{step.Role}' selects a random subset. Use AllSelected or the all-exact-model provider assignment so the exact assigned model is guaranteed to belong to the role.");
                        }
                        if (stepRole.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels &&
                            !stepRole.AssignedModelKeys.Contains(step.AssignedModelName, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Workflow step '{step.DisplayName}' assigns model '{step.AssignedModelName}', but that model is not bound to role '{step.Role}'.");
                        }
                    }
                }
            }

            var workflowStepKeys = team.WorkflowSteps
                .Select(step => step.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var step in team.WorkflowSteps.Where(step => step.XFunctionsEnabled && step.XCanRevisit))
            {
                if (!string.IsNullOrWhiteSpace(step.XDefaultTargetStepKey) &&
                    !workflowStepKeys.Contains(step.XDefaultTargetStepKey))
                {
                    throw new InvalidOperationException(
                        $"Workflow step '{step.DisplayName}' uses missing default X-Round target '{step.XDefaultTargetStepKey}'.");
                }
            }

            NormalizeLoopGroups(team.WorkflowSteps);
            ValidateLoopGroups(team.WorkflowSteps);

            var enabledSteps = team.WorkflowSteps.Where(step => step.IsEnabled).ToList();
            if (enabledSteps.Count == 0)
                throw new InvalidOperationException("Enable at least one workflow step before saving the council team.");
            var expandedCount = CalculateMaximumExpandedRounds(enabledSteps);
            if (expandedCount > MaxExpandedWorkflowSteps)
                throw new InvalidOperationException($"The enabled workflow can expand to {expandedCount} rounds including bounded loops. The technical limit is {MaxExpandedWorkflowSteps} per run.");

            team.WorkflowSteps = team.WorkflowSteps
                .OrderBy(step => step.SortOrder)
                .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            team.PreferredCapabilities = team.PreferredCapabilities.Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            team.AllowedAutomaticFunctions ??= [];
            team.AllowedAutomaticFunctions = NormalizeFunctionNames(team.AllowedAutomaticFunctions);
            foreach (var step in team.WorkflowSteps)
            {
                step.AllowedAutomaticFunctions ??= [];
                step.AllowedAutomaticFunctions = NormalizeFunctionNames(step.AllowedAutomaticFunctions);
                step.AutomaticFunctionPolicyMode = NormalizeAutomaticFunctionPolicy(step);
                step.CanUseOrganicFunctions = step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.Disabled;
                step.RoleComplianceRetryCount = Math.Clamp(step.RoleComplianceRetryCount, 0, 3);
                if (!Enum.IsDefined(typeof(CouncilMemberFailureRecoveryMode), step.MemberFailureRecoveryMode))
                    step.MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool;
                step.MemberFailureRecoveryAttempts = Math.Clamp(step.MemberFailureRecoveryAttempts, 0, 8);
                step.FinalAnswerRecoveryMaxOutputTokens = Math.Clamp(step.FinalAnswerRecoveryMaxOutputTokens, 128, 32768);
            }
            team.ArchitectureContracts = team.ArchitectureContracts.Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeAndValidateUserDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeAndValidateUserDefinition)} failed.");
        throw;
    }
}

    }
}
