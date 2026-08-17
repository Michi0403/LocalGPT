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
        /// Builds configured role participant pool as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="assignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IReadOnlyList<string> BuildConfiguredRoleParticipantPool(
            OrganicCouncilRoleDefinition definition,
            IReadOnlyList<string> participants,
            IDictionary<string, CouncilRoleRuntimeAssignment> assignments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(definition.DistinctAiAssignmentGroup))
                    return participants.ToList();

                var alreadyAssigned = assignments.Values
                    .Where(assignment =>
                        assignment.Definition is not null &&
                        string.Equals(
                            assignment.Definition.DistinctAiAssignmentGroup,
                            definition.DistinctAiAssignmentGroup,
                            StringComparison.OrdinalIgnoreCase))
                    .SelectMany(assignment => assignment.AiParticipants)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                return participants
                    .Where(participant => !alreadyAssigned.Contains(participant))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build the configured role participant pool for role {RoleName}.", definition.Role);
                throw;
            }
        }


        /// <summary>
        /// Retrieves configured role pairing reservation multiplier as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="definition">Definition value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="rolesByName">Organic council role definition dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        private int GetConfiguredRolePairingReservationMultiplier(
            OrganicCouncilRoleDefinition definition,
            IReadOnlyDictionary<string, OrganicCouncilRoleDefinition> rolesByName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(definition.PairedRole) ||
                    string.IsNullOrWhiteSpace(definition.DistinctAiAssignmentGroup) ||
                    !rolesByName.TryGetValue(definition.PairedRole, out var pairedRole) ||
                    pairedRole.HumanParticipationMode == HumanParticipationMode.HumanOnly ||
                    !string.Equals(
                        pairedRole.DistinctAiAssignmentGroup,
                        definition.DistinctAiAssignmentGroup,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        pairedRole.MatchAiParticipantCountToRole,
                        definition.Role,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }

                return 2;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to calculate paired-role model reservation for configured role {RoleName} paired with {PairedRoleName}.",
                    definition.Role,
                    definition.PairedRole);
                throw;
            }
        }

        /// <summary>
        /// Chooses a stable pseudo-random participant count for one role and run without imposing a product-specific count ceiling.
        /// </summary>
        /// <param name="runId">Council run identifier used as deterministic entropy.</param>
        /// <param name="teamKey">Stable team key that isolates the selection from other teams.</param>
        /// <param name="roleName">Role name that isolates the selection from other roles.</param>
        /// <param name="minimum">Inclusive configured minimum participant count.</param>
        /// <param name="maximum">Inclusive configured maximum participant count.</param>
        /// <returns>A deterministic count inside the inclusive configured interval.</returns>
        private int DeterministicallySelectConfiguredRoleCount(
            Guid runId,
            string teamKey,
            string roleName,
            int minimum,
            int maximum)
        {
            try
            {
                var normalizedMinimum = Math.Max(1, minimum);
                var normalizedMaximum = Math.Max(normalizedMinimum, maximum);
                var seed = $"{runId:N}|{teamKey}|{roleName}|count";
                var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(seed));
                var value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(hash);
                var range = (ulong)((long)normalizedMaximum - normalizedMinimum + 1L);
                return normalizedMinimum + (int)(value % range);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to select deterministic configured role count for team {TeamKey}, role {RoleName}.", teamKey, roleName);
                throw;
            }
        }

        /// <summary>
        /// Selects provider-bound role participants in deterministic shuffled cycles, allowing deliberate repeated invocations when the requested count exceeds the distinct pool size.
        /// </summary>
        /// <param name="runId">Council run identifier used as deterministic entropy.</param>
        /// <param name="teamKey">Stable team key that isolates selection order from other teams.</param>
        /// <param name="roleName">Role name that isolates selection order from other roles.</param>
        /// <param name="pool">Exact provider-qualified model identities eligible for the role.</param>
        /// <param name="selectedCount">Number of role invocations to produce; it may exceed the number of entries in <paramref name="pool"/>.</param>
        /// <returns>An ordered participant list. Each cycle uses every pool member at most once before another shuffled cycle begins.</returns>
        private IReadOnlyList<string> DeterministicallySelectConfiguredRoleParticipantsWithRepeats(
            Guid runId,
            string teamKey,
            string roleName,
            IReadOnlyList<string> pool,
            int selectedCount)
        {
            try
            {
                if (selectedCount <= 0)
                    return [];
                if (pool.Count == 0)
                    throw new InvalidOperationException($"Role '{roleName}' cannot select repeated provider-bound participants from an empty pool.");

                var selected = new List<string>(selectedCount);
                var seed = $"{runId:N}|{teamKey}|{roleName}|provider-pool";
                for (var cycle = 0; selected.Count < selectedCount; cycle++)
                {
                    var cycleSeed = $"{seed}|cycle|{cycle}";
                    var ordered = pool
                        .OrderBy(model => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                            Encoding.UTF8.GetBytes(cycleSeed + "|member|" + model))))
                        .ThenBy(model => model, StringComparer.OrdinalIgnoreCase);
                    foreach (var model in ordered)
                    {
                        selected.Add(model);
                        if (selected.Count == selectedCount)
                            break;
                    }
                }

                return selected;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to select repeated provider-bound participants for team {TeamKey}, role {RoleName}.", teamKey, roleName);
                throw;
            }
        }

        /// <summary>
        /// Performs deterministically select configured role participants as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="runId">Identifier of the run to use for this operation.</param>
        /// <param name="teamKey">Team key value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleName">Role name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="pool">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="selectedCount">Selected count value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IReadOnlyList<string> DeterministicallySelectConfiguredRoleParticipants(
            Guid runId,
            string teamKey,
            string roleName,
            IReadOnlyList<string> pool,
            int selectedCount)
        {
            try
            {
                var seed = $"{runId:N}|{teamKey}|{roleName}";
                return pool
                    .OrderBy(model => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        Encoding.UTF8.GetBytes(seed + "|member|" + model))))
                    .ThenBy(model => model, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Clamp(selectedCount, 0, pool.Count))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to select deterministic configured role participants for team {TeamKey}, role {RoleName}.", teamKey, roleName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves configured role assignment as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleName">Role name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="participants">String dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="assignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The council role runtime assignment produced by the operation.</returns>
        private CouncilRoleRuntimeAssignment GetConfiguredRoleAssignment(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            string? roleName,
            IReadOnlyList<string> participants,
            IDictionary<string, CouncilRoleRuntimeAssignment> assignments)
        {
            try
            {
                var normalizedRole = string.IsNullOrWhiteSpace(roleName) ? "Council participant" : roleName.Trim();
                if (assignments.TryGetValue(normalizedRole, out var assignment))
                    return assignment;

                if (team.Roles.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Workflow role '{normalizedRole}' is not present in team '{team.DisplayName}'. LocalGPT will not let a configured round escape its saved role structure.");
                }

                assignment = new CouncilRoleRuntimeAssignment(normalizedRole, null, participants.ToList());
                assignments[normalizedRole] = assignment;
                logger.LogWarning(
                    "Council run {RunId} team {TeamKey} has no role definitions; workflow role {RoleName} uses all selected models for compatibility.",
                    result.RunId,
                    team.Key,
                    normalizedRole);
                return assignment;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed to obtain workflow role assignment {RoleName}.", result.RunId, roleName);
                throw;
            }
        }

        /// <summary>
        /// Builds configured role pairings as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="assignments">Council role runtime assignment dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The collection produced by the operation.</returns>
        private IReadOnlyList<CouncilParticipantPairing> BuildConfiguredRolePairings(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            IReadOnlyDictionary<string, CouncilRoleRuntimeAssignment> assignments)
        {
            try
            {
                var pairings = new List<CouncilParticipantPairing>();
                var processedRolePairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var role in team.Roles.Where(role => !string.IsNullOrWhiteSpace(role.PairedRole)))
                {
                    var leftRole = role.Role.Trim();
                    var rightRole = role.PairedRole.Trim();
                    var rolePairKey = string.Compare(leftRole, rightRole, StringComparison.OrdinalIgnoreCase) <= 0
                        ? $"{leftRole}|{rightRole}"
                        : $"{rightRole}|{leftRole}";
                    if (!processedRolePairs.Add(rolePairKey))
                        continue;
                    if (!assignments.TryGetValue(leftRole, out var leftAssignment) ||
                        !assignments.TryGetValue(rightRole, out var rightAssignment))
                    {
                        var warning = $"Configured role pairing '{leftRole}' ↔ '{rightRole}' could not be created because one role has no runtime assignment.";
                        if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                            result.Warnings.Add(warning);
                        continue;
                    }

                    var pairCount = Math.Min(leftAssignment.AiParticipants.Count, rightAssignment.AiParticipants.Count);
                    if (pairCount == 0)
                    {
                        var warning = $"Configured role pairing '{leftRole}' ↔ '{rightRole}' has no AI participants to pair in this run.";
                        if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                            result.Warnings.Add(warning);
                        continue;
                    }
                    if (leftAssignment.AiParticipants.Count != rightAssignment.AiParticipants.Count)
                    {
                        var warning = $"Configured role pairing '{leftRole}' ↔ '{rightRole}' has unequal AI counts ({leftAssignment.AiParticipants.Count} and {rightAssignment.AiParticipants.Count}); only {pairCount} one-to-one pair(s) were created.";
                        if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                            result.Warnings.Add(warning);
                    }

                    for (var index = 0; index < pairCount; index++)
                    {
                        var leftParticipant = leftAssignment.AiParticipants[index];
                        var rightParticipant = rightAssignment.AiParticipants[index];
                        pairings.Add(new CouncilParticipantPairing(leftRole, leftParticipant, rightRole, rightParticipant));
                        pairings.Add(new CouncilParticipantPairing(rightRole, rightParticipant, leftRole, leftParticipant));
                    }
                }

                if (pairings.Count > 0)
                {
                    var summary = BuildConfiguredRolePairingSummary(pairings);
                    request.ProgressMessage?.Invoke($"Runtime role pairings:{Environment.NewLine}{summary}");
                    logger.LogInformation(
                        "Council run {RunId} created {PairingCount} directional participant pairing(s) for team {TeamKey}: {PairingSummary}",
                        result.RunId,
                        pairings.Count,
                        team.Key,
                        summary.Replace(Environment.NewLine, " | ", StringComparison.Ordinal));
                }

                return pairings;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council run {RunId} failed to build role pairings for team {TeamKey}.", result.RunId, team.Key);
                throw;
            }
        }

        /// <summary>
        /// Builds configured role pairing summary as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="pairings">Council participant pairing dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredRolePairingSummary(IReadOnlyList<CouncilParticipantPairing> pairings)
        {
            try
            {
                var unique = pairings
                    .Where(pairing => string.Compare(pairing.RoleName, pairing.PairedRoleName, StringComparison.OrdinalIgnoreCase) <= 0)
                    .Select(pairing => $"- {pairing.RoleName} {pairing.Participant} ↔ {pairing.PairedRoleName} {pairing.PairedParticipant}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return unique.Count == 0
                    ? "No one-to-one role pairings are configured for this run."
                    : string.Join(Environment.NewLine, unique);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build configured role pairing summary.");
                throw;
            }
        }

        /// <summary>
        /// Performs describe configured role AI policy as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="role">Role value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string DescribeConfiguredRoleAiPolicy(OrganicCouncilRoleDefinition role)
        {
    try
    {
                if (role.HumanParticipationMode == HumanParticipationMode.HumanOnly)
                    return "human only; no AI model";
                if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected)
                    return "all selected council AIs";
                if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels)
                    return $"{role.AssignedModelKeys.Count} exact provider-bound AI model(s)";
                if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                {
                    var countText = role.MinimumAiParticipants == role.MaximumAiParticipants
                        ? Math.Max(1, role.MinimumAiParticipants).ToString()
                        : $"{Math.Max(1, role.MinimumAiParticipants)}-{Math.Max(role.MinimumAiParticipants, role.MaximumAiParticipants)}";
                    return $"deterministic-random {countText} invocation(s) from {role.AssignedModelKeys.Count} exact provider-bound AI model(s), cycling the pool when needed";
                }
                return role.MinimumAiParticipants == role.MaximumAiParticipants
                    ? $"deterministic-random {Math.Max(1, role.MinimumAiParticipants)} AI member(s) per run"
                    : $"deterministic-random {Math.Max(1, role.MinimumAiParticipants)}-{Math.Max(role.MinimumAiParticipants, role.MaximumAiParticipants)} AI member(s) per run";
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(DescribeConfiguredRoleAiPolicy)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(DescribeConfiguredRoleAiPolicy)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds configured role performance instruction as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="performanceMode">Performance mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleName">Role name value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredRolePerformanceInstruction(
            CouncilRolePerformanceMode performanceMode,
            string modelName,
            string roleName) {
    try
    {
        return performanceMode switch
        {
            CouncilRolePerformanceMode.ImprovisationPlayer =>
                $"You are AI kernel '{modelName}', a genuine improvisation player performing the assigned role '{roleName}' inside the configured fictional scene. " +
                "You are not an NPC or a passive narrator. Make creative, bounded choices for your own role, preserve continuity, react to other players, and remain aware that the world, prizes, creatures and consequences are fictional. " +
                "Do not seize another participant's role, decide another player's action, or step outside the scenario to redesign the workflow unless the role explicitly requires it.",
            _ =>
                $"Work as AI kernel '{modelName}' in the bounded task-specialist role '{roleName}'. Stay within that role's responsibility and do not take over another role."
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRolePerformanceInstruction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRolePerformanceInstruction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds configured role boundary instruction as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="boundaryMode">Boundary mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleName">Role name value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredRoleBoundaryInstruction(CouncilRoleBoundaryMode boundaryMode, string roleName) {
    try
    {
        return boundaryMode switch
        {
            CouncilRoleBoundaryMode.Strict =>
                $"Strict role ownership is active for '{roleName}'. Speak and act only for this role. Do not narrate another participant's private thinking, choose another player's move, issue a ruling reserved for another role, or manufacture another role's dialogue or outcome.",
            CouncilRoleBoundaryMode.Collaborative =>
                $"Collaborative role boundaries are active for '{roleName}'. You may offer clearly labeled suggestions to neighboring roles, but you may not perform their choices, speak as them, or convert a suggestion into an accomplished action.",
            _ =>
                $"Bounded role ownership is active for '{roleName}'. Stay inside this role's responsibility, refer to other participants only as shared context, and never decide their actions or outcomes."
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleBoundaryInstruction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleBoundaryInstruction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds configured role language instruction as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="languageMode">Language mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredRoleLanguageInstruction(CouncilRoleLanguageMode languageMode) {
    try
    {
        return languageMode switch
        {
            CouncilRoleLanguageMode.SenderLanguage =>
                "Use the natural language of the latest human sender message for both visible output and any thinking text the model exposes. Preserve identifiers, code, names and quoted commands unchanged. If the latest human message is mixed-language, follow its dominant language.",
            CouncilRoleLanguageMode.English =>
                "Use English for visible output and any thinking text the model exposes, while preserving identifiers, code, names and quoted commands unchanged.",
            _ =>
                "Choose the response language that best fits the current conversation, while preserving identifiers, code, names and quoted commands unchanged."
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleLanguageInstruction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleLanguageInstruction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds configured role human participation instruction as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="mode">Mode value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string BuildConfiguredRoleHumanParticipationInstruction(HumanParticipationMode mode) {
    try
    {
        return mode switch
        {
            HumanParticipationMode.Optional =>
                "A human may optionally send a current role command or improvisation cue. Use a clearly targeted current human message when present; otherwise continue autonomously without asking, blocking or inventing a human command.",
            HumanParticipationMode.Required =>
                "A current human response is required before this role continues. Use the approved human response as guidance without treating it as proof that an outcome already happened.",
            HumanParticipationMode.HumanOnly =>
                "This role belongs to the human participant. Do not simulate the missing human decision.",
            _ =>
                "No human turn is configured for this role. Continue autonomously and do not ask the user to choose commands unless the workflow prompt explicitly creates a decision checkpoint."
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleHumanParticipationInstruction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(BuildConfiguredRoleHumanParticipationInstruction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs wait for configured role human participation as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="assignment">Assignment value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="round">Round value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="repeatIndex">Repeat index value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task WaitForConfiguredRoleHumanParticipationAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CouncilRoleRuntimeAssignment assignment,
            int round,
            string phase,
            int repeatIndex,
            CancellationToken cancellationToken)
        {
    try
    {
                var profile = await humanCollaboration.GetProfileAsync(cancellationToken).ConfigureAwait(false);
                if (!profile.IsEnabled)
                {
                    var warning = $"Role '{assignment.RoleName}' requires a human response while the human participant profile is disabled. The run remains safely paused in the Human Collaboration Inbox until a local human answers.";
                    if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                        result.Warnings.Add(warning);
                }

                var roleSeed = $"{result.RunId:N}|{team.Key}|{assignment.RoleName}|{round}|{repeatIndex}";
                var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(roleSeed))).ToLowerInvariant();
                var roleKey = fingerprint[..16];
                var requestedAtUtc = DateTime.UtcNow;
                var spec = new HumanApprovalRequestSpec(
                    CorrelationId: $"council:role:{result.RunId:N}:{round}:{repeatIndex}:{roleKey}",
                    OperationKey: $"council.role.response.{roleKey}.{round}.{repeatIndex}",
                    Title: $"{assignment.RoleName} response required — {phase}",
                    Description: $"The configured council team '{team.DisplayName}' requires a local human to participate as '{assignment.RoleName}' before this round can continue.",
                    RiskLevel: "Low",
                    Source: nameof(MultiModelCouncilService),
                    RequestedBy: team.DisplayName,
                    RequestedRole: assignment.RoleName,
                    CouncilRunId: result.RunId,
                    EarliestCouncilRound: round,
                    RequiredBeforeCompletion: false,
                    IsSensitive: false,
                    RequestKind: vocabulary.Get().HumanRequestGuidance,
                    SuggestedResponsesText: string.Empty,
                    ResponsePrompt: $"Respond as the '{assignment.RoleName}' role for {phase}. The response enters the council transcript as peer evidence.",
                    PrefillText: string.Join(
                        Environment.NewLine,
                        new[]
                        {
                            $"Role: {assignment.RoleName}",
                            string.IsNullOrWhiteSpace(assignment.Definition?.Expertise) ? string.Empty : $"Expertise/viewpoint: {assignment.Definition.Expertise}",
                            string.IsNullOrWhiteSpace(assignment.Definition?.Responsibility) ? string.Empty : $"Responsibility: {assignment.Definition.Responsibility}"
                        }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    AllowFreeText: true,
                    ParameterFingerprint: fingerprint,
                    QuestionScope: "Member",
                    GateMode: "None",
                    TargetMembersText: profile.DisplayName,
                    RequestedCouncilRound: round,
                    RequestedCouncilPhase: phase);

                var waitingStepAdded = false;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(spec, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (gate.IsAuthorized)
                    {
                        if (string.IsNullOrWhiteSpace(gate.UserResponse))
                            throw new InvalidOperationException($"The required human response for role '{assignment.RoleName}' was empty.");

                        humanCollaboration.UpdateCouncilRun(result.RunId, round, phase);
                        var humanStep = new MultiModelCouncilStep
                        {
                            Round = round,
                            Phase = phase,
                            ModelName = $"Human: {profile.DisplayName}",
                            CouncilMembers = [.. result.ModelNames, $"Human: {profile.DisplayName}"],
                            Role = assignment.RoleName,
                            Content = gate.UserResponse.Trim(),
                            VisibleContent = gate.UserResponse.Trim(),
                            StartedAtUtc = requestedAtUtc,
                            CompletedAtUtc = DateTime.UtcNow,
                            DurationSeconds = Math.Max(0, (DateTime.UtcNow - requestedAtUtc).TotalSeconds)
                        };
                        MultiModelCouncilServiceAddOrderedStep(result, humanStep, logger);
                        request.StepCompleted?.Invoke(humanStep);
                        request.ProgressMessage?.Invoke(
                            $"Human participant {profile.DisplayName} completed required role '{assignment.RoleName}' for round {round} / {phase}. The response is now peer evidence in the transcript.");
                        logger.LogInformation(
                            "Council run {RunId} received the required human response for role {RoleName}, round {Round}, phase {Phase}; response content was omitted from logs.",
                            result.RunId,
                            assignment.RoleName,
                            round,
                            phase);
                        return;
                    }

                    if (gate.IsDeclined)
                        throw new InvalidOperationException($"The local human declined required role '{assignment.RoleName}' for {phase}.");

                    if (!waitingStepAdded)
                    {
                        var visible = $"Council paused for required human role '{assignment.RoleName}' before {phase}. Answer the waiting guidance request in Approvals & team to continue.";
                        var waitingStep = new MultiModelCouncilStep
                        {
                            Round = round,
                            Phase = phase,
                            ModelName = "LocalGPT: required human role",
                            CouncilMembers = [.. result.ModelNames, $"Human: {profile.DisplayName}"],
                            Role = assignment.RoleName,
                            Content = visible,
                            VisibleContent = visible,
                            StartedAtUtc = DateTime.UtcNow,
                            CompletedAtUtc = DateTime.UtcNow,
                            DurationSeconds = 0
                        };
                        MultiModelCouncilServiceAddOrderedStep(result, waitingStep, logger);
                        request.StepCompleted?.Invoke(waitingStep);
                        request.ProgressMessage?.Invoke(visible);
                        waitingStepAdded = true;
                    }

                    humanCollaboration.UpdateCouncilRun(
                        result.RunId,
                        round,
                        $"Awaiting human role: {assignment.RoleName}",
                        true);

                    var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    void HandleChanged() => changed.TrySetResult(true);
                    humanCollaboration.Changed += HandleChanged;
                    try
                    {
                        var fallback = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        await Task.WhenAny(changed.Task, fallback).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    finally
                    {
                        humanCollaboration.Changed -= HandleChanged;
                    }
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(WaitForConfiguredRoleHumanParticipationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(WaitForConfiguredRoleHumanParticipationAsync)} failed.");
        throw;
    }
}

    }
}
