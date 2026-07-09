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
                await regexSvc.AddOrUpdateAsync(patternName, patternValue);

                logger.LogInformation($"Migrated regex '{patternName}' from static to database");
            }
        }
    }

    private async Task MigrateSystemPrompts(CancellationToken cancellationToken)
    {
        var promptFields = typeof(CouncilChatStaticsGeneral).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(string) && (f.Name.Contains("Prompt") || f.Name.Contains("Message")));

        foreach (var field in promptFields)
        {
            var text = (string?)field.GetValue(null);
            if (!string.IsNullOrEmpty(text))
            {
                await promptSvc.UpdatePromptAsync(new PromptConfigDto { Key = field.Name, Language = "en", Text = text });
            }
        }
    }

    private async Task MigrateGlobalVariables(CancellationToken cancellationToken)
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

    // Migration methods omitted for brevity
}
