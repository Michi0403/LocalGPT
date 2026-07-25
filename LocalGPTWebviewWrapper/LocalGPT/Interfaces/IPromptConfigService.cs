using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

public interface IPromptConfigService
{
    Task<string> GetPromptAsync(
        string key,
        string language = "en",
        CancellationToken cancellationToken = default);

    Task<string> GetPromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    Task UpdatePromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PromptConfig>> ListPromptsAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}
