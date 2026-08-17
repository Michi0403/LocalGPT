using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LocalGPT.Services;

/// <summary>Centralizes JSON-to-text serialization so scope, diagnostics and serialization policy remain service-owned.</summary>
/// <param name="logger">Logger used for serialization diagnostics.</param>
public sealed class JsonTextService(ILogger<JsonTextService> logger) : IJsonTextService
{
    /// <summary>
    /// Performs serialize as part of the JSON text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string Serialize(object? value, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Serialize(value, options ?? CreateDefaultOptions());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Serializing application data to JSON text failed; serialized content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs escape string value as part of the JSON text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string EscapeStringValue(string? value)
    {
        try
        {
            return JsonEncodedText.Encode(value ?? string.Empty).ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Escaping a JSON string value failed; string content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs serialize node as part of the JSON text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string SerializeNode(JsonNode? node, JsonSerializerOptions? options = null)
    {
        try
        {
            return node?.ToJsonString(options) ?? "null";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Serializing a JSON node to text failed; node content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Creates default options as part of the JSON text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The JSON serializer options produced by the operation.</returns>
    private JsonSerializerOptions CreateDefaultOptions()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = null,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the default JSON serialization options failed.");
            throw;
        }
    }
}
