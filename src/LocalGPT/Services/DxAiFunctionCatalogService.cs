using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>
/// Keeps the user-facing DX function catalog in the existing SystemVariables table. This deliberately avoids
/// destructive schema replacement: old databases receive missing records, while user-edited exposure and
/// confirmation settings survive descriptor refreshes and application upgrades.
/// </summary>
public sealed partial class DxAiFunctionCatalogService : IDxAiFunctionCatalogService
{
    /// <summary>
    /// Stores the local GPT vocabulary service dependency used by <see cref="DxAiFunctionCatalogService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ILocalGptVocabularyService vocabulary;
    /// <summary>
    /// Stores the internal synchronization gate state used by <see cref="DxAiFunctionCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly DxAiFunctionCatalogSynchronizationGate synchronizationGate;
    /// <summary>
    /// Stores the database initialization service dependency used by <see cref="DxAiFunctionCatalogService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDatabaseInitializationService databaseInitialization;
    /// <summary>
    /// Stores the database context factory dependency used by <see cref="DxAiFunctionCatalogService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
    /// <summary>
    /// Stores the DevExpress AI function registry dependency used by <see cref="DxAiFunctionCatalogService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDxAiFunctionRegistry registry;
    /// <summary>
    /// Stores the organic addon manifest service dependency used by <see cref="DxAiFunctionCatalogService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IOrganicAddonManifestService addonManifests;
    /// <summary>Stores the user-owned dynamic DXFunction service used to refresh runtime descriptors before catalog synchronization.</summary>
    private readonly IUserDxAiFunctionService userFunctions;
    /// <summary>
    /// Stores the logger used by <see cref="DxAiFunctionCatalogService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<DxAiFunctionCatalogService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="vocabulary">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="synchronizationGate">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="databaseInitialization">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="dbContextFactory">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="registry">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="addonManifests">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="logger">Injected dependency used by DxAiFunctionCatalogService.</param>
    /// <param name="userFunctions">User devexpress ai function service dependency used by the DevExpress AI function catalog workflow to provide the corresponding application capability.</param>
    public DxAiFunctionCatalogService(
        ILocalGptVocabularyService vocabulary,
        DxAiFunctionCatalogSynchronizationGate synchronizationGate,
        IDatabaseInitializationService databaseInitialization,
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDxAiFunctionRegistry registry,
        IOrganicAddonManifestService addonManifests,
        IUserDxAiFunctionService userFunctions,
        ILogger<DxAiFunctionCatalogService> logger)
    {
        this.vocabulary = vocabulary;
        this.synchronizationGate = synchronizationGate;
        this.databaseInitialization = databaseInitialization;
        this.dbContextFactory = dbContextFactory;
        this.registry = registry;
        this.addonManifests = addonManifests;
        this.userFunctions = userFunctions;
        this.logger = logger;
    }

