using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class PromptConfigService(
    LocalGptMemoryDbContext db,
    ILogger<PromptConfigService> logger) : IPromptConfigService
{
    public async Task<string> GetPromptAsync(
        string key,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A prompt key is required.", nameof(key));

        try
        {
            var prompt = await db.Prompts
                .AsNoTracking()
                .Where(item => item.Key == key && item.Language == language)
                .OrderByDescending(item => item.LastUpdated)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (prompt is null)
                logger.LogWarning("Prompt {PromptKey} in language {Language} was not found.", key, language);

            return prompt?.Text ?? string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load prompt {PromptKey} in language {Language}.", key, language);
            return string.Empty;
        }
    }

    public Task<string> GetPromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return GetPromptAsync(dto.Key, dto.Language ?? "en", cancellationToken);
    }

    public async Task UpdatePromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        try
        {
            var entity = await db.Prompts
                .Where(item => item.Key == dto.Key && item.Language == dto.Language)
                .OrderByDescending(item => item.LastUpdated)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                await db.Prompts.AddAsync(new PromptConfig
                {
                    Key = dto.Key,
                    Language = dto.Language,
                    Text = dto.Text,
                    LastUpdated = DateTime.UtcNow
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                entity.Text = dto.Text;
                entity.LastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not update prompt {PromptKey} in language {Language}.", dto.Key, dto.Language);
            throw;
        }
    }

    public async Task<IEnumerable<PromptConfig>> ListPromptsAsync(
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = db.Prompts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(prompt => prompt.Language == language);

            return await query
                .OrderBy(prompt => prompt.Key)
                .ThenBy(prompt => prompt.Language)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list prompts for language {Language}.", language);
            return [];
        }
    }
}
