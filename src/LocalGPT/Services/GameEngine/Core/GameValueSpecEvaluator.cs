using System.Globalization;
using System.Text;
using System.Text.Json;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Evaluates declarative value specifications and conditions of ruleset rows.
/// </summary>
/// <param name="formatter">Formats template strings.</param>
/// <param name="textUtility">Joins, cleans and compacts values.</param>
/// <param name="logger">Receives evaluation diagnostics.</param>
public sealed class GameValueSpecEvaluator(
    IGameTemplateFormatter formatter,
    IGameTextUtilityService textUtility,
    ILogger<GameValueSpecEvaluator> logger) : IGameValueSpecEvaluator
{
    /// <inheritdoc />
    public string Evaluate(JsonElement specification, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            switch (specification.ValueKind)
            {
                case JsonValueKind.String:
                    return formatter.Format(specification.GetString(), tokens);
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return specification.GetRawText();
                case JsonValueKind.Array:
                    var builder = new StringBuilder();
                    foreach (var item in specification.EnumerateArray())
                        builder.Append(Evaluate(item, tokens));
                    return builder.ToString();
                case JsonValueKind.Object:
                    return EvaluateObject(specification, tokens);
                default:
                    return string.Empty;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating a game value specification failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public bool Test(string? condition, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;
            foreach (var alternative in condition.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var all = true;
                foreach (var atom in alternative.Split("&&", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!TestAtom(atom, tokens))
                    {
                        all = false;
                        break;
                    }
                }
                if (all)
                    return true;
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing game condition {Condition} failed.", condition);
            throw;
        }
    }

    /// <summary>
    /// Evaluates an object specification.
    /// </summary>
    /// <param name="specification">The object specification.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The evaluated text after the optional clean and max post-processing.</returns>
    private string EvaluateObject(JsonElement specification, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            string result;
            if (specification.TryGetProperty("when", out var when))
            {
                var branch = Test(when.ValueKind == JsonValueKind.String ? when.GetString() : string.Empty, tokens) ? "then" : "else";
                result = specification.TryGetProperty(branch, out var selected) ? Evaluate(selected, tokens) : string.Empty;
            }
            else if (specification.TryGetProperty("firstOf", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
            {
                result = string.Empty;
                foreach (var candidate in candidates.EnumerateArray())
                {
                    var value = Evaluate(candidate, tokens);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result = value;
                        break;
                    }
                }
            }
            else if (specification.TryGetProperty("join", out var separator) &&
                specification.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
            {
                result = textUtility.JoinDistinct(
                    separator.ValueKind == JsonValueKind.String ? separator.GetString() ?? string.Empty : string.Empty,
                    values.EnumerateArray().Select(item => Evaluate(item, tokens)).ToList());
            }
            else if (specification.TryGetProperty("value", out var value))
            {
                result = Evaluate(value, tokens);
            }
            else
            {
                result = string.Empty;
            }

            if (specification.TryGetProperty("clean", out var clean) && clean.ValueKind == JsonValueKind.True)
                result = textUtility.CleanSpokenLine(result);
            if (specification.TryGetProperty("max", out var maximum) && maximum.ValueKind == JsonValueKind.Number && maximum.TryGetInt32(out var length))
                result = textUtility.Compact(result, length);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating a game value object failed.");
            throw;
        }
    }

    /// <summary>
    /// Tests one atomic condition.
    /// </summary>
    /// <param name="atom">The atom such as <c>name</c>, <c>!name</c>, <c>name==value</c> or <c>name&gt;=3</c>.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The atom result.</returns>
    private bool TestAtom(string atom, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            foreach (var comparison in new[] { "==", "!=", ">=", "<=", ">", "<" })
            {
                var index = atom.IndexOf(comparison, StringComparison.Ordinal);
                if (index <= 0)
                    continue;
                var left = Value(atom[..index].Trim(), tokens);
                var right = atom[(index + comparison.Length)..].Trim().Trim('"', '\'');
                if (comparison is "==" or "!=")
                {
                    var equal = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                    return comparison == "==" ? equal : !equal;
                }
                if (!double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out var leftNumber) ||
                    !double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out var rightNumber))
                    return false;
                return comparison switch
                {
                    ">=" => leftNumber >= rightNumber,
                    "<=" => leftNumber <= rightNumber,
                    ">" => leftNumber > rightNumber,
                    _ => leftNumber < rightNumber
                };
            }
            return atom.StartsWith('!') ? !Truthy(atom[1..].Trim(), tokens) : Truthy(atom, tokens);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing game condition atom {ConditionAtom} failed.", atom);
            throw;
        }
    }

    /// <summary>
    /// Gets the invariant text of a token.
    /// </summary>
    /// <param name="name">The token name.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns>The token text, or an empty string for unknown tokens.</returns>
    private string Value(string name, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            return tokens.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading game condition token {TokenName} failed.", name);
            throw;
        }
    }

    /// <summary>
    /// Tests whether a token is set: non-empty text other than false or 0, a true boolean or a non-zero number.
    /// </summary>
    /// <param name="name">The token name.</param>
    /// <param name="tokens">The token values.</param>
    /// <returns><c>true</c> when the token is set.</returns>
    private bool Truthy(string name, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            if (!tokens.TryGetValue(name, out var value) || value is null)
                return false;
            return value switch
            {
                bool flag => flag,
                int number => number != 0,
                long number => number != 0,
                double number => number != 0d,
                string text => text.Length > 0 && !string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) && text != "0",
                _ => true
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Testing game condition token {TokenName} failed.", name);
            throw;
        }
    }
}