    /// <summary>
    /// Defines the data type constant used by <see cref="DxAiFunctionCatalogService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string DataType = "DxAiFunctionCatalogEntry";
    /// <summary>
    /// Defines the storage name prefix constant used by <see cref="DxAiFunctionCatalogService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string StorageNamePrefix = "DxFunctionCatalog.";
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="DxAiFunctionCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Performs synchronize as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await userFunctions.RefreshAsync(cancellationToken).ConfigureAwait(false);
        await synchronizationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await SynchronizeCoreAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException exception) when (
                    attempt < 3 && IsSystemVariableNameConflict(exception))
                {
                    logger.LogWarning(
                        exception,
                        "DX function catalog storage changed concurrently. Retrying synchronization attempt {Attempt} of 3 without replacing user policy.",
                        attempt + 1);
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            synchronizationGate.Release();
        }
    }

    /// <summary>
    /// Performs synchronize core as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            // Catalog rows from earlier versions may carry a legacy DataType or a partially written payload.
            // Load both the owned storage-name range and the current DataType so a unique Name is always reused
            // instead of queued as a second INSERT.
            var variables = await db.SystemVariables
                .Where(item => item.DataType == DataType || item.Name.StartsWith(StorageNamePrefix))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var storedRows = variables
                .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                .Where(item => item.Entry is not null && !string.IsNullOrWhiteSpace(item.Entry.CatalogKey))
                .Select(item => (item.Variable, Entry: item.Entry!))
                .ToList();
            var existing = new Dictionary<string, (SystemVariable Variable, DxAiFunctionCatalogEntry Entry)>(StringComparer.OrdinalIgnoreCase);
            var duplicateCount = 0;
            var removedVariables = new HashSet<SystemVariable>();
            foreach (var group in storedRows.GroupBy(item => GetSemanticIdentity(item.Entry), StringComparer.OrdinalIgnoreCase))
            {
                var canonical = SelectCanonicalCatalogRow(group);
                canonical.Variable.DataType = DataType;
                existing[group.Key] = canonical;
                foreach (var duplicate in group.Where(item => item.Variable.Id != canonical.Variable.Id))
                {
                    db.SystemVariables.Remove(duplicate.Variable);
                    removedVariables.Add(duplicate.Variable);
                    duplicateCount++;
                }
            }

            if (duplicateCount > 0)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var variablesByName = variables
                .Where(variable => !removedVariables.Contains(variable))
                .GroupBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(variable => variable.LastUpdated).ThenBy(variable => variable.Id).First(),
                    StringComparer.OrdinalIgnoreCase);
            var claimedVariables = new HashSet<SystemVariable>();
            var discovered = DiscoverEntries();
            var discoveredKeys = discovered.Select(GetSemanticIdentity).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in discovered)
            {
                var semanticIdentity = GetSemanticIdentity(descriptor);
                var storageName = BuildStorageName(descriptor.CatalogKey);
                var hasStored = existing.TryGetValue(semanticIdentity, out var stored);
                variablesByName.TryGetValue(storageName, out var namedVariable);

                if (namedVariable is not null && (!hasStored || namedVariable.Id != stored.Variable.Id))
                {
                    // The unique storage name already exists, possibly with a legacy DataType or stale payload.
                    // Reuse that row and carry forward the policy from the semantic match when one exists.
                    var refreshedEntry = hasStored ? stored.Entry : Deserialize(namedVariable.ValueString) ?? descriptor;
                    PreservePolicyAndRefreshDescriptor(refreshedEntry, descriptor);
                    namedVariable.Name = storageName;
                    namedVariable.DataType = DataType;
                    namedVariable.ValueString = JsonSerializer.Serialize(refreshedEntry, JsonOptions);
                    namedVariable.LastUpdated = DateTime.UtcNow;

                    if (hasStored && stored.Variable.Id != namedVariable.Id)
                    {
                        db.SystemVariables.Remove(stored.Variable);
                        removedVariables.Add(stored.Variable);
                        duplicateCount++;
                    }

                    foreach (var priorKey in existing
                        .Where(item => ReferenceEquals(item.Value.Variable, namedVariable) &&
                            !string.Equals(item.Key, semanticIdentity, StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.Key)
                        .ToList())
                    {
                        existing.Remove(priorKey);
                    }

                    existing[semanticIdentity] = (namedVariable, refreshedEntry);
                    claimedVariables.Add(namedVariable);
                    continue;
                }

                if (hasStored)
                {
                    PreservePolicyAndRefreshDescriptor(stored.Entry, descriptor);
                    stored.Variable.Name = storageName;
                    stored.Variable.DataType = DataType;
                    stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                    stored.Variable.LastUpdated = DateTime.UtcNow;
                    variablesByName[storageName] = stored.Variable;
                    claimedVariables.Add(stored.Variable);
                    continue;
                }

                var variable = new SystemVariable
                {
                    Name = storageName,
                    DataType = DataType,
                    ValueString = JsonSerializer.Serialize(descriptor, JsonOptions),
                    LastUpdated = DateTime.UtcNow
                };
                db.SystemVariables.Add(variable);
                variablesByName[storageName] = variable;
                existing[semanticIdentity] = (variable, descriptor);
                claimedVariables.Add(variable);
            }

            foreach (var stored in existing.Values.Where(item =>
                !claimedVariables.Contains(item.Variable) &&
                !removedVariables.Contains(item.Variable) &&
                !discoveredKeys.Contains(GetSemanticIdentity(item.Entry))))
            {
                if (string.Equals(stored.Entry.Source, "UserDxFunction", StringComparison.OrdinalIgnoreCase))
                {
                    db.SystemVariables.Remove(stored.Variable);
                    removedVariables.Add(stored.Variable);
                    continue;
                }
                stored.Entry.IsAvailable = false;
                stored.Entry.UpdatedAtUtc = DateTime.UtcNow;
                stored.Entry.UpdatedBy = "LocalGPT runtime catalog";
                stored.Variable.DataType = DataType;
                stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                stored.Variable.LastUpdated = DateTime.UtcNow;
            }

            foreach (var invalidLegacyRow in variables.Where(variable =>
                !claimedVariables.Contains(variable) &&
                !removedVariables.Contains(variable) &&
                variable.Name.StartsWith(StorageNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                Deserialize(variable.ValueString) is null))
            {
                db.SystemVariables.Remove(invalidLegacyRow);
                removedVariables.Add(invalidLegacyRow);
                duplicateCount++;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var result = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Synchronized {CatalogCount} unique DX function/public service catalog entries and removed {DuplicateCount} duplicate rows without replacing user policy.",
                result.Count,
                duplicateCount);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SynchronizeCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SynchronizeCoreAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether system variable name conflict as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="exception">Exception value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSystemVariableNameConflict(DbUpdateException exception)
    {
    try
    {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqliteException sqliteException &&
                    sqliteException.SqliteErrorCode == 19 &&
                    sqliteException.Message.Contains(
                        "UNIQUE constraint failed: SystemVariables.Name",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSystemVariableNameConflict)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSystemVariableNameConflict)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves entries as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var entries = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
            return entries.Count == 0 ? await SynchronizeAsync(cancellationToken).ConfigureAwait(false) : entries;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntriesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntriesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves entry as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="catalogKey">Catalog key value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    public async Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalogKey);
            return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.CatalogKey, catalogKey, StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntryAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntryAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves by function name as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    public async Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
            return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.FunctionName, functionName, StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetByFunctionNameAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetByFunctionNameAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists policy as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
    public async Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.CatalogKey);
            _ = JsonSerializer.Deserialize<List<string>>(request.AllowedPeerIdsJson) ?? [];

            await synchronizationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
                var variables = await db.SystemVariables
                    .Where(item => item.DataType == DataType)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                var matches = variables
                    .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                    .Where(item => item.Entry is not null && string.Equals(item.Entry.CatalogKey, request.CatalogKey, StringComparison.OrdinalIgnoreCase))
                    .Select(item => (item.Variable, Entry: item.Entry!))
                    .ToList();
                if (matches.Count == 0)
                    throw new KeyNotFoundException($"DX function catalog entry '{request.CatalogKey}' was not found. Synchronize the catalog first.");

                var match = SelectCanonicalCatalogRow(matches);
                foreach (var duplicate in matches.Where(item => item.Variable.Id != match.Variable.Id))
                    db.SystemVariables.Remove(duplicate.Variable);

                match.Variable.Name = BuildStorageName(match.Entry.CatalogKey);
                match.Variable.DataType = DataType;
                match.Entry.IsEnabled = request.IsEnabled;
                match.Entry.ExposeToAiChat = request.ExposeToAiChat;
                match.Entry.ExposeToOneWire = request.ExposeToOneWire;
                match.Entry.AllowRemoteInvocation = request.AllowRemoteInvocation;
                match.Entry.RequiresFrontendConfirmation = request.RequiresFrontendConfirmation;
                match.Entry.InteractionEditor = request.InteractionEditor;
                match.Entry.AllowedPeerIdsJson = NormalizePeersJson(request.AllowedPeerIdsJson);
                match.Entry.IsSystemSeed = false;
                match.Entry.UpdatedAtUtc = DateTime.UtcNow;
                match.Entry.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "CurrentUser" : request.UpdatedBy.Trim();
                match.Variable.ValueString = JsonSerializer.Serialize(match.Entry, JsonOptions);
                match.Variable.LastUpdated = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return match.Entry;
            }
            finally
            {
                synchronizationGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SavePolicyAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SavePolicyAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Coordinates DevExpress AI function catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="scopeFactory">Service scope factory dependency used by the DevExpress AI function catalog workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DxAiFunctionCatalogHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DxAiFunctionCatalogHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Synchronizes the DevExpress AI function catalog in the background so catalog/database work
    /// cannot prevent the local HTTP listener from coming online.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that is signaled when the host is stopping.</param>
    /// <returns>A task that completes when synchronization has finished.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1, stoppingToken).ConfigureAwait(false);

        try
        {
            var scope = scopeFactory.CreateAsyncScope();
            await using var configuredScopeAsyncDisposal = scope.ConfigureAwait(false);
            await scope.ServiceProvider.GetRequiredService<IDxAiFunctionCatalogService>()
                .SynchronizeAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "DX function catalog synchronization failed. Existing database content was left untouched and startup continues.");
        }
    }
}
