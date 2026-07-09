using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces
{
    public interface IPromptConfigService
    {
        Task<string> GetPromptAsync(PromptConfigDto dto);
        Task UpdatePromptAsync(PromptConfigDto dto);
        Task<IEnumerable<PromptConfig>> ListPromptsAsync(string language);
    }
}
