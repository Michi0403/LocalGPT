using System.Runtime.CompilerServices;
using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Resolves game texts from the ruleset's text rows and, as fallback, from the maintained engine default texts
/// (runtime policy value <c>GameEngineDefaultTextsJson</c>).
/// </summary>
/// <param name="runtimePolicy">Provides the maintained engine default texts.</param>
/// <param name="formatter">Formats text templates.</param>
/// <param name="logger">Receives text diagnostics.</param>
public sealed class GameTextService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IGameTemplateFormatter formatter,
    ILogger<GameTextService> logger) : IGameTextService
{
    /// <summary>
    /// Text lookups per ruleset instance; rulesets are immutable while sessions run.
    /// </summary>
    private readonly ConditionalWeakTable<GameRuleset, Dictionary<string, string>> rulesetTexts = new();

    /// <summary>
    /// Guards the cached engine default texts.
    /// </summary>
    private readonly object defaultsGate = new();

    /// <summary>
    /// The maintained JSON the cached defaults were parsed from.
    /// </summary>
    private string defaultsRaw = string.Empty;

    /// <summary>
    /// The cached engine default texts.
    /// </summary>
    private Dictionary<string, string> defaultsLookup = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool Has(GameRuleset ruleset, string key)
    {
        try
        {
            return Lookup(ruleset).ContainsKey(key) || Defaults().ContainsKey(key);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking game text {TextKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public string Get(GameRuleset ruleset, string key)
    {
        try
        {
            if (Lookup(ruleset).TryGetValue(key, out var text))
                return text;
            if (Defaults().TryGetValue(key, out var fallback))
                return fallback;
            logger.LogDebug("Game text {TextKey} is defined neither by the game nor by the engine defaults.", key);
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading game text {TextKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public string Format(GameRuleset ruleset, string key, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            return formatter.Format(Get(ruleset, key), tokens);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting game text {TextKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Gets the text dictionary of a ruleset.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns>The texts keyed case-insensitively.</returns>
    private Dictionary<string, string> Lookup(GameRuleset ruleset)
    {
        try
        {
            return rulesetTexts.GetValue(ruleset, BuildLookup);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the game text lookup failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the text dictionary of a ruleset; later rows with the same key win.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns>The texts keyed case-insensitively.</returns>
    private Dictionary<string, string> BuildLookup(GameRuleset ruleset)
    {
        try
        {
            var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in ruleset.Texts)
                texts[row.TextKey] = row.Text ?? string.Empty;
            return texts;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building game texts failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads the maintained engine default texts.
    /// </summary>
    /// <returns>The engine default texts keyed case-insensitively.</returns>
    private Dictionary<string, string> Defaults()
    {
        try
        {
            var raw = runtimePolicy.GetString("GameEngineDefaultTextsJson");
            lock (defaultsGate)
            {
                if (!string.Equals(raw, defaultsRaw, StringComparison.Ordinal))
                {
                    var values = JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? new Dictionary<string, string>();
                    defaultsLookup = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
                    defaultsRaw = raw;
                    logger.LogDebug("Loaded {DefaultTextCount} maintained game engine default text(s).", defaultsLookup.Count);
                }
                return defaultsLookup;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the engine default texts failed.");
            throw;
        }
    }
}
