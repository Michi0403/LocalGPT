using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates regex function parameter behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RegexFunctionParameterService(ILogger<RegexFunctionParameterService> logger) : IRegexFunctionParameterService
{
    /// <summary>
    /// Retrieves required string as part of the regex function parameter service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the regex function parameter operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the regex function parameter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetRequiredString(JsonElement element, string propertyName)
    {
        try
        {
            logger.LogTrace($"Reading required regex-function parameter {propertyName}.");
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var value)
                || value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new ArgumentException($"'{propertyName}' is required.");
            }

            return value.GetString()!.Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read required regex-function parameter {propertyName}: {exception.Message}");
            throw;
        }
    }
}
