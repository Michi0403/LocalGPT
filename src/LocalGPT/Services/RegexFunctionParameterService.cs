using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Provides regex function parameter service operations.
/// </summary>
public sealed class RegexFunctionParameterService(ILogger<RegexFunctionParameterService> logger) : IRegexFunctionParameterService
{
    /// <summary>
    /// Gets required string.
    /// </summary>
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
