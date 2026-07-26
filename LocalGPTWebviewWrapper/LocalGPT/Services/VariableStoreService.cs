using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class VariableStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<VariableStoreService> logger,
    SqliteUtilityService sqliteUtility) : IVariableStoreService
{
    public async Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var variable = await db.SystemVariables.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Name == name, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Variable '{name}' was not found.");
        return sqliteUtility.ParseValue<T>(variable.ValueString, variable.DataType, logger);
    }

    public async Task SetAsync<T>(string name, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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

    public Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default) =>
        ListAllAsync(string.Empty, cancellationToken);

    public async Task<IEnumerable<SystemVariable>> ListAllAsync(string filter, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.SystemVariables.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(item => item.Name.Contains(filter) ||
                item.ValueString.Contains(filter) ||
                (item.DataType != null && item.DataType.Contains(filter)));
        }
        return await query.OrderBy(item => item.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
