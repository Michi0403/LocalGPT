using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates council DevExpress function policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="databaseInitialization">Database initialization service dependency used by the council DevExpress function policy workflow to provide the corresponding application capability.</param>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the council DevExpress function policy workflow to provide the corresponding application capability.</param>
/// <param name="definitions">System variable definition service dependency used by the council DevExpress function policy workflow to provide the corresponding application capability.</param>
/// <param name="prompts">Prompt config service dependency used by the council DevExpress function policy workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilDxFunctionPolicyDataService(
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ISystemVariableDefinitionService definitions,
    IPromptConfigService prompts,
    ILogger<CouncilDxFunctionPolicyDataService> logger) : ICouncilDxFunctionPolicyDataService
{
    /// <summary>
    /// Retrieves policy as part of the council DevExpress function policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council DevExpress function policy produced by the operation.</returns>
    public async Task<CouncilDxFunctionPolicy> GetPolicyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var requestedNames = new[]
            {
                definitions.CouncilDxMaximumCallsPerStep.Name,
                definitions.CouncilDxMaximumParameterCharacters.Name,
                definitions.CouncilDxMaximumResultCharacters.Name
            };
            var values = await db.SystemVariables.AsNoTracking()
                .Where(item => requestedNames.Contains(item.Name))
                .ToDictionaryAsync(item => item.Name, item => item.ValueString, StringComparer.OrdinalIgnoreCase, cancellationToken)
                .ConfigureAwait(false);
            var prompt = await prompts.GetPromptAsync(nameof(CouncilDxFunctionPolicy), cancellationToken: cancellationToken).ConfigureAwait(false);
            var policy = new CouncilDxFunctionPolicy
            {
                MaximumCallsPerStep = ParsePositive(values, definitions.CouncilDxMaximumCallsPerStep),
                MaximumParameterCharacters = ParsePositive(values, definitions.CouncilDxMaximumParameterCharacters),
                MaximumResultCharacters = ParsePositive(values, definitions.CouncilDxMaximumResultCharacters),
                PromptInstruction = prompt
            };
            logger.LogDebug($"Loaded Council DX function policy from database-backed system variables and prompts; values omitted from logs.");
            return policy;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Council DX function policy could not be loaded from its database data service.");
            throw;
        }
    }

    /// <summary>
    /// Parses positive as part of the council DevExpress function policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">String dependency used by the council DevExpress function policy workflow to provide the corresponding application capability.</param>
    /// <param name="definition">Definition value supplied to the council DevExpress function policy operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ParsePositive(
        IReadOnlyDictionary<string, string> values,
        SystemVariableDefinition<int> definition)
    {
        try
        {
            if (values.TryGetValue(definition.Name, out var raw) &&
                int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
                parsed > 0)
            {
                logger.LogDebug("Resolved Council DX policy variable {VariableName}; value omitted from logs.", definition.Name);
                return parsed;
            }

            logger.LogWarning(
                "Council DX policy variable {VariableName} is missing or invalid. Using the declared default.",
                definition.Name);
            return definition.DefaultValue;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Council DX policy variable {definition.Name} could not be parsed.");
            throw;
        }
    }
}
