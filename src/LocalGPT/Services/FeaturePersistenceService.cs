using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>EF Core implementation for durable feature records that are not transient runtime DTOs.</summary>
/// <param name="dbContextFactory">Creates isolated EF Core contexts.</param>
/// <param name="databaseInitializer">Ensures migrations are current before CRUD operations.</param>
/// <param name="catalog">Provides maintained prompt starters that are seeded when missing.</param>
/// <param name="logger">Writes bounded feature-persistence diagnostics.</param>
[DocumentationUpdated("2.1.23")]
public sealed class FeaturePersistenceService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    LocalGptCatalogService catalog,
    ILogger<FeaturePersistenceService> logger) : IFeaturePersistenceService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CouncilPromptStarterConfiguration>> GetCouncilPromptStartersAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await EnsureBuiltInCouncilStartersAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.CouncilPromptStarterConfigurations.AsNoTracking();
            if (!includeDisabled)
                query = query.Where(item => item.IsEnabled);
            return await query.OrderBy(item => item.Title).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not list persistent Council prompt starters.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CouncilPromptStarterConfiguration?> GetCouncilPromptStarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.CouncilPromptStarterConfigurations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load Council prompt starter {StarterId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CouncilPromptStarterConfiguration> SaveCouncilPromptStarterAsync(SaveFeatureRecordRequest<CouncilPromptStarterConfiguration> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateWrite(request);
            NormalizeStarter(request.Record);
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.CouncilPromptStarterConfigurations
                .SingleOrDefaultAsync(item => item.Id == request.Record.Id || item.Key == request.Record.Key, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                request.Record.Id = request.Record.Id == Guid.Empty ? Guid.NewGuid() : request.Record.Id;
                request.Record.CreatedAtUtc = DateTime.UtcNow;
                request.Record.UpdatedAtUtc = request.Record.CreatedAtUtc;
                db.CouncilPromptStarterConfigurations.Add(request.Record);
                existing = request.Record;
            }
            else
            {
                existing.Key = request.Record.Key;
                existing.Title = request.Record.Title;
                existing.Summary = request.Record.Summary;
                existing.PromptMessage = request.Record.PromptMessage;
                existing.TeamKeysJson = request.Record.TeamKeysJson;
                existing.StartsCouncilDirectly = request.Record.StartsCouncilDirectly;
                existing.IsBuiltIn = request.Record.IsBuiltIn;
                existing.IsEnabled = request.Record.IsEnabled;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved persistent Council prompt starter {StarterKey}.", existing.Key);
            return existing;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save a persistent Council prompt starter.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteCouncilPromptStarterAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default) {
    try
    {
        return DeleteAsync(id, userConfirmed, "Council prompt starter", db => db.CouncilPromptStarterConfigurations, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteCouncilPromptStarterAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteCouncilPromptStarterAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalizationCatalogRegistration>> GetLocalizationCatalogsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.LocalizationCatalogRegistrations.AsNoTracking();
            if (!includeDisabled)
                query = query.Where(item => item.IsEnabled);
            return await query.OrderBy(item => item.DisplayName).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not list localization catalog registrations.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<LocalizationCatalogRegistration?> GetLocalizationCatalogAsync(Guid id, CancellationToken cancellationToken = default) {
    try
    {
        return GetByIdAsync(id, "localization catalog registration", db => db.LocalizationCatalogRegistrations, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetLocalizationCatalogAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetLocalizationCatalogAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<LocalizationCatalogRegistration> SaveLocalizationCatalogAsync(SaveFeatureRecordRequest<LocalizationCatalogRegistration> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateWrite(request);
            NormalizeLocalization(request.Record);
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.LocalizationCatalogRegistrations
                .SingleOrDefaultAsync(item => item.Id == request.Record.Id || item.CultureName == request.Record.CultureName, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                request.Record.Id = request.Record.Id == Guid.Empty ? Guid.NewGuid() : request.Record.Id;
                request.Record.CreatedAtUtc = DateTime.UtcNow;
                request.Record.UpdatedAtUtc = request.Record.CreatedAtUtc;
                db.LocalizationCatalogRegistrations.Add(request.Record);
                existing = request.Record;
            }
            else
            {
                existing.CultureName = request.Record.CultureName;
                existing.DisplayName = request.Record.DisplayName;
                existing.CatalogPath = request.Record.CatalogPath;
                existing.StringCount = request.Record.StringCount;
                existing.MissingBaselineKeyCount = request.Record.MissingBaselineKeyCount;
                existing.IsUserOverride = request.Record.IsUserOverride;
                existing.IsEnabled = request.Record.IsEnabled;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved localization catalog registration {CultureName}.", existing.CultureName);
            return existing;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save a localization catalog registration.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteLocalizationCatalogAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default) {
    try
    {
        return DeleteAsync(id, userConfirmed, "localization catalog registration", db => db.LocalizationCatalogRegistrations, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteLocalizationCatalogAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteLocalizationCatalogAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentationBuildRecord>> GetDocumentationBuildsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.DocumentationBuildRecords.AsNoTracking().OrderByDescending(item => item.GeneratedAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not list documentation build records.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<DocumentationBuildRecord?> GetDocumentationBuildAsync(Guid id, CancellationToken cancellationToken = default) {
    try
    {
        return GetByIdAsync(id, "documentation build record", db => db.DocumentationBuildRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetDocumentationBuildAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetDocumentationBuildAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<DocumentationBuildRecord> SaveDocumentationBuildAsync(SaveFeatureRecordRequest<DocumentationBuildRecord> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateWrite(request);
            request.Record.Version = Require(request.Record.Version, nameof(request.Record.Version), 80);
            request.Record.DocumentationMode = Trim(request.Record.DocumentationMode, 120);
            request.Record.PdfMode = Trim(request.Record.PdfMode, 120);
            request.Record.ToolSource = Trim(request.Record.ToolSource, 240);
            request.Record.OutputRoot = Trim(request.Record.OutputRoot, 2048);
            request.Record.Warning = Trim(request.Record.Warning, 4000);
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.DocumentationBuildRecords
                .SingleOrDefaultAsync(item => item.Id == request.Record.Id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                request.Record.Id = request.Record.Id == Guid.Empty ? Guid.NewGuid() : request.Record.Id;
                request.Record.GeneratedAtUtc = request.Record.GeneratedAtUtc == default ? DateTime.UtcNow : request.Record.GeneratedAtUtc;
                db.DocumentationBuildRecords.Add(request.Record);
                existing = request.Record;
            }
            else
            {
                existing.Version = request.Record.Version;
                existing.GeneratedAtUtc = request.Record.GeneratedAtUtc;
                existing.HtmlAvailable = request.Record.HtmlAvailable;
                existing.PdfAvailable = request.Record.PdfAvailable;
                existing.DocumentationMode = request.Record.DocumentationMode;
                existing.PdfMode = request.Record.PdfMode;
                existing.ToolSource = request.Record.ToolSource;
                existing.OutputRoot = request.Record.OutputRoot;
                existing.Warning = request.Record.Warning;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved documentation build record for LocalGPT {Version}.", existing.Version);
            return existing;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save a documentation build record.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteDocumentationBuildAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default) {
    try
    {
        return DeleteAsync(id, userConfirmed, "documentation build record", db => db.DocumentationBuildRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteDocumentationBuildAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteDocumentationBuildAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddedFirmwarePlanRecord>> GetEmbeddedFirmwarePlansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.EmbeddedFirmwarePlanRecords.AsNoTracking().OrderByDescending(item => item.UpdatedAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not list embedded firmware plan records.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<EmbeddedFirmwarePlanRecord?> GetEmbeddedFirmwarePlanAsync(Guid id, CancellationToken cancellationToken = default) {
    try
    {
        return GetByIdAsync(id, "embedded firmware plan record", db => db.EmbeddedFirmwarePlanRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetEmbeddedFirmwarePlanAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetEmbeddedFirmwarePlanAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<EmbeddedFirmwarePlanRecord> SaveEmbeddedFirmwarePlanAsync(SaveFeatureRecordRequest<EmbeddedFirmwarePlanRecord> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateWrite(request);
            request.Record.PlanKey = Require(request.Record.PlanKey, nameof(request.Record.PlanKey), 160);
            request.Record.DeviceName = Trim(request.Record.DeviceName, 240);
            request.Record.BoardProfileKey = Trim(request.Record.BoardProfileKey, 160);
            request.Record.Status = Trim(request.Record.Status, 80);
            request.Record.PlanJson = RequireJson(request.Record.PlanJson, nameof(request.Record.PlanJson));
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.EmbeddedFirmwarePlanRecords
                .SingleOrDefaultAsync(item => item.Id == request.Record.Id || item.PlanKey == request.Record.PlanKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                request.Record.Id = request.Record.Id == Guid.Empty ? Guid.NewGuid() : request.Record.Id;
                request.Record.CreatedAtUtc = DateTime.UtcNow;
                request.Record.UpdatedAtUtc = request.Record.CreatedAtUtc;
                db.EmbeddedFirmwarePlanRecords.Add(request.Record);
                existing = request.Record;
            }
            else
            {
                existing.PlanKey = request.Record.PlanKey;
                existing.ProjectId = request.Record.ProjectId;
                existing.DeviceName = request.Record.DeviceName;
                existing.BoardProfileKey = request.Record.BoardProfileKey;
                existing.Status = request.Record.Status;
                existing.PlanJson = request.Record.PlanJson;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved embedded firmware plan {PlanKey}.", existing.PlanKey);
            return existing;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save an embedded firmware plan record.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteEmbeddedFirmwarePlanAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default) {
    try
    {
        return DeleteAsync(id, userConfirmed, "embedded firmware plan record", db => db.EmbeddedFirmwarePlanRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteEmbeddedFirmwarePlanAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteEmbeddedFirmwarePlanAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<IReadOnlyList<CouncilGameSessionRecord>> GetCouncilGameSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.CouncilGameSessionRecords.AsNoTracking().OrderByDescending(item => item.UpdatedAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not list GameDirector session records.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<CouncilGameSessionRecord?> GetCouncilGameSessionAsync(Guid id, CancellationToken cancellationToken = default) {
    try
    {
        return GetByIdAsync(id, "GameDirector session record", db => db.CouncilGameSessionRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetCouncilGameSessionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(GetCouncilGameSessionAsync)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public async Task<CouncilGameSessionRecord> SaveCouncilGameSessionAsync(SaveFeatureRecordRequest<CouncilGameSessionRecord> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateWrite(request);
            request.Record.SessionKey = Require(request.Record.SessionKey, nameof(request.Record.SessionKey), 160);
            request.Record.GameKey = Require(request.Record.GameKey, nameof(request.Record.GameKey), 160);
            request.Record.TeamKey = Trim(request.Record.TeamKey, 160);
            request.Record.Status = Trim(request.Record.Status, 80);
            request.Record.SnapshotJson = RequireJson(request.Record.SnapshotJson, nameof(request.Record.SnapshotJson));
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existing = await db.CouncilGameSessionRecords
                .SingleOrDefaultAsync(item => item.Id == request.Record.Id || item.SessionKey == request.Record.SessionKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                request.Record.Id = request.Record.Id == Guid.Empty ? Guid.NewGuid() : request.Record.Id;
                request.Record.CreatedAtUtc = DateTime.UtcNow;
                request.Record.UpdatedAtUtc = request.Record.CreatedAtUtc;
                db.CouncilGameSessionRecords.Add(request.Record);
                existing = request.Record;
            }
            else
            {
                existing.SessionKey = request.Record.SessionKey;
                existing.ConversationId = request.Record.ConversationId;
                existing.GameKey = request.Record.GameKey;
                existing.TeamKey = request.Record.TeamKey;
                existing.Status = request.Record.Status;
                existing.SnapshotJson = request.Record.SnapshotJson;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved GameDirector session {SessionKey}.", existing.SessionKey);
            return existing;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save a GameDirector session record.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteCouncilGameSessionAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default) {
    try
    {
        return DeleteAsync(id, userConfirmed, "GameDirector session record", db => db.CouncilGameSessionRecords, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteCouncilGameSessionAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(DeleteCouncilGameSessionAsync)} failed.");
        throw;
    }
}

    /// <summary>Seeds missing maintained direct-Council prompt rows without overwriting database edits.</summary>
    /// <param name="cancellationToken">Cancels the seed operation.</param>
    /// <returns>A task that completes when missing built-in rows are present.</returns>
    private async Task EnsureBuiltInCouncilStartersAsync(CancellationToken cancellationToken)
    {
    try
    {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existingKeys = await db.CouncilPromptStarterConfigurations.AsNoTracking()
                .Select(item => item.Key)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var keySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var changed = false;
            foreach (var starter in catalog.GetSuggestion().Where(item => item.StartsCouncilDirectly))
            {
                if (!keySet.Add(starter.Key))
                    continue;
                db.CouncilPromptStarterConfigurations.Add(new CouncilPromptStarterConfiguration
                {
                    Id = Guid.NewGuid(),
                    Key = starter.Key,
                    Title = starter.Title,
                    Summary = starter.Text,
                    PromptMessage = starter.PromptMessage,
                    TeamKeysJson = System.Text.Json.JsonSerializer.Serialize(starter.TeamKeys),
                    StartsCouncilDirectly = true,
                    IsBuiltIn = true,
                    IsEnabled = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                changed = true;
            }
            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Seeded missing persistent direct-Council prompt starter records.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(EnsureBuiltInCouncilStartersAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(EnsureBuiltInCouncilStartersAsync)} failed.");
        throw;
    }
}

    /// <summary>Ensures database migrations and seeds are ready.</summary>
    /// <param name="cancellationToken">Cancels initialization.</param>
    /// <returns>A task that completes when persistence is ready.</returns>
    private Task EnsureReadyAsync(CancellationToken cancellationToken) {
    try
    {
        return databaseInitializer.InitializeAsync(cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(EnsureReadyAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(EnsureReadyAsync)} failed.");
        throw;
    }
}

    /// <summary>Loads one EF record by its primary key with bounded diagnostics.</summary>
    /// <typeparam name="TRecord">Entity type.</typeparam>
    /// <param name="id">Entity identifier.</param>
    /// <param name="recordName">Bounded record category used in diagnostics.</param>
    /// <param name="selector">Selects the target DbSet.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that returns the matching row or null.</returns>
    private async Task<TRecord?> GetByIdAsync<TRecord>(
        Guid id,
        string recordName,
        Func<LocalGptMemoryDbContext, DbSet<TRecord>> selector,
        CancellationToken cancellationToken) where TRecord : class
    {
        try
        {
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await selector(db).AsNoTracking()
                .SingleOrDefaultAsync(item => EF.Property<Guid>(item, "Id") == id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load {RecordName} {RecordId}.", recordName, id);
            throw;
        }
    }

    /// <summary>Deletes one EF record after explicit confirmation.</summary>
    /// <typeparam name="TRecord">Entity type.</typeparam>
    /// <param name="id">Entity identifier.</param>
    /// <param name="userConfirmed">Whether the user approved deletion.</param>
    /// <param name="recordName">Bounded record category used in diagnostics.</param>
    /// <param name="selector">Selects the target DbSet.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that returns true when the row existed and was deleted.</returns>
    private async Task<bool> DeleteAsync<TRecord>(
        Guid id,
        bool userConfirmed,
        string recordName,
        Func<LocalGptMemoryDbContext, DbSet<TRecord>> selector,
        CancellationToken cancellationToken) where TRecord : class
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException($"Fresh human confirmation is required before deleting a {recordName}.");
            await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var record = await selector(db).FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
            if (record is null)
                return false;
            selector(db).Remove(record);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted {RecordName} {RecordId}.", recordName, id);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not delete {RecordName} {RecordId}.", recordName, id);
            throw;
        }
    }

    /// <summary>Requires explicit approval and a non-null record.</summary>
    /// <typeparam name="TRecord">Requested record type.</typeparam>
    /// <param name="request">Write request to validate.</param>
    private void ValidateWrite<TRecord>(SaveFeatureRecordRequest<TRecord> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Record is null)
            throw new ArgumentException("A feature record is required.", nameof(request));
        if (!request.UserConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required before changing persistent feature data.");
    }

    /// <summary>Normalizes and validates a prompt starter.</summary>
    /// <param name="record">Record to normalize.</param>
    private void NormalizeStarter(CouncilPromptStarterConfiguration record)
    {
        record.Key = Require(record.Key, nameof(record.Key), 160).ToLowerInvariant();
        record.Title = Require(record.Title, nameof(record.Title), 240);
        record.Summary = Trim(record.Summary, 1000);
        record.PromptMessage = Require(record.PromptMessage, nameof(record.PromptMessage), 200_000);
        record.TeamKeysJson = RequireJson(record.TeamKeysJson, nameof(record.TeamKeysJson));
    }

    /// <summary>Normalizes and validates a localization catalog registration.</summary>
    /// <param name="record">Record to normalize.</param>
    private void NormalizeLocalization(LocalizationCatalogRegistration record)
    {
        record.CultureName = Require(record.CultureName, nameof(record.CultureName), 40);
        record.DisplayName = Require(record.DisplayName, nameof(record.DisplayName), 240);
        record.CatalogPath = Require(record.CatalogPath, nameof(record.CatalogPath), 2048);
        record.StringCount = Math.Max(0, record.StringCount);
        record.MissingBaselineKeyCount = Math.Max(0, record.MissingBaselineKeyCount);
    }

    /// <summary>Requires a bounded non-empty string.</summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="name">Parameter name used by the exception.</param>
    /// <param name="maximumLength">Maximum accepted length.</param>
    /// <returns>The trimmed value.</returns>
    private string Require(string? value, string name, int maximumLength)
    {
    try
    {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length == 0)
                throw new ArgumentException($"{name} is required.", name);
            if (normalized.Length > maximumLength)
                throw new ArgumentOutOfRangeException(name, $"{name} must not exceed {maximumLength} characters.");
            return normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(Require)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(Require)} failed.");
        throw;
    }
}

    /// <summary>Trims a nullable string to a bounded length.</summary>
    /// <param name="value">Candidate value.</param>
    /// <param name="maximumLength">Maximum stored length.</param>
    /// <returns>The bounded value.</returns>
    private string Trim(string? value, int maximumLength)
    {
    try
    {
            var normalized = value?.Trim() ?? string.Empty;
            return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(Trim)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(Trim)} failed.");
        throw;
    }
}

    /// <summary>Validates and returns a JSON object or array string.</summary>
    /// <param name="value">JSON text.</param>
    /// <param name="name">Parameter name used by the exception.</param>
    /// <returns>The validated JSON text.</returns>
    private string RequireJson(string? value, string name)
    {
    try
    {
            var normalized = Require(value, name, 4_000_000);
            using var _ = System.Text.Json.JsonDocument.Parse(normalized);
            return normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(RequireJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(FeaturePersistenceService)}.{nameof(RequireJson)} failed.");
        throw;
    }
}
}
