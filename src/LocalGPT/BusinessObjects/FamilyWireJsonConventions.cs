using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Keeps LocalGPT-family JSON wire enum and property-reading conventions consistent across
/// products without replacing MVC, SignalR, MCP or persistence-specific serializer settings.
/// </summary>
public sealed class FamilyWireJsonConventions
{
    /// <summary>Applies the interoperable JSON enum and case-insensitive input conventions.</summary>
    /// <param name="options">The transport-owned serializer options to configure.</param>
    public void ApplyEnumCompatibility(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.PropertyNameCaseInsensitive = true;
        if (!options.Converters.Any(converter => converter is JsonStringEnumConverter))
            options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>Creates the canonical JSON format for authenticated 1-Wire TCP, UDP and HTTP envelopes.</summary>
    /// <returns>A distinct options instance preserving the deployed 1-Wire wire representation.</returns>
    public JsonSerializerOptions CreateOneWireOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // MVC may emit quoted numeric values. Accept them without changing the
            // canonical one-wire numeric output or signed envelope digest bytes.
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        ApplyEnumCompatibility(options);
        return options;
    }

    /// <summary>Creates JSON-RPC/web-style options while retaining the web transport's camel-case contract.</summary>
    /// <returns>A distinct web serializer options instance with compatible string-enum handling.</returns>
    public JsonSerializerOptions CreateWebOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApplyEnumCompatibility(options);
        return options;
    }
}
