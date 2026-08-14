using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates variable store behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the variable store workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the variable store workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="sqliteUtility">Sqlite utility service dependency used by the variable store workflow to provide the corresponding application capability.</param>
public sealed class VariableStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<VariableStoreService> logger,
    SqliteUtilityService sqliteUtility) : IVariableStoreService
{
    /// <summary>
    /// Performs get as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="VariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The t produced by the operation.</returns>
    public async Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
        var variable = await db.SystemVariables.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Name == name, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Variable '{name}' was not found.");
        return sqliteUtility.ParseValue<T>(variable.ValueString, variable.DataType, logger);
    }

    /// <summary>
    /// Performs set as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="VariableStoreService"/>.</typeparam>
    /// <param name="name">Name value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task SetAsync<T>(string name, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
        var existing = await db.SystemVariables.SingleOrDefaultAsync(item => item.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new SystemVariable { Name = name };
            db.SystemVariables.Add(existing);
        }
        existing.ValueString = value?.ToString() ?? string.Empty;
        existing.DataType = typeof(T).FullName;
        existing.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Stored system variable {VariableName}; value omitted from logs.", name);
    }

    /// <summary>
    /// Lists all as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default) {
    try
    {
        return ListAllAsync(string.Empty, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(VariableStoreService)}.{nameof(ListAllAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(VariableStoreService)}.{nameof(ListAllAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Lists all as part of the variable store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filter">Filter value supplied to the variable store operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IEnumerable<SystemVariable>> ListAllAsync(string filter, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.SystemVariables.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(item => item.Name.Contains(filter) ||
                    item.ValueString.Contains(filter) ||
                    (item.DataType != null && item.DataType.Contains(filter)));
            }
            return await query.OrderBy(item => item.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(VariableStoreService)}.{nameof(ListAllAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(VariableStoreService)}.{nameof(ListAllAsync)} failed.");
        throw;
    }
}
}
