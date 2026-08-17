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
        /// Performs uses built in council workflow as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool UsesBuiltInCouncilWorkflow(OrganicCouncilTeamDefinition team)
        {
    try
    {
                if (!team.IsSystemSeed || team.IsUserModified)
                    return false;

                var expected = new Dictionary<string, (int SortOrder, string ExecutionMode)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["member-readiness-introduction"] = (5, "AllMembersParallel"),
                    ["expert-preparation"] = (10, "LeaderSingle"),
                    ["leader-synthesis"] = (20, "LeaderSingle"),
                    ["member-proposals"] = (30, "AllMembersParallel"),
                    ["peer-review"] = (40, "AllMembersParallel"),
                    ["consensus"] = (50, "LeaderSingle")
                };
                var enabled = team.WorkflowSteps.Where(step => step.IsEnabled).ToList();
                if (enabled.Count != expected.Count || enabled.Any(step => !step.UseBuiltInBehavior || step.RepeatCount != 1))
                    return false;

                foreach (var step in enabled)
                {
                    if (!expected.TryGetValue(step.Key, out var contract))
                        return false;
                    if (step.SortOrder != contract.SortOrder || !string.Equals(NormalizeConfiguredExecutionMode(step.ExecutionMode), contract.ExecutionMode, StringComparison.Ordinal))
                        return false;
                }

                return true;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(UsesBuiltInCouncilWorkflow)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(UsesBuiltInCouncilWorkflow)} failed.");
        throw;
    }
}

        /// <summary>Runs the explicitly configured role-aware all-members readiness preflight without making it part of substantive workflow state.</summary>
        /// <param name="result">Council result that owns the visible preflight evidence.</param>
        /// <param name="request">Current Council request.</param>
        /// <param name="team">Configured social team.</param>
        /// <param name="baseUri">Default provider base URI.</param>
        /// <param name="participants">All selected provider-qualified Council members.</param>
        /// <param name="bootstrap">Existing run bootstrap context.</param>
        /// <param name="modelRoutes">Resolved hardware-road plans.</param>
        /// <param name="keepAlive">Provider keep-alive value.</param>
        /// <param name="ollamaNumGpu">Optional Ollama GPU-layer override.</param>
        /// <param name="maxContextTokens">Run context-token ceiling.</param>
        /// <param name="modelTimeoutSeconds">Per-model timeout.</param>
        /// <param name="roleAssignments">Resolved configured role assignments for the run.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task that completes after every selected member has finished or failed its optional preflight probe.</returns>
        private async Task RunConfiguredAllMembersReadinessPreflightAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            string baseUri,
            IReadOnlyList<string> participants,
            string bootstrap,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            IReadOnlyDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments,
            CancellationToken cancellationToken)
        {
            try
            {
                var preflightBootstrap = await PrepareHumanHeartbeatAsync(
                    result,
                    request,
                    0,
                    "Team preflight",
                    bootstrap,
                    cancellationToken).ConfigureAwait(false);
                var maxOutputTokens = Math.Min(
                    request.MaxOutputTokens,
                    Math.Clamp(team.AllMembersReadinessPreflightMaxOutputTokens, 32, 2048));

                request.ProgressMessage?.Invoke(
                    $"Running optional all-members readiness preflight for team {team.DisplayName}: {participants.Count} selected member(s), role-aware probe, maximum {maxOutputTokens} output token(s) per member. Preflight output is {(team.IncludeAllMembersReadinessPreflightInWorkflowContext ? "included in" : "excluded from")} later workflow model context.");

                await RunPhaseAsync(
                    result,
                    baseUri,
                    participants,
                    round: 0,
                    phase: "Team preflight",
                    role: "All-members readiness preflight",
                    promptFactory: modelName => BuildConfiguredAllMembersReadinessPrompt(modelName, team, roleAssignments),
                    preflightBootstrap,
                    maxOutputTokens,
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
                    councilMembers: participants,
                    sequentialPerHost: true).ConfigureAwait(false);
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RunConfiguredAllMembersReadinessPreflightAsync)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(RunConfiguredAllMembersReadinessPreflightAsync)} failed.");
                throw;
            }
        }

        /// <summary>Builds one compact team preflight prompt from the member's actual configured role assignments.</summary>
        /// <param name="modelName">Provider-qualified member identity.</param>
        /// <param name="team">Configured social team.</param>
        /// <param name="roleAssignments">Resolved role assignments for the run.</param>
        /// <returns>A compact preflight-only prompt that does not ask the member to execute substantive role work.</returns>
        private string BuildConfiguredAllMembersReadinessPrompt(
            string modelName,
            OrganicCouncilTeamDefinition team,
            IReadOnlyDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments)
        {
            try
            {
                var assigned = roleAssignments.Values
                    .Where(assignment => assignment.AiParticipants.Contains(modelName, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(assignment => assignment.RoleName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var assignedRoles = assigned.Count == 0
                    ? "none"
                    : string.Join(", ", assigned.Select(assignment => assignment.RoleName));
                var responsibilities = assigned.Count == 0
                    ? "No AI workflow role is assigned to this member in the current run. Report that as a preflight blocker."
                    : string.Join(Environment.NewLine, assigned.Select(assignment =>
                        $"- {assignment.RoleName}: {(string.IsNullOrWhiteSpace(assignment.Definition?.Responsibility) ? "follow the configured workflow-step role task exactly" : assignment.Definition.Responsibility)}"));

                var configuredTemplate = team.AllMembersReadinessPreflightPromptTemplate?.Trim() ?? string.Empty;
                var prompt = string.IsNullOrWhiteSpace(configuredTemplate)
                    ? $"""
This is an optional team readiness preflight only. Do not execute the user's original request and do not perform the substantive workflow tasks yet.
Provider-qualified member: {modelName}
Team: {team.DisplayName}
Assigned role(s): {assignedRoles}
Assigned role responsibilities:
{responsibilities}

Confirm only whether you can later execute the role tasks listed above. Do not plan the whole Council, do not take over another role, do not call tools, and do not produce benchmark/profile results during this preflight.
Return exactly three short lines:
READINESS: Ready | Blocked
ROLES: <the assigned role names you understand>
BLOCKERS: none | <specific missing capability or ambiguity>
"""
                    : configuredTemplate
                        .Replace("{{ModelName}}", modelName, StringComparison.Ordinal)
                        .Replace("{{TeamName}}", team.DisplayName, StringComparison.Ordinal)
                        .Replace("{{AssignedRoles}}", assignedRoles, StringComparison.Ordinal)
                        .Replace("{{RoleResponsibilities}}", responsibilities, StringComparison.Ordinal);

                return prompt.Trim() + Environment.NewLine + Environment.NewLine +
                    "Preflight boundary: the current role task remains authoritative when substantive workflow execution starts. The original user request is background context only and must not replace an assigned role task.";
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredAllMembersReadinessPrompt)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredAllMembersReadinessPrompt)} failed.");
                throw;
            }
        }

        /// <summary>Returns whether a Council step belongs to the explicit configurable all-members preflight phase.</summary>
        /// <param name="step">Council step to inspect.</param>
        /// <returns><see langword="true"/> when the step is explicit preflight evidence.</returns>
        private bool IsConfiguredAllMembersReadinessPreflightStep(MultiModelCouncilStep step)
        {
            try
            {
                return string.Equals(step.Phase, "Team preflight", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(step.Role, "All-members readiness preflight", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(IsConfiguredAllMembersReadinessPreflightStep)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(IsConfiguredAllMembersReadinessPreflightStep)} failed.");
                throw;
            }
        }

        /// <summary>Filters explicit preflight chatter out of later model context unless the team explicitly opted in.</summary>
        /// <param name="steps">Council steps accumulated so far.</param>
        /// <param name="team">Configured social team.</param>
        /// <returns>The steps allowed into later model prompt context.</returns>
        private IReadOnlyList<MultiModelCouncilStep> GetCouncilWorkflowContextSteps(
            IEnumerable<MultiModelCouncilStep> steps,
            OrganicCouncilTeamDefinition team)
        {
            try
            {
                return team.IncludeAllMembersReadinessPreflightInWorkflowContext
                    ? steps.ToList()
                    : steps.Where(step => !IsConfiguredAllMembersReadinessPreflightStep(step)).ToList();
            }
            catch (Exception __serviceMethodException)
            {
                if (__serviceMethodException is OperationCanceledException)
                    logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(GetCouncilWorkflowContextSteps)} was canceled.");
                else
                    logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(GetCouncilWorkflowContextSteps)} failed.");
                throw;
            }
        }

        /// <summary>
        /// Builds configured role assignments as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The dictionary string council role runtime assignment produced by the operation.</returns>
        private Dictionary<string, CouncilRoleRuntimeAssignment> BuildConfiguredRoleAssignments(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            IReadOnlyList<string> participants)
        {
            try
            {
                var rolesByName = team.Roles
                    .Where(role => !string.IsNullOrWhiteSpace(role.Role))
                    .ToDictionary(role => role.Role.Trim(), StringComparer.OrdinalIgnoreCase);
                var assignments = new Dictionary<string, CouncilRoleRuntimeAssignment>(StringComparer.OrdinalIgnoreCase);
                var activeRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var roleName in rolesByName.Keys)
                {
                    ResolveConfiguredRoleAssignment(
                        result,
                        request,
                        team,
                        roleName,
                        participants,
                        rolesByName,
                        assignments,
                        activeRoles);
                }

                logger.LogInformation(
                    "Council run {RunId} created {RoleAssignmentCount} configured role assignment(s) for team {TeamKey}.",
                    result.RunId,
                    assignments.Count,
                    team.Key);
                return assignments;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} could not create configured role assignments for team {TeamKey}; selected model count {ParticipantCount}.",
                    result.RunId,
                    team.Key,
                    participants.Count);
                throw;
            }
        }

        /// <summary>
        /// Resolves configured role assignment as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleName">Role name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="rolesByName">Organic council role definition dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="assignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="activeRoles">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The council role runtime assignment produced by the operation.</returns>
        private CouncilRoleRuntimeAssignment ResolveConfiguredRoleAssignment(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            string roleName,
            IReadOnlyList<string> participants,
            IReadOnlyDictionary<string, OrganicCouncilRoleDefinition> rolesByName,
            IDictionary<string, CouncilRoleRuntimeAssignment> assignments,
            ISet<string> activeRoles)
        {
            try
            {
                var normalizedRole = string.IsNullOrWhiteSpace(roleName) ? "Council participant" : roleName.Trim();
                if (assignments.TryGetValue(normalizedRole, out var existing))
                    return existing;

                if (!rolesByName.TryGetValue(normalizedRole, out var definition))
                {
                    if (rolesByName.Count > 0)
                    {
                        throw new InvalidOperationException(
                            $"Workflow role '{normalizedRole}' has no exact saved role policy in team '{team.DisplayName}'. LocalGPT will not assign unrelated Council models to an undefined role.");
                    }

                    var fallback = new CouncilRoleRuntimeAssignment(normalizedRole, null, participants.ToList());
                    assignments[normalizedRole] = fallback;
                    logger.LogWarning(
                        "Council run {RunId} team {TeamKey} has no saved role policies; workflow role {RoleName} uses all selected AI models for compatibility.",
                        result.RunId,
                        team.Key,
                        normalizedRole);
                    return fallback;
                }

                if (!activeRoles.Add(normalizedRole))
                    throw new InvalidOperationException($"Role AI participant count references contain a runtime cycle involving '{normalizedRole}'.");

                try
                {
                    IReadOnlyList<string> selectedAiParticipants;
                    if (definition.HumanParticipationMode == HumanParticipationMode.HumanOnly)
                    {
                        selectedAiParticipants = [];
                    }
                    else if (definition.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected)
                    {
                        selectedAiParticipants = participants.ToList();
                    }
                    else if (definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels)
                    {
                        var configuredModelKeys = definition.AssignedModelKeys
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Select(value => value.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        if (configuredModelKeys.Count == 0)
                            throw new InvalidOperationException($"Role '{normalizedRole}' has provider-bound AI assignment enabled but no model identity is saved.");

                        var missingModelKeys = configuredModelKeys
                            .Where(value => !participants.Contains(value, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        if (missingModelKeys.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Role '{normalizedRole}' requires provider-bound model(s) {string.Join(", ", missingModelKeys)}, but they are unavailable in this run. Refresh provider models or update the team assignment; LocalGPT will not substitute another host or model.");
                        }

                        selectedAiParticipants = configuredModelKeys
                            .Where(value => participants.Contains(value, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                    }
                    else if (definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                    {
                        var configuredModelKeys = definition.AssignedModelKeys
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Select(value => value.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        if (configuredModelKeys.Count == 0)
                            throw new InvalidOperationException($"Role '{normalizedRole}' has random provider-pool assignment enabled but no model identity is saved.");

                        var missingModelKeys = configuredModelKeys
                            .Where(value => !participants.Contains(value, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        if (missingModelKeys.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Role '{normalizedRole}' requires provider-bound model(s) {string.Join(", ", missingModelKeys)}, but they are unavailable in this run. Refresh provider models or update the team assignment; LocalGPT will not substitute another host or model.");
                        }

                        var rolePool = BuildConfiguredRoleParticipantPool(definition, participants, assignments)
                            .Where(value => configuredModelKeys.Contains(value, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        if (rolePool.Count == 0)
                            throw new InvalidOperationException($"Role '{normalizedRole}' has no available provider-bound model after distinct-role exclusions.");

                        var requestedMinimum = Math.Max(1, definition.MinimumAiParticipants);
                        var requestedMaximum = Math.Max(requestedMinimum, definition.MaximumAiParticipants);
                        int selectedCount;
                        if (!string.IsNullOrWhiteSpace(definition.MatchAiParticipantCountToRole))
                        {
                            var matched = ResolveConfiguredRoleAssignment(
                                result,
                                request,
                                team,
                                definition.MatchAiParticipantCountToRole,
                                participants,
                                rolesByName,
                                assignments,
                                activeRoles);
                            selectedCount = matched.AiParticipants.Count;
                            if (selectedCount <= 0)
                            {
                                throw new InvalidOperationException(
                                    $"Role '{normalizedRole}' matches its AI participant count to role '{matched.RoleName}', but that role has no AI participant in this run.");
                            }
                        }
                        else
                        {
                            selectedCount = DeterministicallySelectConfiguredRoleCount(
                                result.RunId, team.Key, normalizedRole, requestedMinimum, requestedMaximum);
                        }

                        var pairingReservationMultiplier = GetConfiguredRolePairingReservationMultiplier(definition, rolesByName);
                        if (pairingReservationMultiplier > 1 && selectedCount > rolePool.Count / pairingReservationMultiplier)
                        {
                            throw new InvalidOperationException(
                                $"Role '{normalizedRole}' requests {selectedCount} provider-pool invocation(s), but its paired-role contract requires distinct partner capacity. " +
                                "Lower this role count, enlarge the exact provider pool, or remove the distinct pairing requirement.");
                        }

                        selectedAiParticipants = DeterministicallySelectConfiguredRoleParticipantsWithRepeats(
                            result.RunId, team.Key, normalizedRole, rolePool, selectedCount);
                    }
                    else
                    {
                        var requestedMinimum = Math.Max(1, definition.MinimumAiParticipants);
                        var requestedMaximum = Math.Max(requestedMinimum, definition.MaximumAiParticipants);
                        int selectedCount;

                        if (!string.IsNullOrWhiteSpace(definition.MatchAiParticipantCountToRole))
                        {
                            var matched = ResolveConfiguredRoleAssignment(
                                result,
                                request,
                                team,
                                definition.MatchAiParticipantCountToRole,
                                participants,
                                rolesByName,
                                assignments,
                                activeRoles);
                            selectedCount = matched.AiParticipants.Count;
                            if (selectedCount <= 0)
                            {
                                throw new InvalidOperationException(
                                    $"Role '{normalizedRole}' matches its AI participant count to role '{matched.RoleName}', but that role has no AI participant in this run.");
                            }
                        }
                        else
                        {
                            selectedCount = -1;
                        }

                        var pool = BuildConfiguredRoleParticipantPool(definition, participants, assignments);
                        var pairingReservationMultiplier = GetConfiguredRolePairingReservationMultiplier(definition, rolesByName);
                        var selectablePoolCount = pairingReservationMultiplier <= 1
                            ? pool.Count
                            : pool.Count / pairingReservationMultiplier;
                        if (selectedCount < 0)
                        {
                            if (selectablePoolCount < requestedMinimum)
                            {
                                if (!string.IsNullOrWhiteSpace(definition.DistinctAiAssignmentGroup))
                                {
                                    var alreadyUsed = participants.Count - pool.Count;
                                    var pairedReservation = pairingReservationMultiplier > 1
                                        ? $" and must reserve one distinct paired AI for each '{normalizedRole}' member"
                                        : string.Empty;
                                    throw new InvalidOperationException(
                                        $"Role '{normalizedRole}' requires at least {requestedMinimum} unused AI model(s) in distinct assignment group '{definition.DistinctAiAssignmentGroup}'{pairedReservation}, " +
                                        $"but the {pool.Count} remaining model(s) support only {selectablePoolCount}. Select more distinct models or lower the saved role range.");
                                }

                                var warning = $"Role '{normalizedRole}' requests at least {requestedMinimum} AI members, but this run supports only {selectablePoolCount} after paired-role reservations. All available capacity is used for that role.";
                                if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                                    result.Warnings.Add(warning);
                            }

                            var effectiveMinimum = Math.Min(requestedMinimum, selectablePoolCount);
                            var effectiveMaximum = Math.Min(requestedMaximum, selectablePoolCount);
                            if (effectiveMaximum <= 0)
                                throw new InvalidOperationException($"Role '{normalizedRole}' has no available AI model in this run after paired-role reservations.");

                            selectedCount = DeterministicallySelectConfiguredRoleCount(
                                result.RunId, team.Key, normalizedRole, effectiveMinimum, effectiveMaximum);
                        }

                        if (selectedCount > selectablePoolCount)
                        {
                            var alreadyUsed = participants.Count - pool.Count;
                            var pairedReservation = pairingReservationMultiplier > 1
                                ? $" while reserving {selectedCount} distinct paired-role model(s)"
                                : string.Empty;
                            throw new InvalidOperationException(
                                $"Role '{normalizedRole}' requires {selectedCount} distinct AI model(s){pairedReservation}, but only {pool.Count} remain after {alreadyUsed} model(s) were assigned. " +
                                $"Select more distinct models for team '{team.DisplayName}'.");
                        }

                        selectedAiParticipants = DeterministicallySelectConfiguredRoleParticipants(
                            result.RunId,
                            team.Key,
                            normalizedRole,
                            pool,
                            selectedCount);
                    }

                    var assignment = new CouncilRoleRuntimeAssignment(normalizedRole, definition, selectedAiParticipants);
                    assignments[normalizedRole] = assignment;
                    logger.LogInformation(
                        "Council run {RunId} assigned role {RoleName} to AI members {AiMembers} with human mode {HumanMode}, distinct group {DistinctGroup}, matched-count role {MatchedRole} and paired role {PairedRole}.",
                        result.RunId,
                        normalizedRole,
                        selectedAiParticipants.Count == 0 ? "none" : string.Join(", ", selectedAiParticipants),
                        assignment.HumanParticipationMode,
                        string.IsNullOrWhiteSpace(definition.DistinctAiAssignmentGroup) ? "none" : definition.DistinctAiAssignmentGroup,
                        string.IsNullOrWhiteSpace(definition.MatchAiParticipantCountToRole) ? "none" : definition.MatchAiParticipantCountToRole,
                        string.IsNullOrWhiteSpace(definition.PairedRole) ? "none" : definition.PairedRole);
                    request.ProgressMessage?.Invoke(
                        $"Role assignment '{normalizedRole}': {assignment.AiSelectionDescription}; human participation {assignment.HumanParticipationMode}." +
                        (string.IsNullOrWhiteSpace(definition.DistinctAiAssignmentGroup)
                            ? string.Empty
                            : $" Distinct model group: {definition.DistinctAiAssignmentGroup}.") +
                        (string.IsNullOrWhiteSpace(definition.PairedRole)
                            ? string.Empty
                            : $" Paired role: {definition.PairedRole}."));
                    return assignment;
                }
                finally
                {
                    activeRoles.Remove(normalizedRole);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} failed while resolving configured role assignment {RoleName} for team {TeamKey}.",
                    result.RunId,
                    roleName,
                    team.Key);
                throw;
            }
        }

    }
}
