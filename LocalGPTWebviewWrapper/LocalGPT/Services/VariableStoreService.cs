using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class VariableStoreService(
    LocalGptMemoryDbContext db,
    ILogger<VariableStoreService> logger,
        SqliteUtilityService sqliteUtility) : IVariableStoreService
{
    public async Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variable name is required.", nameof(name));

        try
        {
            var variable = await db.SystemVariables
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Name == name, cancellationToken)
                .ConfigureAwait(false);
            if (variable is null)
                throw new KeyNotFoundException($"Variable '{name}' was not found.");

            return sqliteUtility.ParseValue<T>(variable.ValueString, variable.DataType, logger);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load system variable {VariableName}.", name);
            throw;
        }
    }

    public async Task SetAsync<T>(
        string name,
        T value,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variable name is required.", nameof(name));

        try
        {
            var existing = await db.SystemVariables
                .SingleOrDefaultAsync(item => item.Name == name, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                await db.SystemVariables.AddAsync(new SystemVariable
                {
                    Name = name,
                    ValueString = value?.ToString() ?? string.Empty,
                    DataType = typeof(T).FullName,
                    LastUpdated = DateTime.UtcNow
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.ValueString = value?.ToString() ?? string.Empty;
                existing.DataType = typeof(T).FullName;
                existing.LastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not store system variable {VariableName}.", name);
            throw;
        }
    }

    public Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default) =>
        ListAllAsync(string.Empty, cancellationToken);

    public async Task<IEnumerable<SystemVariable>> ListAllAsync(
        string filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = db.SystemVariables.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(item =>
                    item.Name.Contains(filter) ||
                    item.ValueString.Contains(filter) ||
                    (item.DataType != null && item.DataType.Contains(filter)));
            }

            return await query
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list system variables for filter {Filter}.", filter);
            return [];
        }
    }
}
