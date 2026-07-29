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
    private const int CurrentSeedVersion = 5;
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
            throw new InvalidOperationException("Fresh human confirmation is required before changing a council team or heartbeat script.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Team.Key);

        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var key = request.Team.Key.Trim().ToLowerInvariant();
        var row = await db.CouncilTeamConfigurations.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = new CouncilTeamConfiguration { Id = Guid.NewGuid(), Key = key, CreatedAtUtc = DateTime.UtcNow };
            db.CouncilTeamConfigurations.Add(row);
        }

        ApplyDefinition(row, request.Team);
        row.IsEnabled = request.IsEnabled;
        row.IsSystemSeed = row.IsSystemSeed && seedData.CreateDefaultTeams().Any(team => team.Key == key);
        row.IsUserModified = true;
        row.SeedVersion = CurrentSeedVersion;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved editable council team {TeamKey} with {WorkflowStepCount} workflow step(s).", key, request.Team.WorkflowSteps.Count);
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
            NormalizeDefaults(definition);
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

            // Lossless seed evolution: a row explicitly edited by the user is never overwritten.
            // An untouched system row, however, must receive the complete newer workflow/prompt set;
            // otherwise new seeded features remain invisible forever on upgraded databases.
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
            else if (row.IsUserModified && MergeRequiredIntroductionStep(row, definition))
            {
                row.SeedVersion = Math.Max(row.SeedVersion, CurrentSeedVersion);
                row.UpdatedAtUtc = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void NormalizeDefaults(OrganicCouncilTeamDefinition team)
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
                new() { Key = "expert-preparation", DisplayName = "Expert preparation", SortOrder = 10, Phase = "Preparation", Role = "RegEx/language/domain expert", ExecutionMode = "LeaderSingle", PromptTemplate = team.ExpertPreparationPromptTemplate },
                new() { Key = "leader-synthesis", DisplayName = "Leader synthesis", SortOrder = 20, Phase = "Planning", Role = "Council leader", ExecutionMode = "LeaderSingle", PromptTemplate = team.LeaderSynthesisPromptTemplate },
                new() { Key = "member-proposals", DisplayName = "Member proposals", SortOrder = 30, Phase = "Proposal", Role = "Role-directed council member", ExecutionMode = "AllMembersParallel", PromptTemplate = team.MainRoundInstructionTemplate },
                new() { Key = "peer-review", DisplayName = "Peer review", SortOrder = 40, Phase = "Critique", Role = "Peer reviewer", ExecutionMode = "AllMembersParallel", PromptTemplate = "Review the current transcript, integrate useful contributions, correct problems and identify anything needing user decision." },
                new() { Key = "consensus", DisplayName = "Consensus", SortOrder = 50, Phase = "Consensus", Role = "Consensus writer", ExecutionMode = "LeaderSingle", PromptTemplate = "Consolidate the evidence into one buildable, testable answer while preserving unresolved risks and approval gates." }
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
                CanUseOrganicFunctions = false
            });
        }
        team.WorkflowSteps = team.WorkflowSteps.OrderBy(step => step.SortOrder).ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private bool MergeRequiredIntroductionStep(CouncilTeamConfiguration row, OrganicCouncilTeamDefinition seededDefinition)
    {
        var existing = Deserialize<List<CouncilWorkflowStepDefinition>>(row.WorkflowStepsJson) ?? [];
        if (existing.Any(step => string.Equals(step.Key, "member-readiness-introduction", StringComparison.OrdinalIgnoreCase)))
            return false;
        var seeded = seededDefinition.WorkflowSteps.First(step => string.Equals(step.Key, "member-readiness-introduction", StringComparison.OrdinalIgnoreCase));
        existing.Add(seeded);
        row.WorkflowStepsJson = Serialize(existing.OrderBy(step => step.SortOrder).ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase).ToList());
        return true;
    }

    private void ApplyDefinition(CouncilTeamConfiguration row, OrganicCouncilTeamDefinition definition)
    {
        NormalizeDefaults(definition);
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
        IsEnabled = row.IsEnabled
    };

    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    private T? Deserialize<T>(string json) { try { return JsonSerializer.Deserialize<T>(json, JsonOptions); } catch (JsonException) { return default; } }
    private bool IsEmptyArray(string value) => string.IsNullOrWhiteSpace(value) || value.Trim() is "[]" or "null";
}
