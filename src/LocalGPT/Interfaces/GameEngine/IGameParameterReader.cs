using System.Text.Json;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads the JSON parameters of ruleset rows. Missing or malformed values fall back to the caller's engine default.
/// </summary>
public interface IGameParameterReader
{
    /// <summary>
    /// Parses a parameter document; malformed or empty JSON yields an empty object.
    /// </summary>
    /// <param name="json">The parameter JSON of a ruleset row.</param>
    /// <returns>A detached JSON object element.</returns>
    JsonElement Parse(string? json);

    /// <summary>
    /// Reads an integer parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The engine default.</param>
    /// <returns>The integer value or <paramref name="fallback"/>.</returns>
    int ReadInt(JsonElement parameters, string name, int fallback);

    /// <summary>
    /// Reads a floating-point parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The engine default.</param>
    /// <returns>The numeric value or <paramref name="fallback"/>.</returns>
    double ReadDouble(JsonElement parameters, string name, double fallback);

    /// <summary>
    /// Reads a string parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The engine default.</param>
    /// <returns>The string value or <paramref name="fallback"/>.</returns>
    string ReadString(JsonElement parameters, string name, string fallback);

    /// <summary>
    /// Reads a boolean parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The engine default.</param>
    /// <returns>The boolean value or <paramref name="fallback"/>.</returns>
    bool ReadBool(JsonElement parameters, string name, bool fallback);

    /// <summary>
    /// Reads a string array parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The strings, or an empty list.</returns>
    IReadOnlyList<string> ReadStrings(JsonElement parameters, string name);

    /// <summary>
    /// Reads an integer array parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The integers, or an empty list.</returns>
    IReadOnlyList<int> ReadInts(JsonElement parameters, string name);

    /// <summary>
    /// Reads a nested object parameter.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The nested object, or an empty object when it is missing.</returns>
    JsonElement ReadObject(JsonElement parameters, string name);

    /// <summary>
    /// Reads an array of nested objects.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The object elements, or an empty list.</returns>
    IReadOnlyList<JsonElement> ReadObjects(JsonElement parameters, string name);

    /// <summary>
    /// Reads a raw parameter value of any JSON kind.
    /// </summary>
    /// <param name="parameters">The parameter object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value when present.</param>
    /// <returns><c>true</c> when the property exists.</returns>
    bool TryRead(JsonElement parameters, string name, out JsonElement value);
}
