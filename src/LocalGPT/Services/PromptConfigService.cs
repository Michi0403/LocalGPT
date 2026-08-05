using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class PromptConfigService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<PromptConfigService> logger) : IPromptConfigService
{
    public async Task<string> GetPromptAsync(string key, string language = "en", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var prompt = await db.Prompts.AsNoTracking()
            .Where(item => item.Key == key && item.Language == language)
            .OrderByDescending(item => item.LastUpdated)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (prompt is null)
            logger.LogWarning("Prompt {PromptKey} in language {Language} was not found.", key, language);
        return prompt?.Text ?? string.Empty;
    }

    public Task<string> GetPromptAsync(PromptConfigDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return GetPromptAsync(dto.Key, dto.Language ?? "en", cancellationToken);
    }

    public async Task UpdatePromptAsync(PromptConfigDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Key);
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await db.Prompts
            .Where(item => item.Key == dto.Key && item.Language == dto.Language)
            .OrderByDescending(item => item.LastUpdated)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new PromptConfig { Key = dto.Key, Language = dto.Language };
            db.Prompts.Add(entity);
        }
        entity.Text = dto.Text;
        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PromptConfig>> ListPromptsAsync(string? language = null, CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Prompts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(prompt => prompt.Language == language);
        return await query.OrderBy(prompt => prompt.Key).ThenBy(prompt => prompt.Language).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
