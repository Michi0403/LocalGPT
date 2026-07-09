using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LocalGPT.Services.Migration;

public class DataMigrationService(ILogger<DataMigrationService> logger, IServiceProvider provider, IRegexPatternService regexSvc, IPromptConfigService promptSvc, IVariableStoreService varSvc) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await MigrateRegexPatterns(stoppingToken);
            await MigrateSystemPrompts(stoppingToken);
            await MigrateGlobalVariables(stoppingToken);
            logger.LogInformation("Migration completed successfully at {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Log error and rollback changes if necessary
            logger.LogError(ex, $"Error in ExecuteAsync {ex.ToString()}");
        }
    }

    private async Task MigrateRegexPatterns(CancellationToken cancellationToken)
    {
        try
        {
            var regexFields = typeof(CouncilChatStringFunctions)
     .GetFields(BindingFlags.Static | BindingFlags.Public)
     .Where(f => f.FieldType == typeof(string) &&
                (f.Name.Contains("Pattern") || f.Name.EndsWith("Pattern")));

            foreach (var field in regexFields)
            {
                var patternName = field.Name.Replace("Pattern", "").Trim();
                var patternValue = field.GetValue(null)?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(patternValue))
                {
                    await regexSvc.AddOrUpdateAsync(new( patternName, patternValue,null));

                    logger.LogInformation($"Migrated regex '{patternName}' from static to database");
                }
            }
        }
        catch (Exception ex)
        {
            // Log error and rollback changes if necessary
            logger.LogError(ex, $"Error in MigrateRegexPatterns {ex.ToString()}");
        }
 
    }

    private async Task MigrateSystemPrompts(CancellationToken cancellationToken)
    {
        try
        {
            var promptFields = typeof(CouncilChatStaticsGeneral).GetFields(BindingFlags.Static | BindingFlags.Public)
           .Where(f => f.FieldType == typeof(string) && (f.Name.Contains("Prompt") || f.Name.Contains("Message")));

            foreach (var field in promptFields)
            {
                var text = (string?)field.GetValue(null);
                if (!string.IsNullOrEmpty(text))
                {
                    await promptSvc.UpdatePromptAsync(new PromptConfigDto(field.Name, "en",  text));
                }
            }
        }
        catch (Exception ex)
        {
            // Log error and rollback changes if necessary
            logger.LogError(ex, $"Error in MigrateSystemPrompts {ex.ToString()}");
        }
       
    }
    public async Task MigrateAllStaticDataAsync()
    {
        await MigrateRegexPatternsAsync();
        await MigrateSystemPromptsAsync();
        await MigrateGlobalVariablesAsync();
    }

    private async Task MigrateRegexPatternsAsync()
    {
        // Get all static regex fields from CouncilChatStringFunctions
        var regexFields = typeof(CouncilChatStringFunctions)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(string) &&
                       (f.Name.EndsWith("Pattern") || f.Name.Contains("Regex")));

        foreach (var field in regexFields)
        {
            try
            {
                var pattern = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(pattern))
                {
                    await regexSvc.AddOrUpdateAsync(new RegexPatternDto
                    (
                       field.Name.Replace("Pattern", "").Replace("Regex", ""),
                       pattern,
                        "i" // Default case-insensitive
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to migrate regex field {field.Name}: {ex.Message}");
            }
        }
    }

    private async Task MigrateSystemPromptsAsync()
    {
        // Get all static prompt fields from CouncilChatStaticsGeneral
        var promptFields = typeof(CouncilChatStaticsGeneral)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(string) &&
                       (f.Name.Contains("Prompt") || f.Name.Contains("Message") || f.Name.Contains("Help")));

        foreach (var field in promptFields)
        {
            try
            {
                var text = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(text))
                {
                    await promptSvc.UpdatePromptAsync(new PromptConfigDto(
                    
                        field.Name,
                       "en",
                       text
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to migrate prompt field {field.Name}: {ex.Message}");
            }
        }
    }

    private async Task MigrateGlobalVariablesAsync()
    {
        // Get all static fields from GlobalVariableSlopCollectionToRemove
        var variableFields = typeof(GlobalVariableSlopCollectionToRemove)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => !f.Name.StartsWith("_")); // Skip private fields

        foreach (var field in variableFields)
        {
            try
            {
                var value = field.GetValue(null);
                if (value != null)
                {
                    await _varSvc.SetAsync(field.Name, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to migrate variable field {field.Name}: {ex.Message}");
            }
        }
    }

    private async Task MigrateGlobalVariables(CancellationToken cancellationToken)
    {
        try
        {
            var variableFields = typeof(GlobalVariableSlopCollectionToRemove).GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => !f.Name.StartsWith("_")); // Skip private fields

            foreach (var field in variableFields)
            {
                var value = field.GetValue(null);
                if (value != null)
                {
                    await varSvc.SetAsync(field.Name, value);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error and rollback changes if necessary
            logger.LogError(ex, $"Error in MigrateGlobalVariables {ex.ToString()}");
        }
    }
}
