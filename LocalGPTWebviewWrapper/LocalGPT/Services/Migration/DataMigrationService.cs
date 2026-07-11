using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LocalGPT.Services.Migration;

public class DataMigrationService(IServiceProvider serviceProvider, ILogger<DataMigrationService> logger  /*IRegexPatternService regexSvc, IPromptConfigService promptSvc, IVariableStoreService varSvc*/) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
                try
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        logger.LogInformation($"Starting DataMigrationService Routine Giving all 30 sec to boot");
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
                        var myRegExService = scope.ServiceProvider.GetRequiredService<IRegexPatternService>();
                        await MigrateRegexPatterns(stoppingToken, myRegExService);
                        var myPromptConfigService = scope.ServiceProvider.GetRequiredService<IPromptConfigService>();
                        await MigrateSystemPrompts(stoppingToken, myPromptConfigService);
                        var myVariableStoreService = scope.ServiceProvider.GetRequiredService<IVariableStoreService>();
                        await MigrateGlobalVariables(stoppingToken, myVariableStoreService);
                    }
                }
                catch (OperationCanceledException ex)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception during duplicate check");
                    throw;
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
            //}
            logger.LogInformation("Migration completed successfully at {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Log error and rollback changes if necessary
            logger.LogError(ex, $"Error in ExecuteAsync {ex.ToString()}");
        }
    }

    private async Task MigrateRegexPatterns(CancellationToken cancellationToken, IRegexPatternService regexSvc)
    {
        try
        {
            var regexFields = typeof(CouncilChatStringFunctions)
     .GetFields(BindingFlags.Static | BindingFlags.Public)
     .Where(f => f.FieldType == typeof(string) &&
                (f.Name.Contains("Pattern") || f.Name.EndsWith("Pattern") || f.Name.Contains("Regex")));

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

    private async Task MigrateSystemPrompts(CancellationToken cancellationToken, IPromptConfigService promptSvc)
    {
        try
        {
            var promptFields = typeof(CouncilChatStaticsGeneral).GetFields(BindingFlags.Static | BindingFlags.Public)
           .Where(f => f.FieldType == typeof(string) && (f.Name.Contains("Prompt") || f.Name.Contains("Message") || f.Name.Contains("Help")));

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
    private async Task MigrateGlobalVariables(CancellationToken cancellationToken, IVariableStoreService varSvc)
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
