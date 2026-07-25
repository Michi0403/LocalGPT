using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IVariableStoreService
{
    Task<T> GetAsync<T>(string name, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string name, T value, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemVariable>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemVariable>> ListAllAsync(string filter, CancellationToken cancellationToken = default);
}
