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
[DocumentationUpdated("2.1.21")]
public sealed partial class CouncilTeamConfigurationService : ICouncilTeamConfigurationService
{
    /// <summary>
    /// Stores the database context factory dependency used by <see cref="CouncilTeamConfigurationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
    /// <summary>
    /// Stores the database initialization service dependency used by <see cref="CouncilTeamConfigurationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseInitializationService databaseInitializer;
    /// <summary>
    /// Stores the organic council blueprint seed data service dependency used by <see cref="CouncilTeamConfigurationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IOrganicCouncilBlueprintSeedDataService seedData;
    /// <summary>
    /// Stores the logger used by <see cref="CouncilTeamConfigurationService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<CouncilTeamConfigurationService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="dbContextFactory">Injected dependency used by the CouncilTeamConfigurationService.</param>
    /// <param name="databaseInitializer">Injected dependency used by the CouncilTeamConfigurationService.</param>
    /// <param name="seedData">Injected dependency used by the CouncilTeamConfigurationService.</param>
    /// <param name="logger">Injected dependency used by the CouncilTeamConfigurationService.</param>
    public CouncilTeamConfigurationService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseInitializationService databaseInitializer,
        IOrganicCouncilBlueprintSeedDataService seedData,
        ILogger<CouncilTeamConfigurationService> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.databaseInitializer = databaseInitializer;
        this.seedData = seedData;
        this.logger = logger;
    }

    /// <summary>
    /// Defines the current seed version constant used by <see cref="CouncilTeamConfigurationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int CurrentSeedVersion = 26;
    /// <summary>
    /// Defines the max roles constant used by <see cref="CouncilTeamConfigurationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxRoles = 100;
    /// <summary>
    /// Defines the max workflow steps constant used by <see cref="CouncilTeamConfigurationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxWorkflowSteps = 100;
    /// <summary>
    /// Defines the max expanded workflow steps constant used by <see cref="CouncilTeamConfigurationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxExpandedWorkflowSteps = 100;
    /// <summary>
    /// Stores the in-memory supported execution modes collection maintained internally by <see cref="CouncilTeamConfigurationService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<string> SupportedExecutionModes =
    [
        "AllMembersParallel",
        "AllMembersSequentialOnEachAIHostParallel",
        "AllMembersSequential",
        "LeaderSingle",
        "RoundRobinSingle",
        "AssignedModelSingle",
        "SystemBenchmarkCalibration"
    ];
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="CouncilTeamConfigurationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Retrieves teams as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
    try
    {
            await EnsureSeededAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.CouncilTeamConfigurations.AsNoTracking();
            if (!includeDisabled)
                query = query.Where(item => item.IsEnabled && !item.IsDeleted);
            var rows = await query.OrderBy(item => item.DisplayName).ToListAsync(cancellationToken).ConfigureAwait(false);
            return rows.Select(ToDefinition).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(GetTeamsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(GetTeamsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Finds team as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default)
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(key) ? "general" : key.Trim().ToLowerInvariant();
            await EnsureSeededAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.CouncilTeamConfigurations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Key == normalized && item.IsEnabled && !item.IsDeleted, cancellationToken)
                .ConfigureAwait(false);
            return row is null ? null : ToDefinition(row);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(FindTeamAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(FindTeamAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs save as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<OrganicCouncilTeamDefinition> SaveAsync(SaveCouncilTeamConfigurationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Team);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before changing a council team or workflow.");

            NormalizeAndValidateUserDefinition(request.Team);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var requestedKey = request.Team.Key;
            var row = await db.CouncilTeamConfigurations.SingleOrDefaultAsync(item => item.Key == requestedKey, cancellationToken).ConfigureAwait(false);
            var definitionToSave = request.Team;
            if (row is { IsSystemSeed: true })
            {
                var existingKeys = await db.CouncilTeamConfigurations
                    .Select(item => item.Key)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                var customKey = CreateUniqueUserCopyKey(requestedKey, existingKeys);
                definitionToSave = CloneAsUserOwnedDefinition(request.Team, customKey);
                row = new CouncilTeamConfiguration
                {
                    Id = Guid.NewGuid(),
                    Key = customKey,
                    IsSystemSeed = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.CouncilTeamConfigurations.Add(row);
                logger.LogInformation(
                    "Preserved supplied Council seed {SeedKey} and redirected the confirmed edit to user-owned team {CustomKey}.",
                    requestedKey,
                    customKey);
            }
            else if (row is null)
            {
                row = new CouncilTeamConfiguration
                {
                    Id = Guid.NewGuid(),
                    Key = requestedKey,
                    IsSystemSeed = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.CouncilTeamConfigurations.Add(row);
            }

            ApplyDefinition(row, definitionToSave);
            row.IsDeleted = false;
            row.IsEnabled = request.IsEnabled;
            row.IsSystemSeed = false;
            row.IsUserModified = true;
            row.SeedVersion = CurrentSeedVersion;
            row.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Saved editable council team {TeamKey} with {RoleCount} role(s), {WorkflowStepCount} workflow step(s) and {ExpandedStepCount} expanded round(s).",
                row.Key,
                definitionToSave.Roles.Count,
                definitionToSave.WorkflowSteps.Count,
                CalculateMaximumExpandedRounds(definitionToSave.WorkflowSteps));
            return ToDefinition(row);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(SaveAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(SaveAsync)} failed.");
        throw;
    }
}

    /// <summary>Returns the maintained supplied Council templates independently from user-owned or deleted persisted team rows.</summary>
    /// <param name="cancellationToken">Cancels template-catalog retrieval.</param>
    /// <returns>The resettable supplied template catalog.</returns>
    public Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetDefaultTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var templates = seedData.CreateDefaultTeams()
                .Select(template =>
                {
                    NormalizeSeedDefaults(template);
                    return template;
                })
                .OrderBy(template => template.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult<IReadOnlyList<OrganicCouncilTeamDefinition>>(templates);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading supplied Council team templates failed.");
            throw;
        }
    }

    /// <summary>Tombstones one configured Council team after explicit user confirmation while leaving supplied templates available for reset.</summary>
    /// <param name="key">Configured team key to delete.</param>
    /// <param name="userConfirmed">Whether the user explicitly confirmed the destructive action.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that completes after the deletion tombstone is persisted.</returns>
    public async Task DeleteAsync(string key, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before deleting a Council team configuration.");
            var normalized = (key ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Length == 0)
                throw new ArgumentException("Choose a configured Council team before deleting it.", nameof(key));

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.CouncilTeamConfigurations.SingleOrDefaultAsync(item => item.Key == normalized, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Council team '{normalized}' was not found.");
            row.IsDeleted = true;
            row.IsEnabled = false;
            row.IsUserModified = true;
            row.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted configured Council team {TeamKey}; supplied templates remain available for explicit reset.", normalized);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Deleting Council team configuration was canceled.");
            else
                logger.LogError(exception, "Deleting Council team configuration {TeamKey} failed.", key);
            throw;
        }
    }

    /// <summary>Replaces one configured team's behavior with a selected supplied template while preserving the configured target key.</summary>
    /// <param name="targetKey">Configured team key to replace or restore.</param>
    /// <param name="templateKey">Supplied template key whose resettable behavior should be copied.</param>
    /// <param name="userConfirmed">Whether the user explicitly confirmed the reset.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>The persisted normalized team definition after the reset.</returns>
    public async Task<OrganicCouncilTeamDefinition> ResetToTemplateAsync(
        string targetKey,
        string templateKey,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before resetting a Council team from a supplied template.");
            var normalizedTarget = (targetKey ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedTemplate = (templateKey ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedTarget.Length == 0 || normalizedTemplate.Length == 0)
                throw new ArgumentException("Choose both a configured team and a supplied template before resetting.");

            var template = seedData.CreateDefaultTeams()
                .FirstOrDefault(item => string.Equals(item.Key, normalizedTemplate, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Supplied Council template '{normalizedTemplate}' was not found.");
            NormalizeSeedDefaults(template);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.CouncilTeamConfigurations.SingleOrDefaultAsync(item => item.Key == normalizedTarget, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new CouncilTeamConfiguration
                {
                    Id = Guid.NewGuid(),
                    Key = normalizedTarget,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.CouncilTeamConfigurations.Add(row);
            }

            var restoresCanonicalSeed = string.Equals(normalizedTarget, normalizedTemplate, StringComparison.OrdinalIgnoreCase);
            OrganicCouncilTeamDefinition restored;
            if (restoresCanonicalSeed)
            {
                var json = JsonSerializer.Serialize(template, JsonOptions);
                restored = JsonSerializer.Deserialize<OrganicCouncilTeamDefinition>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Council template cloning returned no definition.");
            }
            else
            {
                restored = CloneAsUserOwnedDefinition(template, normalizedTarget);
            }
            restored.Key = normalizedTarget;
            restored.DisplayName = template.DisplayName;
            restored.IsEnabled = true;
            restored.IsDeleted = false;
            ApplyDefinition(row, restored);
            row.IsDeleted = false;
            row.IsEnabled = true;
            row.SeedVersion = CurrentSeedVersion;
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.IsSystemSeed = restoresCanonicalSeed;
            row.IsUserModified = !restoresCanonicalSeed;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Reset Council team {TargetKey} from supplied template {TemplateKey}.", normalizedTarget, normalizedTemplate);
            return ToDefinition(row);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Resetting Council team configuration was canceled.");
            else
                logger.LogError(exception, "Resetting Council team {TargetKey} from template {TemplateKey} failed.", targetKey, templateKey);
            throw;
        }
    }
}
