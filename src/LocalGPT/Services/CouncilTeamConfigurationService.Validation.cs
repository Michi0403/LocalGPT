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
            team.AllMembersReadinessPreflightMaxOutputTokens = Math.Clamp(team.AllMembersReadinessPreflightMaxOutputTokens, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MinimumReadinessOutputTokens, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumReadinessOutputTokens);
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
                step.RoleResultSynthesisPromptTemplate = step.RoleResultSynthesisPromptTemplate?.Trim() ?? string.Empty;
                step.RolePeerReviewPromptTemplate = step.RolePeerReviewPromptTemplate?.Trim() ?? string.Empty;
                step.AllowedAutomaticFunctions ??= [];
                step.AllowedAutomaticFunctions = step.AllowedAutomaticFunctions
                    .Select(value => value?.Trim() ?? string.Empty)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!Enum.IsDefined(typeof(CouncilToolResultContinuationMode), step.ToolResultContinuationMode))
                    step.ToolResultContinuationMode = CouncilToolResultContinuationMode.SameMemberBounded;
                step.ToolResultContinuationRounds = step.ToolResultContinuationMode == CouncilToolResultContinuationMode.Disabled
                    ? 0
                    : Math.Clamp(step.ToolResultContinuationRounds, 1, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumContinuationRounds);
                step.RepeatCount = Math.Clamp(step.RepeatCount, 1, MaxExpandedWorkflowSteps);
                step.ExecutionMode = NormalizeExecutionMode(step.ExecutionMode);
                step.LoopGroup = step.LoopGroup?.Trim() ?? string.Empty;
                step.MaximumLoopIterations = string.IsNullOrWhiteSpace(step.LoopGroup)
                    ? 1
                    : Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps);
                step.LoopCompletionMarker = step.LoopCompletionMarker?.Trim() ?? string.Empty;
                step.XMaximumTransitions = Math.Clamp(step.XMaximumTransitions, 1, MaxExpandedWorkflowSteps);
                step.XMaximumChildCouncilDepth = Math.Clamp(step.XMaximumChildCouncilDepth, 1, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumChildCouncilDepth);
                step.XDefaultTargetStepKey = step.XDefaultTargetStepKey?.Trim().ToLowerInvariant() ?? string.Empty;
                step.XChildCouncilTeamKey = step.XChildCouncilTeamKey?.Trim().ToLowerInvariant() ?? string.Empty;
                step.XChildModelName = step.XChildModelName?.Trim() ?? string.Empty;
                step.AsciiFrameWidth = Math.Clamp(step.AsciiFrameWidth, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MinimumFrameWidth, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MaximumFrameWidth);
                step.AsciiFrameHeight = Math.Clamp(step.AsciiFrameHeight, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MinimumFrameHeight, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MaximumFrameHeight);
                step.WorldStepScale = Math.Clamp(step.WorldStepScale, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MinimumWorldStepScale, runtimePolicy.GetJson<CouncilGameRuntimeParameters>(LocalGptRuntimeValue.CouncilGameRuntimeParametersJson).MaximumWorldStepScale);
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

            // Evaluate recovery permissions only after all saved steps have been
            // normalized. Before this point a user-edited step can still have its
            // previous effective function mode or unnormalized allow-list values.
            var duplicateRecoveryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in team.CompletionRecovery.Rules)
            {
                if (rule is null) throw new InvalidOperationException("Completion recovery rules cannot be null.");
                rule.FunctionName = rule.FunctionName?.Trim() ?? string.Empty;
                rule.WorkflowStepKey = rule.WorkflowStepKey?.Trim() ?? string.Empty;
                rule.RecoveryPromptTemplate = rule.RecoveryPromptTemplate?.Trim() ?? string.Empty;
                rule.PrerequisiteFunctions = NormalizeFunctionNames(rule.PrerequisiteFunctions);
                rule.MaximumAttempts = Math.Clamp(rule.MaximumAttempts, 0,
                    runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumCompletionRecoveryAttempts);
                if (string.IsNullOrWhiteSpace(rule.FunctionName) ||
                    !team.RequiredCompletedFunctions.Contains(rule.FunctionName, StringComparer.OrdinalIgnoreCase) ||
                    !duplicateRecoveryNames.Add(rule.FunctionName))
                    throw new InvalidOperationException("Each completion recovery rule needs a unique function from the team's required results.");
                var recoveryStep = team.WorkflowSteps.FirstOrDefault(step => step.IsEnabled &&
                    string.Equals(step.Key, rule.WorkflowStepKey, StringComparison.OrdinalIgnoreCase));
                if (recoveryStep is null || !recoveryStep.CanUseOrganicFunctions ||
                    (recoveryStep.AutomaticFunctionPolicyMode == CouncilAutomaticFunctionPolicyMode.StrictStepAllowList &&
                     !recoveryStep.AllowedAutomaticFunctions.Contains(rule.FunctionName, StringComparer.OrdinalIgnoreCase)) ||
                    (recoveryStep.AutomaticFunctionPolicyMode == CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList &&
                     !team.AllowedAutomaticFunctions.Contains(rule.FunctionName, StringComparer.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Recovery rule '{rule.FunctionName}' needs a function-capable step whose strict function policy permits this function.");
                if (rule.PrerequisiteFunctions.Any(name => !team.RequiredCompletedFunctions.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    string.Equals(name, rule.FunctionName, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Recovery rule '{rule.FunctionName}' has an invalid required-function prerequisite.");
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
            if (!Enum.IsDefined(typeof(CouncilProjectProvisioningMode), team.ProjectProvisioningMode))
                throw new InvalidOperationException("The configured project provisioning mode is not supported.");
            team.RequiredCompletedFunctions = NormalizeFunctionNames(team.RequiredCompletedFunctions);
            team.CompletionNoticeTemplate = team.CompletionNoticeTemplate?.Trim() ?? string.Empty;
            team.CompletionRecovery ??= new();
            team.CompletionRecovery.Rules ??= [];
            team.CompletionRecovery.FinalReviewPromptTemplate = team.CompletionRecovery.FinalReviewPromptTemplate?.Trim() ?? string.Empty;
            if (team.CompletionRecovery.Rules.Count > MaxWorkflowSteps)
                throw new InvalidOperationException("Completion recovery has more rules than the workflow step limit.");
            team.FollowOn ??= new();
            team.FollowOn.TeamKey = team.FollowOn.TeamKey?.Trim().ToLowerInvariant() ?? string.Empty;
            team.FollowOn.PromptTemplate = team.FollowOn.PromptTemplate?.Trim() ?? string.Empty;
            team.FollowOn.MaximumDepth = Math.Clamp(team.FollowOn.MaximumDepth, 1,
                runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumChildCouncilDepth);
            if (!string.IsNullOrWhiteSpace(team.FollowOn.TeamKey) &&
                string.IsNullOrWhiteSpace(team.FollowOn.PromptTemplate))
                throw new InvalidOperationException("A configured follow-on team requires an explicit task prompt. Use {{ParentAnswer}} to pass the previous result.");
            if (string.Equals(team.FollowOn.TeamKey, team.Key, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A Council team cannot follow itself; select a different saved team.");
            foreach (var step in team.WorkflowSteps)
            {
                if (step.RequiresVerifiedWorkspaceFileRead && !step.CanUseOrganicFunctions)
                    throw new InvalidOperationException($"Workflow step '{step.Key}' requires workspace file reads but disables automatic functions.");
                step.RequiredCompletedWorkspaceFiles = Math.Clamp(step.RequiredCompletedWorkspaceFiles, 0, 100000);
                if (!step.RequiresVerifiedWorkspaceFileRead &&
                    (step.RequiredCompletedWorkspaceFiles > 0 || step.RequireEachWorkspaceArchiveRead || step.ContinueWorkspaceReviewOnlyOnProgress))
                    throw new InvalidOperationException($"Workflow step '{step.Key}' has workspace reading acceptance conditions without enabling the required verified workspace-read gate.");
                step.MissingWorkspaceFileReadRecoveryAttempts = Math.Clamp(step.MissingWorkspaceFileReadRecoveryAttempts, 0,
                    runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumWorkspaceFileReadRecoveryAttempts);
                step.WorkspaceFileReadRecoveryPromptTemplate = step.WorkspaceFileReadRecoveryPromptTemplate?.Trim() ?? string.Empty;
                step.WorkspaceSourcePriorityTerms = (step.WorkspaceSourcePriorityTerms ?? [])
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .Select(term => term.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(32).ToList();
                if ((step.RetryMissingWorkspaceFileReadOnce || step.MissingWorkspaceFileReadRecoveryAttempts > 0) && !step.RequiresVerifiedWorkspaceFileRead)
                    throw new InvalidOperationException($"Workflow step '{step.Key}' retries workspace reads without requiring verified file reads.");
                step.AllowedAutomaticFunctions ??= [];
                step.AllowedAutomaticFunctions = NormalizeFunctionNames(step.AllowedAutomaticFunctions);
                step.AutomaticFunctionPolicyMode = NormalizeAutomaticFunctionPolicy(step);
                step.CanUseOrganicFunctions = step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.Disabled;
                // Exclusive tool sets are an optional per-step orchestration choice, not an
                // authorization decision. Reject impossible source-review configurations
                // rather than letting a model silently skip a required uploaded-file read.
                if (step.RequiresVerifiedWorkspaceFileRead)
                {
                    var restrictedNames = step.AutomaticFunctionPolicyMode switch
                    {
                        CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList => team.AllowedAutomaticFunctions,
                        CouncilAutomaticFunctionPolicyMode.StrictStepAllowList => step.AllowedAutomaticFunctions,
                        _ => null
                    };
                    if (restrictedNames is not null &&
                        !restrictedNames.Contains("chat.upload_workspace_file", StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Workflow step '{step.Key}' requires a verified uploaded-file read but its explicit strict tool list excludes chat.upload_workspace_file. " +
                            "Add the file reader to this step's selected tools or choose a preferred/all-functions mode.");
                }
                if (!Enum.IsDefined(typeof(CouncilToolResultContinuationMode), step.ToolResultContinuationMode))
                    step.ToolResultContinuationMode = CouncilToolResultContinuationMode.SameMemberBounded;
                step.ToolResultContinuationRounds = step.ToolResultContinuationMode == CouncilToolResultContinuationMode.Disabled
                    ? 0
                    : Math.Clamp(step.ToolResultContinuationRounds, 1, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumContinuationRounds);
                step.RoleComplianceRetryCount = Math.Clamp(step.RoleComplianceRetryCount, 0, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumRoleComplianceRetries);
                if (!Enum.IsDefined(typeof(CouncilMemberFailureRecoveryMode), step.MemberFailureRecoveryMode))
                    step.MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool;
                step.MemberFailureRecoveryAttempts = Math.Clamp(step.MemberFailureRecoveryAttempts, 0, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumMemberRecoveryAttempts);
                step.FinalAnswerRecoveryMaxOutputTokens = Math.Clamp(step.FinalAnswerRecoveryMaxOutputTokens, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MinimumFinalRecoveryOutputTokens, runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>(LocalGptRuntimeValue.CouncilExecutionRuntimeParametersJson).MaximumFinalRecoveryOutputTokens);
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
