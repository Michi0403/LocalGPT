using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Reads ruleset row parameters. Parsed documents are cached by their JSON text because rulesets are immutable while a session runs.
/// </summary>
/// <param name="logger">Receives parse diagnostics.</param>
public sealed class GameParameterReader(ILogger<GameParameterReader> logger) : IGameParameterReader
{
    /// <summary>
    /// Parsed parameter objects keyed by their JSON text.
    /// </summary>
    private readonly ConcurrentDictionary<string, JsonElement> cache = new(StringComparer.Ordinal);

    /// <summary>
    /// The shared empty parameter object.
    /// </summary>
    private readonly JsonElement emptyObject = JsonSerializer.Deserialize<JsonElement>("{}");

    /// <inheritdoc />
    public JsonElement Parse(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return emptyObject;
            return cache.GetOrAdd(json, ParseCore);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing game parameters failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public int ReadInt(JsonElement parameters, string name, int fallback)
    {
        try
        {
            if (!TryRead(parameters, name, out var value))
                return fallback;
            if (value.ValueKind == JsonValueKind.Number)
                return value.TryGetInt32(out var number) ? number : (int)Math.Round(value.GetDouble());
            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading integer game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public double ReadDouble(JsonElement parameters, string name, double fallback)
    {
        try
        {
            if (!TryRead(parameters, name, out var value))
                return fallback;
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetDouble();
            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading numeric game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public string ReadString(JsonElement parameters, string name, string fallback)
    {
        try
        {
            if (!TryRead(parameters, name, out var value))
                return fallback;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? fallback,
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
                _ => fallback
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading text game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public bool ReadBool(JsonElement parameters, string name, bool fallback)
    {
        try
        {
            if (!TryRead(parameters, name, out var value))
                return fallback;
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => fallback
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading boolean game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ReadStrings(JsonElement parameters, string name)
    {
        try
        {
            if (!TryRead(parameters, name, out var value))
                return [];
            if (value.ValueKind == JsonValueKind.String)
                return [value.GetString() ?? string.Empty];
            if (value.ValueKind != JsonValueKind.Array)
                return [];
            var values = new List<string>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    values.Add(item.GetString() ?? string.Empty);
                else if (item.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    values.Add(item.GetRawText());
            }
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading text list game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<int> ReadInts(JsonElement parameters, string name)
    {
        try
        {
            if (!TryRead(parameters, name, out var value) || value.ValueKind != JsonValueKind.Array)
                return [];
            var values = new List<int>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                    values.Add(item.TryGetInt32(out var number) ? number : (int)Math.Round(item.GetDouble()));
            }
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading integer list game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public JsonElement ReadObject(JsonElement parameters, string name)
    {
        try
        {
            return TryRead(parameters, name, out var value) && value.ValueKind == JsonValueKind.Object
                ? value
                : emptyObject;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading object game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<JsonElement> ReadObjects(JsonElement parameters, string name)
    {
        try
        {
            if (!TryRead(parameters, name, out var value) || value.ValueKind != JsonValueKind.Array)
                return [];
            return value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading object list game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryRead(JsonElement parameters, string name, out JsonElement value)
    {
        try
        {
            value = default;
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out value) &&
                value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading game parameter {ParameterName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Parses one parameter document into a detached object element.
    /// </summary>
    /// <param name="json">The parameter JSON.</param>
    /// <returns>The object element, or the empty object when the JSON is malformed or not an object.</returns>
    private JsonElement ParseCore(string json)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            if (element.ValueKind == JsonValueKind.Object)
                return element;
            logger.LogWarning("Game parameters are not a JSON object and were ignored: {ParameterJson}", json.Length > 160 ? json[..160] : json);
            return emptyObject;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Game parameters are malformed JSON and were ignored.");
            return emptyObject;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing game parameter document failed.");
            throw;
        }
    }
}
