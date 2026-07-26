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
    ILogger<RuntimeCapabilityDirectoryService> logger) : IRuntimeCapabilityDirectoryService
{
    private static readonly Guid LocalGptCoreProjectId = Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6101");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<RuntimeCapabilityDirectorySnapshot> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
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

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var project = await db.LocalGptProjects
            .Include(item => item.Artifacts)
            .SingleOrDefaultAsync(item => item.Id == LocalGptCoreProjectId, cancellationToken)
            .ConfigureAwait(false);
        if (project is null)
        {
            snapshot.Warnings.Add("The LocalGPT Core project is missing; the runtime function directory could not be persisted.");
            return snapshot;
        }

        UpsertArtifact(project, "Runtime DXFunction directory", "FunctionDirectory", JsonSerializer.Serialize(functions, JsonOptions),
            "Dependency-injected function descriptors synchronized at boot and before every Council run. Discovery metadata is not permission.");
        UpsertArtifact(project, "Runtime user-controlled DXFunction exposure catalog", "DxFunctionPolicyDirectory", JsonSerializer.Serialize(catalogEntries, JsonOptions),
            "Current database-backed visibility, invocation, peer and receiving-frontend confirmation policies for DX functions and configured public service methods.");
        UpsertArtifact(project, "Runtime organic skill directory", "OrganicSkillDirectory", JsonSerializer.Serialize(skills, JsonOptions),
            "Current 1-Wire organic skills and UI activation metadata synchronized at boot and before every Council run.");
        project.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Synchronized {FunctionCount} DXFunctions, {CatalogCount} catalog policies and {SkillCount} organic skills into the LocalGPT Core project.", functions.Count, catalogEntries.Count, skills.Count);
        return snapshot;
    }

    private static void UpsertArtifact(LocalGptProject project, string name, string kind, string value, string description)
    {
        var artifact = project.Artifacts.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (artifact is null)
        {
            artifact = new LocalGptProjectArtifact
            {
                ProjectId = project.Id,
                Name = name,
                ArtifactKind = kind,
                DataType = "application/json",
                IsUserApproved = true,
                CouncilReviewStatus = "Current",
                CreatedAtUtc = DateTime.UtcNow
            };
            project.Artifacts.Add(artifact);
        }
        artifact.Value = value;
        artifact.Description = description;
        artifact.UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>Feeds the runtime directory once per application boot after lossless database initialization.</summary>
public sealed class RuntimeCapabilityDirectoryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RuntimeCapabilityDirectoryHostedService> logger) : IHostedService
{
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
