using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces
{
    public interface IPromptConfigService
    {
        Task<string> GetPromptAsync(string key, string language);
        Task UpdatePromptAsync(PromptConfigDto dto);
        IEnumerable<PromptConfig> ListPrompts(string language);
    }
}
