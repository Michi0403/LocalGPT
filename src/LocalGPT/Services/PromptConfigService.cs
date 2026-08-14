using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates prompt config behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the prompt config workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the prompt config workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PromptConfigService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<PromptConfigService> logger) : IPromptConfigService
{
    /// <summary>
    /// Retrieves prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="language">Language value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> GetPromptAsync(string key, string language = "en", CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var prompt = await db.Prompts.AsNoTracking()
                .Where(item => item.Key == key && item.Language == language)
                .OrderByDescending(item => item.LastUpdated)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (prompt is null)
                logger.LogWarning("Prompt {PromptKey} in language {Language} was not found.", key, language);
            return prompt?.Text ?? string.Empty;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(GetPromptAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(GetPromptAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public Task<string> GetPromptAsync(PromptConfigDto dto, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(dto);
            return GetPromptAsync(dto.Key, dto.Language ?? "en", cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(GetPromptAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(GetPromptAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Updates prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task UpdatePromptAsync(PromptConfigDto dto, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.Key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(UpdatePromptAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(UpdatePromptAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Lists prompts as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="language">Language value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IEnumerable<PromptConfig>> ListPromptsAsync(string? language = null, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.Prompts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(prompt => prompt.Language == language);
            return await query.OrderBy(prompt => prompt.Key).ThenBy(prompt => prompt.Language).ToListAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(ListPromptsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PromptConfigService)}.{nameof(ListPromptsAsync)} failed.");
        throw;
    }
}
}
