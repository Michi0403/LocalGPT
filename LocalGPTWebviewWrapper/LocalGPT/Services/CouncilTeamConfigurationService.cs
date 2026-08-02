using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Database-owned council team/workflow configuration. System defaults are seeded and merged without
/// overwriting user-edited prompts, roles, capabilities or workflow scripts.
/// </summary>
public sealed class CouncilTeamConfigurationService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IOrganicCouncilBlueprintSeedDataService seedData,
    ILogger<CouncilTeamConfigurationService> logger) : ICouncilTeamConfigurationService
{
    private const int CurrentSeedVersion = 13;
    private const int MaxRoles = 100;
    private const int MaxWorkflowSteps = 100;
    private const int MaxExpandedWorkflowSteps = 100;
    private const int MaxAiParticipantsPerRole = 100;
    private readonly IReadOnlyList<string> SupportedExecutionModes =
    [
        "AllMembersParallel",
        "AllMembersSequential",
        "LeaderSingle",
        "RoundRobinSingle",
        "AssignedModelSingle"
    ];
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.CouncilTeamConfigurations.AsNoTracking();
        if (!includeDisabled)
            query = query.Where(item => item.IsEnabled);
        var rows = await query.OrderBy(item => item.DisplayName).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToDefinition).ToList();
    }

    public async Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default)
    {
        var normalized = string.IsNullOrWhiteSpace(key) ? "general" : key.Trim().ToLowerInvariant();
        await EnsureSeededAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var row = await db.CouncilTeamConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == normalized && item.IsEnabled, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : ToDefinition(row);
    }

    public async Task<OrganicCouncilTeamDefinition> SaveAsync(SaveCouncilTeamConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Team);
        if (!request.UserConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required before changing a council team or workflow.");

        NormalizeAndValidateUserDefinition(request.Team);

        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var key = request.Team.Key;
        var row = await db.CouncilTeamConfigurations.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = new CouncilTeamConfiguration
            {
                Id = Guid.NewGuid(),
                Key = key,
                IsSystemSeed = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.CouncilTeamConfigurations.Add(row);
        }

        ApplyDefinition(row, request.Team);
        row.IsEnabled = request.IsEnabled;
        row.IsSystemSeed = row.IsSystemSeed && seedData.CreateDefaultTeams().Any(team => string.Equals(team.Key, key, StringComparison.OrdinalIgnoreCase));
        row.IsUserModified = true;
        row.SeedVersion = CurrentSeedVersion;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Saved editable council team {TeamKey} with {RoleCount} role(s), {WorkflowStepCount} workflow step(s) and {ExpandedStepCount} expanded round(s).",
            key,
            request.Team.Roles.Count,
            request.Team.WorkflowSteps.Count,
            CalculateMaximumExpandedRounds(request.Team.WorkflowSteps));
        return ToDefinition(row);
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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

            // Lossless seed evolution: an explicitly edited row is never rewritten by later defaults.
            if (row.SeedVersion < CurrentSeedVersion && !row.IsUserModified)
            {
                var enabled = row.IsEnabled;
                ApplyDefinition(row, definition);
                row.IsEnabled = enabled;
                row.IsSystemSeed = true;
                row.SeedVersion = CurrentSeedVersion;
                row.UpdatedAtUtc = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void NormalizeSeedDefaults(OrganicCouncilTeamDefinition team)
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
            role.MinimumAiParticipants = Math.Clamp(role.MinimumAiParticipants, 1, MaxAiParticipantsPerRole);
            role.MaximumAiParticipants = Math.Clamp(role.MaximumAiParticipants, role.MinimumAiParticipants, MaxAiParticipantsPerRole);
        }

        foreach (var step in team.WorkflowSteps)
        {
            step.RepeatCount = Math.Clamp(step.RepeatCount, 1, MaxExpandedWorkflowSteps);
            step.ExecutionMode = NormalizeExecutionMode(step.ExecutionMode);
            step.LoopGroup = step.LoopGroup?.Trim() ?? string.Empty;
            step.MaximumLoopIterations = string.IsNullOrWhiteSpace(step.LoopGroup)
                ? 1
                : Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps);
            step.LoopCompletionMarker = step.LoopCompletionMarker?.Trim() ?? string.Empty;
            step.AsciiFrameWidth = Math.Clamp(step.AsciiFrameWidth, 20, 240);
            step.AsciiFrameHeight = Math.Clamp(step.AsciiFrameHeight, 8, 120);
            step.WorldStepScale = Math.Clamp(step.WorldStepScale, 1, 1000);
            if (step.ProducesAsciiFrame && step.ExecutionMode is "AllMembersParallel" or "AllMembersSequential")
                step.ExecutionMode = "LeaderSingle";
        }
        NormalizeLoopGroups(team.WorkflowSteps);
        team.WorkflowSteps = team.WorkflowSteps.OrderBy(step => step.SortOrder).ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void NormalizeAndValidateUserDefinition(OrganicCouncilTeamDefinition team)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(team.Key);
        team.Key = team.Key.Trim().ToLowerInvariant();
        team.DisplayName = string.IsNullOrWhiteSpace(team.DisplayName) ? team.Key : team.DisplayName.Trim();
        team.Purpose = team.Purpose?.Trim() ?? string.Empty;
        team.ExpertPreparationPromptTemplate = team.ExpertPreparationPromptTemplate?.Trim() ?? string.Empty;
        team.LeaderSynthesisPromptTemplate = team.LeaderSynthesisPromptTemplate?.Trim() ?? string.Empty;
        team.MainRoundInstructionTemplate = team.MainRoundInstructionTemplate?.Trim() ?? string.Empty;
        team.Roles ??= [];
        team.WorkflowSteps ??= [];
        team.PreferredCapabilities ??= [];
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

            role.MinimumAiParticipants = Math.Clamp(role.MinimumAiParticipants, 1, MaxAiParticipantsPerRole);
            role.MaximumAiParticipants = Math.Clamp(role.MaximumAiParticipants, 1, MaxAiParticipantsPerRole);
            if (role.AiSelectionMode == CouncilRoleAiSelectionMode.RandomRange &&
                role.MinimumAiParticipants > role.MaximumAiParticipants)
            {
                throw new InvalidOperationException(
                    $"Role '{role.Role}' has a minimum AI participant count greater than its maximum.");
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
            step.RepeatCount = Math.Clamp(step.RepeatCount, 1, MaxExpandedWorkflowSteps);
            step.ExecutionMode = NormalizeExecutionMode(step.ExecutionMode);
            step.LoopGroup = step.LoopGroup?.Trim() ?? string.Empty;
            step.MaximumLoopIterations = string.IsNullOrWhiteSpace(step.LoopGroup)
                ? 1
                : Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps);
            step.LoopCompletionMarker = step.LoopCompletionMarker?.Trim() ?? string.Empty;
            step.AsciiFrameWidth = Math.Clamp(step.AsciiFrameWidth, 20, 240);
            step.AsciiFrameHeight = Math.Clamp(step.AsciiFrameHeight, 8, 120);
            step.WorldStepScale = Math.Clamp(step.WorldStepScale, 1, 1000);
            if (step.ProducesAsciiFrame && step.ExecutionMode is "AllMembersParallel" or "AllMembersSequential")
                throw new InvalidOperationException($"ASCII frame step '{step.DisplayName}' must use a single-member execution mode so one AI owns the complete frame.");
            if (string.IsNullOrWhiteSpace(step.LoopGroup) && !string.IsNullOrWhiteSpace(step.LoopCompletionMarker))
                throw new InvalidOperationException($"Workflow step '{step.DisplayName}' defines a loop completion marker without a loop group.");
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
        team.ArchitectureContracts = team.ArchitectureContracts.Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0).ToList();
    }

    private void ValidateRoleCountReferenceCycles(IReadOnlyList<OrganicCouncilRoleDefinition> roles)
    {
        var byName = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.Role))
            .ToDictionary(role => role.Role, StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = role;
            while (!string.IsNullOrWhiteSpace(current.MatchAiParticipantCountToRole) &&
                   byName.TryGetValue(current.MatchAiParticipantCountToRole, out var next))
            {
                if (!visited.Add(current.Role))
                    throw new InvalidOperationException($"Role AI participant count references contain a cycle involving '{role.Role}'.");
                current = next;
            }
        }
    }

    private void ValidateDistinctAssignmentGroups(IReadOnlyList<OrganicCouncilRoleDefinition> roles)
    {
        foreach (var group in roles
                     .Where(role => !string.IsNullOrWhiteSpace(role.DistinctAiAssignmentGroup) &&
                                    role.HumanParticipationMode != HumanParticipationMode.HumanOnly)
                     .GroupBy(role => role.DistinctAiAssignmentGroup, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1 && group.Any(role => role.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected))
            {
                throw new InvalidOperationException(
                    $"Distinct AI assignment group '{group.Key}' contains more than one AI role, so every role in that group must use a bounded random range instead of all selected AIs.");
            }
        }
    }

    private void NormalizeLoopGroups(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
        foreach (var group in steps
                     .Where(step => !string.IsNullOrWhiteSpace(step.LoopGroup))
                     .GroupBy(step => step.LoopGroup, StringComparer.OrdinalIgnoreCase))
        {
            var maximumIterations = group.Max(step => Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps));
            foreach (var step in group)
                step.MaximumLoopIterations = maximumIterations;
        }
    }

    private void ValidateLoopGroups(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
        var ordered = steps
            .Where(step => step.IsEnabled)
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var completedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? activeGroup = null;
        foreach (var step in ordered)
        {
            var group = string.IsNullOrWhiteSpace(step.LoopGroup) ? null : step.LoopGroup;
            if (string.Equals(group, activeGroup, StringComparison.OrdinalIgnoreCase))
                continue;
            if (activeGroup is not null)
                completedGroups.Add(activeGroup);
            if (group is not null && completedGroups.Contains(group))
                throw new InvalidOperationException($"Loop group '{group}' must occupy one consecutive block in workflow sort order.");
            activeGroup = group;
        }

        foreach (var group in ordered
                     .Where(step => !string.IsNullOrWhiteSpace(step.LoopGroup))
                     .GroupBy(step => step.LoopGroup, StringComparer.OrdinalIgnoreCase))
        {
            var markers = group
                .Where(step => !string.IsNullOrWhiteSpace(step.LoopCompletionMarker))
                .Select(step => step.LoopCompletionMarker)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (markers.Count > 1)
                throw new InvalidOperationException($"Loop group '{group.Key}' defines multiple different completion markers. Use one marker for the whole loop.");
        }
    }

    private int CalculateMaximumExpandedRounds(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
        var ordered = steps
            .Where(step => step.IsEnabled)
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var total = 0;
        for (var index = 0; index < ordered.Count;)
        {
            var step = ordered[index];
            if (string.IsNullOrWhiteSpace(step.LoopGroup))
            {
                total += Math.Max(1, step.RepeatCount);
                index++;
                continue;
            }

            var loopGroup = step.LoopGroup;
            var blockRounds = 0;
            var maximumIterations = 1;
            while (index < ordered.Count && string.Equals(ordered[index].LoopGroup, loopGroup, StringComparison.OrdinalIgnoreCase))
            {
                blockRounds += Math.Max(1, ordered[index].RepeatCount);
                maximumIterations = Math.Max(maximumIterations, Math.Max(1, ordered[index].MaximumLoopIterations));
                index++;
            }
            total += blockRounds * maximumIterations;
        }
        return total;
    }

    private string NormalizeExecutionMode(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "AllMembersParallel" : value.Trim();
        if (candidate.Equals("AllMembers", StringComparison.OrdinalIgnoreCase) || candidate.Equals("Parallel", StringComparison.OrdinalIgnoreCase))
            candidate = "AllMembersParallel";
        else if (candidate.Equals("Sequential", StringComparison.OrdinalIgnoreCase))
            candidate = "AllMembersSequential";
        else if (candidate.Equals("Single", StringComparison.OrdinalIgnoreCase))
            candidate = "LeaderSingle";

        var normalized = SupportedExecutionModes.FirstOrDefault(mode => mode.Equals(candidate, StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw new InvalidOperationException(
            $"Execution mode '{candidate}' is not supported. Use {string.Join(", ", SupportedExecutionModes.OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase))}.");
    }

    private void ApplyDefinition(CouncilTeamConfiguration row, OrganicCouncilTeamDefinition definition)
    {
        row.Key = definition.Key.Trim().ToLowerInvariant();
        row.DisplayName = definition.DisplayName.Trim();
        row.Purpose = definition.Purpose.Trim();
        row.RolesJson = Serialize(definition.Roles);
        row.PreferredCapabilitiesJson = Serialize(definition.PreferredCapabilities);
        row.ArchitectureContractsJson = Serialize(definition.ArchitectureContracts);
        row.WorkflowStepsJson = Serialize(definition.WorkflowSteps);
        row.ExpertPreparationPromptTemplate = definition.ExpertPreparationPromptTemplate;
        row.LeaderSynthesisPromptTemplate = definition.LeaderSynthesisPromptTemplate;
        row.MainRoundInstructionTemplate = definition.MainRoundInstructionTemplate;
    }

    private OrganicCouncilTeamDefinition ToDefinition(CouncilTeamConfiguration row) => new()
    {
        Key = row.Key,
        DisplayName = row.DisplayName,
        Purpose = row.Purpose,
        Roles = Deserialize<List<OrganicCouncilRoleDefinition>>(row.RolesJson) ?? [],
        PreferredCapabilities = Deserialize<List<string>>(row.PreferredCapabilitiesJson) ?? [],
        ArchitectureContracts = Deserialize<List<string>>(row.ArchitectureContractsJson) ?? [],
        WorkflowSteps = Deserialize<List<CouncilWorkflowStepDefinition>>(row.WorkflowStepsJson) ?? [],
        ExpertPreparationPromptTemplate = row.ExpertPreparationPromptTemplate,
        LeaderSynthesisPromptTemplate = row.LeaderSynthesisPromptTemplate,
        MainRoundInstructionTemplate = row.MainRoundInstructionTemplate,
        IsEnabled = row.IsEnabled,
        IsSystemSeed = row.IsSystemSeed,
        IsUserModified = row.IsUserModified
    };

    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    private T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
