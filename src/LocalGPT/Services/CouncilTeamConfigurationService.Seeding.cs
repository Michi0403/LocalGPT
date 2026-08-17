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
    /// <summary>Applies missing seed teams and seed-version updates without replacing user-modified definitions.</summary>
    /// <param name="cancellationToken">Cancels the asynchronous seed operation.</param>
    /// <returns>A task that completes when the catalog is current.</returns>
    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var existingRows = await db.CouncilTeamConfigurations.ToListAsync(cancellationToken).ConfigureAwait(false);
            var changed = false;

            // The first experimental arena preset used a third-party franchise name. Preserve user-edited
            // rows, but migrate the untouched system seed to LocalGPT's original kernel-creature identity.
            var legacyArena = existingRows.SingleOrDefault(item =>
                string.Equals(item.Key, "pokemon-tournament", StringComparison.OrdinalIgnoreCase));
            var modernArenaExists = existingRows.Any(item =>
                string.Equals(item.Key, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase));
            if (legacyArena is { IsSystemSeed: true, IsUserModified: false } && !modernArenaExists)
            {
                legacyArena.Key = "kernel-creature-tournament";
                legacyArena.SeedVersion = 0;
                legacyArena.UpdatedAtUtc = DateTime.UtcNow;
                changed = true;
            }

            var existing = existingRows.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var definition in seedData.CreateDefaultTeams())
            {
                NormalizeSeedDefaults(definition);
                if (!existing.TryGetValue(definition.Key, out var row))
                {
                    row = new CouncilTeamConfiguration
                    {
                        Id = Guid.NewGuid(),
                        Key = definition.Key,
                        IsSystemSeed = true,
                        IsUserModified = false,
                        IsEnabled = true,
                        SeedVersion = CurrentSeedVersion,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    ApplyDefinition(row, definition);
                    db.CouncilTeamConfigurations.Add(row);
                    changed = true;
                    continue;
                }

                if (row.IsDeleted)
                {
                    // An explicit deletion tombstone is user-owned state. Keep the supplied template available
                    // through GetDefaultTemplatesAsync, but never silently resurrect the configured row.
                    continue;
                }

                if (row.IsSystemSeed && row.IsUserModified)
                {
                    var customKey = CreateUniqueUserCopyKey(
                        row.Key,
                        existingRows
                            .Select(item => item.Key)
                            .Concat(db.CouncilTeamConfigurations.Local.Select(item => item.Key))
                            .ToList());
                    var preservedDefinition = ToDefinition(row);
                    var preservedCopy = CloneAsUserOwnedDefinition(preservedDefinition, customKey);
                    var customRow = new CouncilTeamConfiguration
                    {
                        Id = Guid.NewGuid(),
                        Key = customKey,
                        IsSystemSeed = false,
                        IsUserModified = true,
                        IsEnabled = row.IsEnabled,
                        SeedVersion = CurrentSeedVersion,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    ApplyDefinition(customRow, preservedCopy);
                    db.CouncilTeamConfigurations.Add(customRow);

                    ApplyDefinition(row, definition);
                    row.IsEnabled = true;
                    row.IsSystemSeed = true;
                    row.IsUserModified = false;
                    row.SeedVersion = CurrentSeedVersion;
                    row.UpdatedAtUtc = DateTime.UtcNow;
                    changed = true;
                    logger.LogInformation(
                        "Recovered supplied Council seed {SeedKey}; the previously edited seed content was preserved as user-owned team {CustomKey}.",
                        row.Key,
                        customKey);
                    continue;
                }

                // Lossless seed evolution: normal seed updates replace only the maintained system row.
                if (row.SeedVersion < CurrentSeedVersion && !row.IsUserModified)
                {
                    var enabled = row.IsEnabled;
                    ApplyDefinition(row, definition);
                    row.IsEnabled = enabled;
                    row.IsSystemSeed = true;
                    row.IsUserModified = false;
                    row.SeedVersion = CurrentSeedVersion;
                    row.UpdatedAtUtc = DateTime.UtcNow;
                    changed = true;
                }
            }

            if (changed)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(EnsureSeededAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(EnsureSeededAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes seed defaults as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="team">Seed definition to normalize in place.</param>
    private void NormalizeSeedDefaults(OrganicCouncilTeamDefinition team)
    {
    try
    {
            team.ExpertPreparationPromptTemplate = string.IsNullOrWhiteSpace(team.ExpertPreparationPromptTemplate)
                ? """
    You lead the expert preparation round for {{TeamName}}. Do not solve the whole task yet.
    Prepare the user's work order: establish current/target state, revision/branch, compiler and file syntax, relevant RegEx probes, installer/bootstrap/port/protocol contracts, missing evidence, required roles, bounded 1-Wire evidence packages and human approval gates.
    User request:
    {{UserPrompt}}
    """
                : team.ExpertPreparationPromptTemplate;
            team.LeaderSynthesisPromptTemplate = string.IsNullOrWhiteSpace(team.LeaderSynthesisPromptTemplate)
                ? """
    You are the council leader for {{TeamName}}. Convert the expert preparation into a precise UML-compatible current-to-target work order. Preserve installer/bootstrap/port and existing integration contracts. Separate facts, assumptions, changes, verification, organ calls and human approval gates. Assign responsibilities to roles and never create an unattended agent loop.
    Expert preparation:
    {{Preparation}}
    Original user request:
    {{UserPrompt}}
    """
                : team.LeaderSynthesisPromptTemplate;
            team.MainRoundInstructionTemplate = string.IsNullOrWhiteSpace(team.MainRoundInstructionTemplate)
                ? "Every member contributes democratically according to role, evidence and demonstrated skill. Integrate new human corrections at the next heartbeat without cancelling the active run."
                : team.MainRoundInstructionTemplate;
            team.AllowedAutomaticFunctions ??= [];
            team.AllowedAutomaticFunctions = NormalizeFunctionNames(team.AllowedAutomaticFunctions);
            var useSuppliedDefaultWorkflow = team.WorkflowSteps.Count == 0;
            if (useSuppliedDefaultWorkflow)
            {
                team.WorkflowSteps =
                [
                    new() { Key = "expert-preparation", DisplayName = "Expert preparation", SortOrder = 10, Phase = "Preparation", Role = "RegEx/language/domain expert", ExecutionMode = "LeaderSingle", PromptTemplate = team.ExpertPreparationPromptTemplate, UseBuiltInBehavior = true },
                    new() { Key = "leader-synthesis", DisplayName = "Leader synthesis", SortOrder = 20, Phase = "Planning", Role = "Council leader", ExecutionMode = "LeaderSingle", PromptTemplate = team.LeaderSynthesisPromptTemplate, UseBuiltInBehavior = true },
                    new() { Key = "member-proposals", DisplayName = "Member proposals", SortOrder = 30, Phase = "Proposal", Role = "Role-directed council member", ExecutionMode = "AllMembersParallel", PromptTemplate = team.MainRoundInstructionTemplate, UseBuiltInBehavior = true },
                    new() { Key = "peer-review", DisplayName = "Peer review", SortOrder = 40, Phase = "Critique", Role = "Peer reviewer", ExecutionMode = "AllMembersParallel", PromptTemplate = "Review the current transcript, integrate useful contributions, correct problems and identify anything needing user decision.", UseBuiltInBehavior = true },
                    new() { Key = "consensus", DisplayName = "Consensus", SortOrder = 50, Phase = "Consensus", Role = "Consensus writer", ExecutionMode = "LeaderSingle", PromptTemplate = "Consolidate the evidence into one buildable, testable answer while preserving unresolved risks and approval gates.", ProducesFinalAnswer = true, UseBuiltInBehavior = true }
                ];
            }

            if (useSuppliedDefaultWorkflow && !team.WorkflowSteps.Any(step => string.Equals(step.Key, "member-readiness-introduction", StringComparison.OrdinalIgnoreCase)))
            {
                team.WorkflowSteps.Add(new CouncilWorkflowStepDefinition
                {
                    Key = "member-readiness-introduction",
                    DisplayName = "Member readiness and introduction",
                    SortOrder = 5,
                    Phase = "Readiness",
                    Role = "Every council member",
                    ExecutionMode = "AllMembersParallel",
                    PromptTemplate = "Introduce yourself to the other Council members. Confirm your model-specific CPU/GPU/accelerator road, token range, directly available DXFunctions, approved skills and connected 1-Wire organs. State evidence-backed strengths, what you want to improve in LocalGPT, and exact missing requirements or user questions. Self-reported capabilities remain untrusted until user approval.",
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false,
                    UseBuiltInBehavior = true
                });
            }

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
                role.MinimumAiParticipants = Math.Max(1, role.MinimumAiParticipants);
                role.MaximumAiParticipants = Math.Max(role.MinimumAiParticipants, role.MaximumAiParticipants);
            }

            foreach (var step in team.WorkflowSteps)
            {
                step.LogicalRoundNumber = Math.Clamp(step.LogicalRoundNumber, 0, MaxExpandedWorkflowSteps);
                if (!Enum.IsDefined(typeof(CouncilTranscriptVisibilityMode), step.TranscriptVisibility))
                    step.TranscriptVisibility = CouncilTranscriptVisibilityMode.FullCouncil;
                if (!Enum.IsDefined(typeof(CouncilRoleResultSynthesisMemberMode), step.RoleResultSynthesisMemberMode))
                    step.RoleResultSynthesisMemberMode = CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember;
                step.RoleResultSynthesisModelName = step.RoleResultSynthesisModelName?.Trim() ?? string.Empty;
                step.AllowedAutomaticFunctions ??= [];
                step.AllowedAutomaticFunctions = NormalizeFunctionNames(step.AllowedAutomaticFunctions);
                step.AutomaticFunctionPolicyMode = NormalizeAutomaticFunctionPolicy(step);
                step.CanUseOrganicFunctions = step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.Disabled;
                step.RoleComplianceRetryCount = Math.Clamp(step.RoleComplianceRetryCount, 0, 3);
                step.FinalAnswerRecoveryMaxOutputTokens = Math.Clamp(step.FinalAnswerRecoveryMaxOutputTokens, 128, 32768);
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
                    step.ExecutionMode = "LeaderSingle";
            }
            NormalizeLoopGroups(team.WorkflowSteps);
            team.WorkflowSteps = team.WorkflowSteps.OrderBy(step => step.SortOrder).ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeSeedDefaults)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeSeedDefaults)} failed.");
        throw;
    }
}

    }
}
