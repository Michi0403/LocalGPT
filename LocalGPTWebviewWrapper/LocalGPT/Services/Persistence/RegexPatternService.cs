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
            var entity = await db.RegexPatterns.SingleOrDefaultAsync(x => x.Name == dto.Name);
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
    //Todo

    private string ValidateAndSanitize(string input)
    {
        // Add regex validation logic here
        return input;
    }

    // Get regex pattern with proper parsing
    public async Task<Regex?> GetRegexAsync(string name)
    {
        try
        {
            var p = await db.RegexPatterns.SingleOrDefaultAsync(x => x.Name == name);

            if (p == null) throw new KeyNotFoundException($"Regex '{name}' not found");
            var flags = ParseFlags(p.Flags, logger);
            if (flags is null)
                throw new InvalidOperationException($"Regex '{name}' has invalid flags '{p.Flags}'.");
            return new Regex(p.Pattern, flags.Value, TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in GetRegexAsync name {name.ToString()} ex {ex.ToString()}");
            return null;
        }
    }

    // Helper to parse regex flags
 

    // List all patterns with pagination support
    public async Task<List<RegexPattern>> ListAllAsync(int? take = null)
    {
        try
        {
            var query = db.RegexPatterns.AsNoTracking();

            if (take.HasValue) query = query.Take(take.Value);

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ListAllAsync take {take.ToString()} ex {ex.ToString()}");
            return new();
        }
   
    }

    // Delete pattern with confirmation
    public async Task DeleteAsync(string name, bool confirm = false)
    {
        try
        {
            if (!confirm) throw new InvalidOperationException("Deletion requires explicit confirmation");

            var entity = await db.RegexPatterns.SingleOrDefaultAsync(x => x.Name == name);

            if (entity != null)
            {
                db.RegexPatterns.Remove(entity);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DeleteAsync name {name.ToString()} confirm {confirm.ToString()} ex {ex.ToString()}");
        }
     
    }

    public async Task<List<RegexPattern>> ListAllAsync()
    {
        try
        {
           return await db.RegexPatterns.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in ListAllAsync ex {ex.ToString()}");
            throw;
        }
    }

    public async Task DeleteAsync(string name)
    {
        try
        {
            var entity = await db.RegexPatterns.SingleOrDefaultAsync(x => x.Name == name);
            if (entity != null)
            {
                db.RegexPatterns.Remove(entity);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in DeleteAsync name {name.ToString()} ex {ex.ToString()}");
        }
    }



    private RegexOptions? ParseFlags(string? flags, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flags))
                return RegexOptions.None;

            var result = RegexOptions.None;
            foreach (var token in flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result |= token.ToLowerInvariant() switch
                {
                    "i" => RegexOptions.IgnoreCase,
                    "m" => RegexOptions.Multiline,
                    "s" => RegexOptions.Singleline,
                    "x" => RegexOptions.IgnorePatternWhitespace,
                    "n" => RegexOptions.ExplicitCapture,
                    "compiled" => RegexOptions.Compiled,
                    "cultureinvariant" => RegexOptions.CultureInvariant,
                    "ecmascript" => RegexOptions.ECMAScript,
                    "ignorecase" => RegexOptions.IgnoreCase,
                    "multiline" => RegexOptions.Multiline,
                    "singleline" => RegexOptions.Singleline,
                    "ignorepatternwhitespace" => RegexOptions.IgnorePatternWhitespace,
                    "explicitcapture" => RegexOptions.ExplicitCapture,
                    "none" => RegexOptions.None,
                    _ when Enum.TryParse<RegexOptions>(token, ignoreCase: true, out var parsed) => parsed,
                    _ => throw new ArgumentException($"Unknown regular-expression option '{token}'.", nameof(flags))
                };
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not parse regular-expression flags {Flags}.", flags);
            return null;
        }
    }
}
