using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    public class VariableStoreService(LocalGptMemoryDbContext db, ILogger<VariableStoreService> logger) : IVariableStoreService
    {
        public async Task<T> GetAsync<T>(string name)
        {
            try
            {
                var v = await db.SystemVariables.FindAsync(name);
                if (v == null) throw new KeyNotFoundException($"Variable '{name}' not found");
                return SQLLiteFunctions.ParseValue<T>(v.ValueString, v.DataType, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetAsync name {name} ex {ex.ToString()}");
                throw;
            }
        }

        public async Task SetAsync<T>(string name, T value)
        {
            try
            {
                var existing = await db.SystemVariables.FindAsync(name);
                if (existing == null)
                    await db.SystemVariables.AddAsync(new SystemVariable
                    {
                        Name = name,
                        ValueString = value?.ToString() ?? string.Empty,
                        DataType = typeof(T).FullName,
                        LastUpdated = DateTime.UtcNow
                    });
                else
                {
                    existing.ValueString = value?.ToString() ?? string.Empty;
                    existing.DataType = typeof(T).FullName;
                    existing.LastUpdated = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SetAsync name {name} value {value?.ToString()} ex {ex.ToString()}");
            }
        }

        public async Task<IEnumerable<SystemVariable>> ListAllAsync()
        {
            try
            {
                return await db.SystemVariables.ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListAllAsync ex {ex.ToString()}");
                return await Task.FromResult<IEnumerable<SystemVariable>>(
                      Enumerable.Empty<SystemVariable>()
                  );
            }
        }

        public async Task<IEnumerable<SystemVariable>> ListAllAsync(string filter)
        {
            try
            {
                return await db.SystemVariables.Where(x => x.Id.ToString() == filter || x.Name.Contains(filter) || x.ValueString.Contains(filter) || ( x.DataType != null && x.DataType.Contains(filter) )).ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListAllAsync filter {filter} ex {ex.ToString()}");
                return await Task.FromResult<IEnumerable<SystemVariable>>(
                       Enumerable.Empty<SystemVariable>()
                   );
            }
        }
    }
}