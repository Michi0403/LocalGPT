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
    private const int CurrentSeedVersion = 6;
    private const int MaxRoles = 100;
    private const int MaxWorkflowSteps = 100;
    private const int MaxExpandedWorkflowSteps = 100;
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
            request.Team.WorkflowSteps.Where(step => step.IsEnabled).Sum(step => step.RepeatCount));
        return ToDefinition(row);
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existingRows = await db.CouncilTeamConfigurations.ToListAsync(cancellationToken).ConfigureAwait(false);
        var existing = existingRows.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        var changed = false;
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
        if (team.WorkflowSteps.Count == 0)
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

        if (!team.WorkflowSteps.Any(step => string.Equals(step.Key, "member-readiness-introduction", StringComparison.OrdinalIgnoreCase)))
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

        foreach (var step in team.WorkflowSteps)
        {
            step.RepeatCount = Math.Clamp(step.RepeatCount, 1, MaxExpandedWorkflowSteps);
            step.ExecutionMode = NormalizeExecutionMode(step.ExecutionMode);
        }
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
        }

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
        }

        var enabledSteps = team.WorkflowSteps.Where(step => step.IsEnabled).ToList();
        if (enabledSteps.Count == 0)
            throw new InvalidOperationException("Enable at least one workflow step before saving the council team.");
        var expandedCount = enabledSteps.Sum(step => step.RepeatCount);
        if (expandedCount > MaxExpandedWorkflowSteps)
            throw new InvalidOperationException($"The enabled workflow expands to {expandedCount} rounds. The technical limit is {MaxExpandedWorkflowSteps} per run.");

        team.WorkflowSteps = team.WorkflowSteps
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        team.PreferredCapabilities = team.PreferredCapabilities.Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        team.ArchitectureContracts = team.ArchitectureContracts.Select(value => value?.Trim() ?? string.Empty).Where(value => value.Length > 0).ToList();
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
