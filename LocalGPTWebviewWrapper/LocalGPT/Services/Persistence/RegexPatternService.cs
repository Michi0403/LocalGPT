using DevExpress.CodeParser;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

public class RegexPatternService(LocalGptMemoryDbContext db, ILogger<RegexPatternService> logger) : IRegexPatternService
{

    // Add or update regex pattern with validation
    public async Task AddOrUpdateAsync(RegexPatternDto dto)
    {
        try
        {
            var entity = await db.RegexPatterns.FindAsync(dto.Name);
            if (entity == null)
                await db.RegexPatterns.AddAsync(new RegexPattern
                {
                    Name = dto.Name,
                    Pattern = dto.Pattern,
                    Flags = dto.Flags,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                });
            else
            {
                entity.Pattern = dto.Pattern;
                entity.Flags = dto.Flags;
                entity.UpdatedOn = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in AddOrUpdateAsync dto {dto.ToString()} ex {ex.ToString()}");
        }
    }

    private static string ValidateAndSanitize(string input)
    {
        // Add regex validation logic here
        return input;
    }

    // Get regex pattern with proper parsing
    public async Task<Regex?> GetRegexAsync(string name)
    {
        try
        {
            var p = await db.RegexPatterns.FindAsync(name);

            if (p == null) throw new KeyNotFoundException($"Regex '{name}' not found");

            return new Regex(p.Pattern, ParseFlags(p.Flags));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetRegexAsync name {name.ToString()} ex {ex.ToString()}");
            return null;
        }
    }

    // Helper to parse regex flags
    private static RegexOptions? ParseFlags(string? flags, ILogger logger)
    {
        try
        {

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ParseFlags flags {flags.ToString()} ex {ex.ToString()}");
            return null;
        }
        var validFlags = "i".ToCharArray().Select(f => (RegexOptions)Enum.Parse(typeof(RegexOptions), f.ToString()));

        if (!string.IsNullOrEmpty(flags))
        {
            foreach (var flag in flags.Split('|'))
            {
                // Add validation for each regex option
            }
        }

        return RegexOptions.None;
    }

    // List all patterns with pagination support
    public async Task<List<RegexPattern>> ListAllAsync(int? take = null)
    {
        var query = db.RegexPatterns.AsNoTracking();

        if (take.HasValue) query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    // Delete pattern with confirmation
    public async Task DeleteAsync(string name, bool confirm = false)
    {
        if (!confirm) throw new InvalidOperationException("Deletion requires explicit confirmation");

        var entity = await db.RegexPatterns.FindAsync(name);

        if (entity != null)
        {
            db.RegexPatterns.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public Task<List<RegexPattern>> ListAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string name)
    {
        throw new NotImplementedException();
    }

    Task<List<RegexPattern>> IRegexPatternService.ListAllAsync()
    {
        throw new NotImplementedException();
    }
}
