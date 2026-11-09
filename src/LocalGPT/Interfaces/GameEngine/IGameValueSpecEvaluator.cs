using System.Text.Json;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Evaluates declarative value specifications of ruleset rows: a template string, or an object with
/// <c>firstOf</c>, <c>join</c>/<c>values</c>, <c>when</c>/<c>then</c>/<c>else</c>, <c>value</c>, <c>max</c> and <c>clean</c>.
/// </summary>
public interface IGameValueSpecEvaluator
{
    /// <summary>
    /// Evaluates a value specification.
    /// </summary>
    /// <param name="specification">The specification element; an undefined element yields an empty string.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The evaluated text.</returns>
    string Evaluate(JsonElement specification, IReadOnlyDictionary<string, object?> tokens);

    /// <summary>
    /// Tests a condition of the form <c>token</c>, <c>!token</c>, <c>token==value</c> or <c>token!=value</c>, combined with <c>&amp;&amp;</c> or <c>||</c>.
    /// </summary>
    /// <param name="condition">The condition; empty conditions are true.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The condition result.</returns>
    bool Test(string? condition, IReadOnlyDictionary<string, object?> tokens);
}
