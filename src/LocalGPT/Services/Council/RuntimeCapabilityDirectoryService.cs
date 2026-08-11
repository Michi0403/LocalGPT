using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Council;

/// <summary>
/// Owns the boot- and run-time DXFunction/organic-skill directory feed. The database copy is discovery
/// metadata only; the DI handler and its declared interaction policy remain authoritative for execution.
/// </summary>
public sealed class RuntimeCapabilityDirectoryService(
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDxAiFunctionRegistry dxFunctions,
    IDxAiFunctionCatalogService functionCatalog,
    IOrganicSkillRegistryService organicSkills,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<RuntimeCapabilityDirectoryService> logger) : IRuntimeCapabilityDirectoryService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // The service itself is scoped because its function/catalog dependencies are scoped. Boot synchronization
    // and Council preflight may nevertheless overlap, so their derived database writes need one process-wide gate.
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly SemaphoreSlim SynchronizationGate = new(1, 1);

    /// <summary>
    /// Runs the synchronize async operation.
    /// </summary>
    public async Task<RuntimeCapabilityDirectorySnapshot> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await SynchronizationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Boot synchronization and Council preflight can otherwise synchronize the same database-backed
                // catalog at the same time. Serialize the full derived-directory refresh, not only its final SaveChanges.
                var functions = dxFunctions.GetFunctions()
                    .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var catalogEntries = (await functionCatalog.SynchronizeAsync(cancellationToken).ConfigureAwait(false))
                    .OrderBy(item => item.Kind).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var skills = (await organicSkills.GetWireSkillsAsync(cancellationToken).ConfigureAwait(false))
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var snapshot = new RuntimeCapabilityDirectorySnapshot
                {
                    Functions = functions,
                    CatalogEntries = catalogEntries,
                    Skills = skills
                };

                await PersistSnapshotWithRetryAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return snapshot;
            }
            finally
            {
                SynchronizationGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(SynchronizeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(SynchronizeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the persist snapshot with retry async operation.
    /// </summary>
    private async Task PersistSnapshotWithRetryAsync(
        RuntimeCapabilityDirectorySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                if (!await PersistSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false))
                    return;

                logger.LogInformation(
                    "Synchronized {FunctionCount} DXFunctions, {CatalogCount} catalog policies and {SkillCount} organic skills into the LocalGPT Core project.",
                    snapshot.Functions.Count,
                    snapshot.CatalogEntries.Count,
                    snapshot.Skills.Count);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt == 1)
            {
                // A previous scoped synchronization may have loaded the same derived artifact just before this
                // process-wide gate was introduced. Retry once with a fresh DbContext and current rows.
                logger.LogWarning(ex, "The runtime capability directory changed while it was being persisted; retrying once with current database rows.");
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (attempt == 1)
            {
                logger.LogWarning(ex, "The runtime capability directory could not be persisted on the first attempt; retrying once with a fresh DbContext.");
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Persistence is a searchable cache of the live in-memory registry. It must never prevent the
                // Council from using the authoritative function and skill collections already built above.
                const string warning = "The live capability directory is available, but its derived LocalGPT Core project artifacts could not be refreshed. Council execution continues.";
                snapshot.Warnings.Add(warning);
                logger.LogWarning(ex, "{Warning}", warning);
                return;
            }
        }
    }

    /// <summary>
    /// Runs the persist snapshot async operation.
    /// </summary>
    private async Task<bool> PersistSnapshotAsync(
        RuntimeCapabilityDirectorySnapshot snapshot,
        CancellationToken cancellationToken)
    {
    try
    {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (!await db.LocalGptProjects.AsNoTracking()
                    .AnyAsync(item => item.Id == runtimePolicy.LocalGptCoreProjectId, cancellationToken)
                    .ConfigureAwait(false))
            {
                snapshot.Warnings.Add("The LocalGPT Core project is missing; the runtime function directory could not be persisted.");
                return false;
            }

            await UpsertArtifactAsync(
                db,
                "Runtime DXFunction directory",
                "FunctionDirectory",
                JsonSerializer.Serialize(snapshot.Functions, JsonOptions),
                "Dependency-injected function descriptors synchronized at boot and before every Council run. Discovery metadata is not permission.",
                cancellationToken).ConfigureAwait(false);
            await UpsertArtifactAsync(
                db,
                "Runtime user-controlled DXFunction exposure catalog",
                "DxFunctionPolicyDirectory",
                JsonSerializer.Serialize(snapshot.CatalogEntries, JsonOptions),
                "Current database-backed visibility, invocation, peer and receiving-frontend confirmation policies for DX functions and configured public service methods.",
                cancellationToken).ConfigureAwait(false);
            await UpsertArtifactAsync(
                db,
                "Runtime organic skill directory",
                "OrganicSkillDirectory",
                JsonSerializer.Serialize(snapshot.Skills, JsonOptions),
                "Current 1-Wire organic skills and UI activation metadata synchronized at boot and before every Council run.",
                cancellationToken).ConfigureAwait(false);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await db.LocalGptProjects
                .Where(item => item.Id == runtimePolicy.LocalGptCoreProjectId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(PersistSnapshotAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(PersistSnapshotAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the upsert artifact async operation.
    /// </summary>
    private async Task UpsertArtifactAsync(
        LocalGptMemoryDbContext db,
        string name,
        string kind,
        string value,
        string description,
        CancellationToken cancellationToken)
    {
    try
    {
            var artifact = await db.LocalGptProjectArtifacts.SingleOrDefaultAsync(
                    item => item.ProjectId == runtimePolicy.LocalGptCoreProjectId && item.ArtifactKind == kind && item.Name == name,
                    cancellationToken)
                .ConfigureAwait(false);
            if (artifact is null)
            {
                artifact = new LocalGptProjectArtifact
                {
                    ProjectId = runtimePolicy.LocalGptCoreProjectId,
                    Name = name,
                    ArtifactKind = kind,
                    DataType = "application/json",
                    IsUserApproved = true,
                    CouncilReviewStatus = "Current",
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.LocalGptProjectArtifacts.Add(artifact);
            }

            artifact.Value = value;
            artifact.Description = description;
            artifact.UpdatedAtUtc = DateTime.UtcNow;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(UpsertArtifactAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryService)}.{nameof(UpsertArtifactAsync)} failed.");
        throw;
    }
}
}

/// <summary>Feeds the runtime directory once per application boot after lossless database initialization.</summary>
public sealed class RuntimeCapabilityDirectoryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RuntimeCapabilityDirectoryHostedService> logger) : IHostedService
{
    /// <summary>
    /// Starts async.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IRuntimeCapabilityDirectoryService>();
            await service.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "The boot-time runtime capability directory feed failed. Startup continues; database content was not deleted or reset, and the next Council preflight will retry.");
        }
    }

    /// <summary>
    /// Stops async.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) {
    try
    {
        return Task.CompletedTask;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryHostedService)}.{nameof(StopAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RuntimeCapabilityDirectoryHostedService)}.{nameof(StopAsync)} failed.");
        throw;
    }
}
}
