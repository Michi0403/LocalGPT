using LocalGPT.Services;
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
    /// <param name="databaseLoggerReadiness">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="hostEnvironment">Injected dependency used by the DatabaseInitializationService.</param>
    /// <param name="logger">Injected dependency used by the DatabaseInitializationService.</param>
    public DatabaseInitializationService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseFileHealthService databaseFileHealth,
        IDatabaseMigrationCompatibilityService migrationCompatibility,
        IInitialDataCatalog catalog,
        ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
        IDatabaseLoggerReadiness databaseLoggerReadiness,
        IHostEnvironment hostEnvironment,
        ILogger<DatabaseInitializationService> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.databaseFileHealth = databaseFileHealth;
        this.migrationCompatibility = migrationCompatibility;
        this.catalog = catalog;
        this.runtimePolicySeed = runtimePolicySeed;
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
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace("Checking whether boot-critical LocalGPT database initialization is required.");
            var initializedNow = await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
            if (initializedNow)
                logger.LogInformation("Database migration and deterministic initial data feed completed.");
            else
                logger.LogTrace("Database initialization request reused the already initialized LocalGPT store.");
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
    /// <returns>A task whose result is <see langword="true"/> only when this call performed migration/seeding work.</returns>
    private async Task<bool> InitializeCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (IsInitializedStorePresent())
                return false;

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsInitializedStorePresent())
                    return false;

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
                return true;
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
                await VerifySeedStageAsync(db, stageName, cancellationToken).ConfigureAwait(false);
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
                    "Database seed stage {SeedStage} retained an unresolved concurrency conflict after {AttemptCount} attempt(s); initialization cannot be marked complete.",
                    stageName,
                    maximumAttempts);
                throw new InvalidOperationException($"Mandatory seed stage '{stageName}' has unresolved concurrency conflicts.", exception);
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
                    "Database seed stage {SeedStage} failed on attempt {Attempt}; initialization cannot be marked complete.",
                    stageName,
                    attempt);
                throw new InvalidOperationException($"Mandatory seed stage '{stageName}' did not complete.", exception);
            }
        }

    }

    /// <summary>Checks durable baseline keys after a seed stage committed; never mistakes a partial seed for readiness.</summary>
    /// <param name="db">Freshly committed database context.</param>
    /// <param name="stageName">Current seed stage.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A completed task only after mandatory seed records have been verified.</returns>
    private async Task VerifySeedStageAsync(LocalGptMemoryDbContext db, string stageName, CancellationToken token)
    {
        if (stageName == "regex definitions")
        {
            var expected = catalog.RegexPatterns.Select(row => row.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = (await db.RegexPatterns.AsNoTracking().Select(row => row.Name).ToListAsync(token).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"Regex seed incomplete: {expected.Except(actual).Count()} required patterns are missing.");
        }
        else if (stageName == "prompt definitions")
        {
            var expected = catalog.Prompts.Select(row => row.Key + "|" + row.Language).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = (await db.Prompts.AsNoTracking().Select(row => row.Key + "|" + row.Language).ToListAsync(token).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"Prompt seed incomplete: {expected.Except(actual).Count()} required prompts are missing.");
        }
        else if (stageName == "system variables")
        {
            var expected = catalog.Variables.Select(row => row.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = (await db.SystemVariables.AsNoTracking().Select(row => row.Name).ToListAsync(token).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"System variable seed incomplete: {expected.Except(actual).Count()} required variables are missing.");
        }
        else if (stageName == "Council model presets")
        {
            var expected = CreateDefaultCouncilModelPresets().Select(row => row.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = (await db.CouncilModelPresets.AsNoTracking()
                .Select(row => row.Name).ToListAsync(token).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"Council preset seed incomplete: {expected.Except(actual).Count()} required presets are missing.");
        }
        else if (stageName == "core projects")
        {
            var expected = new[]
            {
                runtimePolicySeed.GetSeed().LocalGptCoreProjectId,
                Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6102")
            }.ToHashSet();
            var actual = (await db.LocalGptProjects.AsNoTracking()
                .Where(row => expected.Contains(row.Id))
                .Select(row => row.Id).ToListAsync(token).ConfigureAwait(false)).ToHashSet();
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"Core project seed incomplete: {expected.Except(actual).Count()} required projects are missing.");
        }
        else if (stageName == "knowledge entries")
        {
            var expectedRows = await catalog.LoadKnowledgeAsync(token).ConfigureAwait(false);
            var expected = expectedRows.Select(row => row.Id).ToHashSet();
            var actualRows = await db.CouncilKnowledgeEntries.AsNoTracking().Where(row => expected.Contains(row.Id))
                .Select(row => new { row.Id, row.Content, row.Source, row.SourceHash }).ToListAsync(token).ConfigureAwait(false);
            var actual = actualRows.Select(row => row.Id).ToHashSet();
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"Knowledge seed incomplete: {expected.Except(actual).Count()} required reference records are missing.");

            // Seed-owned, unedited content must match the shipped document. A user-edited
            // record is intentionally NOT repaired over the user's changes.
            var sourceHashes = expectedRows.ToDictionary(row => row.Id, row => row.SourceHash);
            foreach (var persisted in actualRows)
            {
                var seedOwned = persisted.Source.StartsWith("repository:", StringComparison.OrdinalIgnoreCase)
                    || persisted.Source.StartsWith("embedded:", StringComparison.OrdinalIgnoreCase)
                    || persisted.Source.StartsWith("builtin:core-knowledge:", StringComparison.OrdinalIgnoreCase);
                if (!seedOwned)
                    continue;
                var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(persisted.Content)));
                if (!string.Equals(contentHash, persisted.SourceHash, StringComparison.OrdinalIgnoreCase))
                    continue; // User-edited content, preserved even if the seed changed.
                if (!string.Equals(persisted.SourceHash, sourceHashes[persisted.Id], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Knowledge reference {persisted.Id} differs from the packaged seed after reconciliation.");
            }
            logger.LogInformation("Verified {Count} mandatory Knowledge seed records and unchanged source hashes.", expected.Count);
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
/// Repairs release-owned defaults in an existing user database after every application upgrade,
/// and reports verified seed coverage for testers without exporting their private data.
/// </summary>
/// <param name="initializer">Coordinates database migration and mandatory seed stages.</param>
/// <param name="runtimePolicy">Reloads persisted policy overrides after initialization.</param>
/// <param name="scopeFactory">Creates the scoped Council seed services without DI cycles.</param>
/// <param name="dbContextFactory">Provides independent post-commit verification reads.</param>
/// <param name="catalog">Provides the maintained mandatory regex, prompt, variable and Knowledge seed keys.</param>
/// <param name="runtimePolicySeed">Provides the stable core project identity.</param>
/// <param name="applicationPaths">Provides the current user's safe runtime report directory.</param>
/// <param name="logger">Records initialization and repair diagnostics.</param>
public sealed class DatabaseInitializationHostedService(
    IDatabaseInitializationService initializer,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IServiceScopeFactory scopeFactory,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IInitialDataCatalog catalog,
    ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
    ILocalGptApplicationPathService applicationPaths,
    ILogger<DatabaseInitializationHostedService> logger) : BackgroundService
{
    /// <summary>Starts core and supplemental seed reconciliation after the host starts.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1, stoppingToken).ConfigureAwait(false);
        var phase = "core database migration and seeding";
        try
        {
            logger.LogInformation("Starting LocalGPT database initialization and existing-user default reconciliation in the background.");
            await initializer.InitializeAsync(stoppingToken).ConfigureAwait(false);
            phase = "runtime policy reload";
            try
            {
                runtimePolicy.Reload();
                logger.LogInformation("Reloaded persisted LocalGPT runtime policy after database initialization.");
            }
            catch (Exception ex)
            {
                // Runtime policy already has its valid built-in defaults; this optional
                // reload must not suppress the other mandatory seed repairs.
                logger.LogError(ex, "Persisted runtime-policy overrides could not be reloaded; the built-in seed remains active.");
            }

            // Council teams and game/runtime classes were previously lazy-seeded only
            // on the first request for those screens/functions. An existing installation
            // may not visit those paths after an update: explicitly reconcile here.
            phase = "Council team templates";
            await using var scope = scopeFactory.CreateAsyncScope();
            var teams = scope.ServiceProvider.GetRequiredService<ICouncilTeamConfigurationService>();
            await teams.ReconcileDefaultsAsync(stoppingToken).ConfigureAwait(false);
            phase = "Council game/runtime classes";
            var runtimeClasses = scope.ServiceProvider.GetRequiredService<ICouncilRuntimeClassService>();
            await runtimeClasses.ReconcileDefaultsAsync(stoppingToken).ConfigureAwait(false);

            phase = "post-repair seed audit";
            var coverage = await AuditRequiredDefaultsAsync(teams, runtimeClasses, stoppingToken).ConfigureAwait(false);
            var missing = coverage.Where(item => item.Present < item.Required).ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException("Default seed coverage incomplete: " +
                    string.Join(", ", missing.Select(item => $"{item.Category} {item.Present}/{item.Required}")));

            await WriteStatusReportAsync("Complete", phase, coverage, stoppingToken).ConfigureAwait(false);
            logger.LogInformation(
                "Release defaults verified for this user database across {CategoryCount} categories: {Summary}. Seed report: {ReportFile}",
                coverage.Count,
                string.Join("; ", coverage.Select(item => $"{item.Category}={item.Present}/{item.Required}")),
                Path.Combine(applicationPaths.GetLayout().RuntimeDirectory, "seed-reconciliation.json"));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Default seed reconciliation canceled because the host is stopping.");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "LocalGPT mandatory default reconciliation failed during {Phase}; the installation must not be considered fully seeded.", phase);
            await WriteStatusReportAsync("Failed", phase, [], CancellationToken.None).ConfigureAwait(false);
            // A failed core migration/seed is fatal because even the database contract is unknown.
            // A supplemental Council/game template repair should remain visible and retryable
            // without taking down unrelated chat, setup, backup and recovery facilities.
            if (string.Equals(phase, "core database migration and seeding", StringComparison.Ordinal))
                throw;
            logger.LogWarning("LocalGPT remains available in degraded default-data state. Repair will be retried on the next startup.");
        }
    }

    /// <summary>Rechecks persisted keys independently of seeding methods; no user content is exported.</summary>
    private async Task<IReadOnlyList<SeedCategoryCoverage>> AuditRequiredDefaultsAsync(
        ICouncilTeamConfigurationService teams,
        ICouncilRuntimeClassService runtimeClasses,
        CancellationToken token)
    {
        try
        {
            var db = await dbContextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var expectedKnowledge = (await catalog.LoadKnowledgeAsync(token).ConfigureAwait(false))
                .Select(item => item.Id).ToHashSet();
            var expectedPrompts = catalog.Prompts.Select(item => item.Key + "|" + item.Language)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedRegex = catalog.RegexPatterns.Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedVariables = catalog.Variables.Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedTeams = (await teams.GetDefaultTemplatesAsync(token).ConfigureAwait(false))
                .Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedRuntime = runtimeClasses.GetSuppliedSeedKeys().ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (expectedTeams.Count == 0 || expectedRuntime.Count == 0)
                throw new InvalidOperationException("Supplied Council team or game/runtime seed catalog is empty.");
            var expectedPresets = new[]
            {
                "Reactive ASCII Gameplay", "Adaptive Mixed Hardware Council", "Learning Round",
                "Fast Game Council (Low-B)", "Code Curator Council", "Benchmark Candidate Pool"
            }.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedCoreProjects = new[]
            {
                runtimePolicySeed.GetSeed().LocalGptCoreProjectId,
                Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6102")
            }.ToHashSet();

            var knowledge = (await db.CouncilKnowledgeEntries.AsNoTracking().Select(item => item.Id).ToListAsync(token).ConfigureAwait(false)).ToHashSet();
            var prompts = (await db.Prompts.AsNoTracking().Select(item => item.Key + "|" + item.Language).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var regex = (await db.RegexPatterns.AsNoTracking().Select(item => item.Name).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var variables = (await db.SystemVariables.AsNoTracking().Select(item => item.Name).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var teamKeys = (await db.CouncilTeamConfigurations.AsNoTracking().Select(item => item.Key).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var runtimeKeys = (await db.CouncilRuntimeClassConfigurations.AsNoTracking().Select(item => item.Key).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var presetNames = (await db.CouncilModelPresets.AsNoTracking().Select(item => item.Name).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var projectIds = (await db.LocalGptProjects.AsNoTracking().Select(item => item.Id).ToListAsync(token).ConfigureAwait(false)).ToHashSet();

            return
            [
                new("Knowledge (C#, DevExpress, Ollama, DXFunctions, source references)", expectedKnowledge.Count, expectedKnowledge.Count(knowledge.Contains)),
                new("Regex patterns", expectedRegex.Count, expectedRegex.Count(regex.Contains)),
                new("Prompts", expectedPrompts.Count, expectedPrompts.Count(prompts.Contains)),
                new("System variables", expectedVariables.Count, expectedVariables.Count(variables.Contains)),
                new("Council team templates", expectedTeams.Count, expectedTeams.Count(teamKeys.Contains)),
                new("Council game/runtime classes", expectedRuntime.Count, expectedRuntime.Count(runtimeKeys.Contains)),
                new("Council model presets", expectedPresets.Count, expectedPresets.Count(presetNames.Contains)),
                new("Core project templates", expectedCoreProjects.Count, expectedCoreProjects.Count(projectIds.Contains))
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Post-repair default inventory audit failed.");
            throw;
        }
    }

    /// <summary>Writes a privacy-safe, replaceable status document under this user's runtime directory.</summary>
    private async Task WriteStatusReportAsync(
        string status,
        string phase,
        IReadOnlyList<SeedCategoryCoverage> coverage,
        CancellationToken token)
    {
        try
        {
            var runtimeDirectory = applicationPaths.GetLayout().RuntimeDirectory;
            Directory.CreateDirectory(runtimeDirectory);
            var path = Path.Combine(runtimeDirectory, "seed-reconciliation.json");
            var temporary = path + ".tmp";
            var content = JsonSerializer.Serialize(new
            {
                Status = status,
                ReleaseVersion = typeof(DatabaseInitializationService).Assembly.GetName().Version?.ToString(),
                VerifiedAtUtc = DateTime.UtcNow,
                Phase = phase,
                Scope = "This operating-system user's existing LocalGPT database; user-owned content preserved",
                Categories = coverage.Select(item => new { item.Category, item.Required, item.Present }).ToArray()
            }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(temporary, content, token).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception exception)
        {
            // Report I/O is separate from actual seed success; failure stays visible in logs.
            logger.LogError(exception, "Could not persist runtime/seed-reconciliation.json; check default reconciliation logs.");
        }
    }

    private sealed record SeedCategoryCoverage(string Category, int Required, int Present);
}
