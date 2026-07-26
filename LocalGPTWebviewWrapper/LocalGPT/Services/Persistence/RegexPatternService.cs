using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

public sealed class RegexPatternService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<RegexPatternService> logger) : IRegexPatternService
{
    public async Task AddOrUpdateAsync(RegexPatternDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);
        ValidatePattern(dto.Pattern, dto.Flags);
        await databaseInitializer.InitializeAsync().ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
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

    public async Task<Regex?> GetRegexAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await databaseInitializer.InitializeAsync().ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var pattern = await db.RegexPatterns.AsNoTracking().SingleOrDefaultAsync(item => item.Name == name).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Regex '{name}' was not found.");
        return new Regex(pattern.Pattern, ParseFlags(pattern.Flags), TimeSpan.FromSeconds(2));
    }

    public Task<List<RegexPattern>> ListAllAsync() => ListAllAsync(null);

    public async Task<List<RegexPattern>> ListAllAsync(int? take = null)
    {
        await databaseInitializer.InitializeAsync().ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = db.RegexPatterns.AsNoTracking().OrderBy(item => item.Name).AsQueryable();
        if (take.HasValue)
            query = query.Take(Math.Clamp(take.Value, 1, 1000));
        return await query.ToListAsync().ConfigureAwait(false);
    }

    public Task DeleteAsync(string name) => DeleteAsync(name, confirm: false);

    public async Task DeleteAsync(string name, bool confirm = false)
    {
        if (!confirm)
            throw new InvalidOperationException("Deletion requires explicit confirmation.");
        await databaseInitializer.InitializeAsync().ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var entity = await db.RegexPatterns.SingleOrDefaultAsync(item => item.Name == name).ConfigureAwait(false);
        if (entity is null)
            return;
        db.RegexPatterns.Remove(entity);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void ValidatePattern(string pattern, string? flags)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length > 16_000)
            throw new ArgumentException("Regex patterns are limited to 16,000 characters.", nameof(pattern));
        _ = new Regex(pattern, ParseFlags(flags), TimeSpan.FromSeconds(2));
    }

    private static RegexOptions ParseFlags(string? flags)
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
                "ecmascript" => RegexOptions.ECMAScript,
                "none" => RegexOptions.None,
                _ when Enum.TryParse<RegexOptions>(token, true, out var parsed) => parsed,
                _ => throw new ArgumentException($"Unknown regular-expression option '{token}'.", nameof(flags))
            };
        }
        return result;
    }
}
