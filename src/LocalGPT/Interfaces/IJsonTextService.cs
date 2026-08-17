using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Interfaces;

/// <summary>Serializes application values and JSON nodes through the DI service boundary instead of exposing serializer extensions to UI or filters.</summary>
public interface IJsonTextService
{
    /// <summary>Serializes an application value with caller-supplied or LocalGPT default JSON options.</summary>
    /// <param name="value">Value value supplied to the JSON text operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string Serialize(object? value, JsonSerializerOptions? options = null);

    /// <summary>Escapes a string for insertion into a JSON string value without adding surrounding quotes.</summary>
    /// <param name="value">Value value supplied to the JSON text operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string EscapeStringValue(string? value);

    /// <summary>Serializes a JSON node with the requested options.</summary>
    /// <param name="node">Node value supplied to the JSON text operation and used when producing its result.</param>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string SerializeNode(JsonNode? node, JsonSerializerOptions? options = null);
}
