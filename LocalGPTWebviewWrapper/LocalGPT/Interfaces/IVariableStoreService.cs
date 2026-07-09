using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IVariableStoreService
    {
        Task<T> GetAsync<T>(string name);
        Task SetAsync<T>(string name, T value);
        Task<IEnumerable<SystemVariable>> ListAllAsync(string filter);
    }
}
