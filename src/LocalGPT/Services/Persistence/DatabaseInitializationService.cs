using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database health, compatibility reconciliation, EF migration, and deterministic seeding.
/// Low-level migration-history inspection belongs to <see cref="IDatabaseMigrationCompatibilityService"/>.
/// </summary>
public sealed partial class DatabaseInitializationService : IDatabaseInitializationService
{
    /// <summary>
    /// Stores the database context factory dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
    /// <summary>
    /// Stores the database file health service dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseFileHealthService databaseFileHealth;
    /// <summary>
    /// Stores the database migration compatibility service dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseMigrationCompatibilityService migrationCompatibility;
    /// <summary>
    /// Stores the initial data catalog dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IInitialDataCatalog catalog;
    /// <summary>
    /// Stores the local GPT runtime policy seed data service dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ILocalGptRuntimePolicySeedDataService runtimePolicySeed;
    /// <summary>
    /// Stores the service activity service dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IServiceActivityService serviceActivity;
    /// <summary>
    /// Stores the database logger readiness dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseLoggerReadiness databaseLoggerReadiness;
    /// <summary>
    /// Stores the host environment dependency used by <see cref="DatabaseInitializationService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IHostEnvironment hostEnvironment;
    /// <summary>
    /// Stores the logger used by <see cref="DatabaseInitializationService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<DatabaseInitializationService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="dbContextFactory">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="databaseFileHealth">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="migrationCompatibility">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="catalog">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="runtimePolicySeed">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="serviceActivity">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="databaseLoggerReadiness">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="hostEnvironment">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="logger">Injected dependency used by the DatabaseInitializationService.</param>
    public DatabaseInitializationService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseFileHealthService databaseFileHealth,
        IDatabaseMigrationCompatibilityService migrationCompatibility,
        IInitialDataCatalog catalog,
        ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
        IServiceActivityService serviceActivity,
        IDatabaseLoggerReadiness databaseLoggerReadiness,
        IHostEnvironment hostEnvironment,
        ILogger<DatabaseInitializationService> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.databaseFileHealth = databaseFileHealth;
        this.migrationCompatibility = migrationCompatibility;
        this.catalog = catalog;
        this.runtimePolicySeed = runtimePolicySeed;
        this.serviceActivity = serviceActivity;
        this.databaseLoggerReadiness = databaseLoggerReadiness;
        this.hostEnvironment = hostEnvironment;
        this.logger = logger;
    }

    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="DatabaseInitializationService"/>.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>
    /// Stores the internal initialized state used by <see cref="DatabaseInitializationService"/> while executing its surrounding workflow.
    /// </summary>
    private volatile bool initialized;

    /// <summary>
    /// Performs initialize as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default) {
    try
    {
        return serviceActivity.RunAsync(
            nameof(DatabaseInitializationService),
            nameof(InitializeAsync),
            InitializeCoreAsync,
            cancellationToken,
            "Database migration and deterministic initial data feed completed.");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(InitializeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(InitializeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs initialize core as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (IsInitializedStorePresent())
                return;

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsInitializedStorePresent())
                    return;

                await databaseFileHealth.EnsureHealthyOrRecoverAsync(cancellationToken).ConfigureAwait(false);
                await migrationCompatibility.PrepareAsync(cancellationToken).ConfigureAwait(false);
                await RunMigrationAsync(cancellationToken).ConfigureAwait(false);

                // Each seed stage receives a fresh DbContext. A failed SaveChanges therefore cannot leave
                // stale tracked entities behind and trigger unrelated concurrency failures in the next stage.
                await RunSeedStageAsync("regex definitions", SeedRegexAsync, cancellationToken).ConfigureAwait(false);
                await RunSeedStageAsync("prompt definitions", SeedPromptsAsync, cancellationToken).ConfigureAwait(false);
                await RunSeedStageAsync("system variables", SeedVariablesAsync, cancellationToken).ConfigureAwait(false);
                await RunSeedStageAsync("knowledge entries", SeedKnowledgeAsync, cancellationToken).ConfigureAwait(false);
                await RunSeedStageAsync("core projects", SeedCoreProjectsAsync, cancellationToken).ConfigureAwait(false);
                await RunSeedStageAsync("Council model presets", SeedCouncilModelPresetsAsync, cancellationToken).ConfigureAwait(false);

                initialized = true;
                databaseLoggerReadiness.MarkReady();
                logger.LogInformation("LocalGPT database migration and initial data feed completed.");
            }
            finally
            {
                gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(InitializeCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(InitializeCoreAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs run migration as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunMigrationAsync(CancellationToken cancellationToken)
    {
        var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "LocalGPT database migration was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT database migration failed. Deterministic seed stages were not started.");
            throw;
        }
    }

    /// <summary>
    /// Performs run seed stage as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stageName">Stage name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="seed">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunSeedStageAsync(
        string stageName,
        Func<LocalGptMemoryDbContext, CancellationToken, Task> seed,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 2;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            try
            {
                await seed(db, cancellationToken).ConfigureAwait(false);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                if (await TryReconcileSeedConcurrencyAsync(db, stageName, attempt, exception, cancellationToken).ConfigureAwait(false))
                    return;

                if (attempt < maximumAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Database seed stage {SeedStage} encountered a concurrency conflict on attempt {Attempt}; retrying once with a fresh DbContext.",
                        stageName,
                        attempt);
                    continue;
                }

                logger.LogError(
                    exception,
                    "Database seed stage {SeedStage} retained an unresolved concurrency conflict after {AttemptCount} attempt(s); later independent stages will continue.",
                    stageName,
                    maximumAttempts);
                return;
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(exception, "Database seed stage {SeedStage} was cancelled.", stageName);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Database seed stage {SeedStage} failed on attempt {Attempt}; later independent stages will continue.",
                    stageName,
                    attempt);
                return;
            }
        }

    }

    /// <summary>
    /// Attempts to reconcile seed concurrency as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="stageName">Stage name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="attempt">Attempt value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> TryReconcileSeedConcurrencyAsync(
        LocalGptMemoryDbContext db,
        string stageName,
        int attempt,
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var conflictingEntryCount = exception.Entries.Count;

            // Never issue a second SaveChanges from the failed DbContext. EF still owns the original
            // modification batch and another save can repeat the same stale concurrency predicate.
            // A fresh context on the next bounded attempt reloads durable rows and applies only missing,
            // additive seed records. Existing user rows remain authoritative.
            foreach (var entry in exception.Entries)
                entry.State = EntityState.Detached;
            db.ChangeTracker.Clear();

            logger.LogWarning(
                exception,
                "Database seed stage {SeedStage} discarded {ConflictingEntryCount} stale tracked seed row(s) on attempt {Attempt}; a fresh DbContext will retry additive seed work without overwriting durable user values.",
                stageName,
                conflictingEntryCount,
                attempt);
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException cancellationException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(cancellationException, "Concurrency reconciliation for database seed stage {SeedStage} was cancelled.", stageName);
            throw;
        }
        catch (Exception reconciliationException)
        {
            logger.LogWarning(
                reconciliationException,
                "Database seed stage {SeedStage} could not clear stale tracked seed rows on attempt {Attempt}.",
                stageName,
                attempt);
            return false;
        }
    }

    /// <summary>
    /// Determines whether initialized store present as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsInitializedStorePresent() {
    try
    {
        return initialized && File.Exists(databaseFileHealth.DatabasePath);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(IsInitializedStorePresent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(IsInitializedStorePresent)} failed.");
        throw;
    }
}
}

/// <summary>
/// Coordinates database initialization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="initializer">Database initialization service dependency used by the database initialization workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DatabaseInitializationHostedService(
    IDatabaseInitializationService initializer,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    /// <summary>
    /// Performs start as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "LocalGPT database initialization failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs stop as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StopAsync(CancellationToken cancellationToken) {
    try
    {
        return Task.CompletedTask;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationHostedService)}.{nameof(StopAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationHostedService)}.{nameof(StopAsync)} failed.");
        throw;
    }
}
}

