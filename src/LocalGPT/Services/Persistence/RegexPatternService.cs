using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates regex pattern behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the regex pattern workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the regex pattern workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RegexPatternService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<RegexPatternService> logger) : IRegexPatternService
{
    /// <summary>
    /// Adds or update as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task AddOrUpdateAsync(RegexPatternDto dto)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);
            ValidatePattern(dto.Pattern, dto.Flags);
            await databaseInitializer.InitializeAsync().ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var entity = await db.RegexPatterns.SingleOrDefaultAsync(item => item.Name == dto.Name).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new RegexPattern { Name = dto.Name, CreatedOn = DateTime.UtcNow };
                db.RegexPatterns.Add(entity);
            }
            entity.Pattern = dto.Pattern;
            entity.Flags = dto.Flags;
            entity.UpdatedOn = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Saved regex pattern {RegexName}; pattern content omitted from logs.", dto.Name);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a regex pattern failed; pattern content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves regex as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    public async Task<Regex?> GetRegexAsync(string name)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            await databaseInitializer.InitializeAsync().ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var pattern = await db.RegexPatterns.AsNoTracking().SingleOrDefaultAsync(item => item.Name == name).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Regex '{name}' was not found.");
            return Compile(pattern.Pattern, pattern.Flags);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading regex pattern {RegexName} failed; pattern content was omitted from logs.", name);
            throw;
        }
    }

    /// <summary>
    /// Performs compile as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    public Regex Compile(string pattern, string? flags = null)
    {
        try
        {
            return Compile(pattern, flags, TimeSpan.FromSeconds(2));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regex compilation failed; pattern content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs compile as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    public Regex Compile(string pattern, string? flags, TimeSpan timeout)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(pattern);
            if (pattern.Length > 16_000)
                throw new ArgumentException("Regex patterns are limited to 16,000 characters.", nameof(pattern));
            var boundedTimeout = timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30)
                ? TimeSpan.FromSeconds(2)
                : timeout;
            return new Regex(pattern, ParseFlags(flags), boundedTimeout);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regex compilation with an explicit timeout failed; pattern content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Lists all as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<List<RegexPattern>> ListAllAsync()
    {
        try
        {
            return await ListAllAsync(null).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing regex patterns failed.");
            throw;
        }
    }

    /// <summary>
    /// Lists all as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<List<RegexPattern>> ListAllAsync(int? take = null)
    {
        try
        {
            await databaseInitializer.InitializeAsync().ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.RegexPatterns.AsNoTracking().OrderBy(item => item.Name).AsQueryable();
            if (take.HasValue)
                query = query.Take(Math.Clamp(take.Value, 1, 1000));
            return await query.ToListAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing regex patterns with a bounded row count failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DeleteAsync(string name)
    {
        try
        {
            await DeleteAsync(name, confirm: false).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting regex pattern {RegexName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="confirm">Value indicating whether confirm should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DeleteAsync(string name, bool confirm = false)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!confirm)
                throw new InvalidOperationException("Deletion requires explicit confirmation.");
            await databaseInitializer.InitializeAsync().ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var entity = await db.RegexPatterns.SingleOrDefaultAsync(item => item.Name == name).ConfigureAwait(false);
            if (entity is null)
                return;
            db.RegexPatterns.Remove(entity);
            await db.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Deleted regex pattern {RegexName}; pattern content was omitted from logs.", name);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Confirmed deletion of regex pattern {RegexName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Validates pattern as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    private void ValidatePattern(string pattern, string? flags)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(pattern);
            if (pattern.Length > 16_000)
                throw new ArgumentException("Regex patterns are limited to 16,000 characters.", nameof(pattern));
            _ = Compile(pattern, flags, TimeSpan.FromSeconds(2));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RegexPatternService)}.{nameof(ValidatePattern)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RegexPatternService)}.{nameof(ValidatePattern)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses flags as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex options produced by the operation.</returns>
    private RegexOptions ParseFlags(string? flags)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(flags))
                return RegexOptions.CultureInvariant;
            var result = RegexOptions.CultureInvariant;
            foreach (var token in flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result |= token.ToLowerInvariant() switch
                {
                    "i" or "ignorecase" => RegexOptions.IgnoreCase,
                    "m" or "multiline" => RegexOptions.Multiline,
                    "s" or "singleline" => RegexOptions.Singleline,
                    "x" or "ignorepatternwhitespace" => RegexOptions.IgnorePatternWhitespace,
                    "n" or "explicitcapture" => RegexOptions.ExplicitCapture,
                    "compiled" => RegexOptions.Compiled,
                    "c" or "cultureinvariant" => RegexOptions.CultureInvariant,
                    "ecmascript" => RegexOptions.ECMAScript,
                    "none" => RegexOptions.None,
                    _ when Enum.TryParse<RegexOptions>(token, true, out var parsed) => parsed,
                    _ => throw new ArgumentException($"Unknown regular-expression option '{token}'.", nameof(flags))
                };
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RegexPatternService)}.{nameof(ParseFlags)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RegexPatternService)}.{nameof(ParseFlags)} failed.");
        throw;
    }
}
}
