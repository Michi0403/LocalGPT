using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine;

/// <summary>
/// Registry of the engine primitives registered in dependency injection, looked up case-insensitively by key.
/// </summary>
/// <param name="mapGenerators">The registered map generators.</param>
/// <param name="actionPrimitives">The registered action primitives.</param>
/// <param name="rulePrimitives">The registered rule primitives.</param>
/// <param name="viewPrimitives">The registered view primitives.</param>
/// <param name="autoplayPrimitives">The registered autoplay primitives.</param>
/// <param name="stylePrimitives">The registered style primitives.</param>
/// <param name="councilProgressions">The registered council progression primitives.</param>
/// <param name="logger">Receives registry diagnostics.</param>
public sealed class GamePrimitiveRegistry(
    IEnumerable<IGameMapGeneratorPrimitive> mapGenerators,
    IEnumerable<IGameActionPrimitive> actionPrimitives,
    IEnumerable<IGameRulePrimitive> rulePrimitives,
    IEnumerable<IGameViewPrimitive> viewPrimitives,
    IEnumerable<IGameAutoplayPrimitive> autoplayPrimitives,
    IEnumerable<IGameStylePrimitive> stylePrimitives,
    IEnumerable<IGameCouncilProgressionPrimitive> councilProgressions,
    ILogger<GamePrimitiveRegistry> logger) : IGamePrimitiveRegistry
{
    /// <summary>The map generators by key.</summary>
    private readonly Dictionary<string, IGameMapGeneratorPrimitive> maps = mapGenerators.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The action primitives by key.</summary>
    private readonly Dictionary<string, IGameActionPrimitive> actions = actionPrimitives.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The rule primitives by key.</summary>
    private readonly Dictionary<string, IGameRulePrimitive> rules = rulePrimitives.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The view primitives by key.</summary>
    private readonly Dictionary<string, IGameViewPrimitive> views = viewPrimitives.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The autoplay primitives by key.</summary>
    private readonly Dictionary<string, IGameAutoplayPrimitive> autoplays = autoplayPrimitives.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The style primitives by key.</summary>
    private readonly Dictionary<string, IGameStylePrimitive> styles = stylePrimitives.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The council progression primitives by key.</summary>
    private readonly Dictionary<string, IGameCouncilProgressionPrimitive> progressions = councilProgressions.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IGameMapGeneratorPrimitive? MapGenerator(string? key)
    {
        try
        {
            return Find(maps, key, "map generator");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up map generator {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameActionPrimitive? Action(string? key)
    {
        try
        {
            return Find(actions, key, "action");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up action primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameRulePrimitive? Rule(string? key)
    {
        try
        {
            return Find(rules, key, "rule");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up rule primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameViewPrimitive? View(string? key)
    {
        try
        {
            return Find(views, key, "view");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up view primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameAutoplayPrimitive? Autoplay(string? key)
    {
        try
        {
            return Find(autoplays, key, "autoplay");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up autoplay primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameStylePrimitive? Style(string? key)
    {
        try
        {
            return Find(styles, key, "style");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up style primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IGameCouncilProgressionPrimitive? CouncilProgression(string? key)
    {
        try
        {
            return Find(progressions, key, "council progression");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Looking up council progression primitive {PrimitiveKey} failed.", key);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ListKeys()
    {
        try
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["map"] = maps.Keys.Order(StringComparer.Ordinal).ToList(),
                ["action"] = actions.Keys.Order(StringComparer.Ordinal).ToList(),
                ["rule"] = rules.Keys.Order(StringComparer.Ordinal).ToList(),
                ["view"] = views.Keys.Order(StringComparer.Ordinal).ToList(),
                ["autoplay"] = autoplays.Keys.Order(StringComparer.Ordinal).ToList(),
                ["style"] = styles.Keys.Order(StringComparer.Ordinal).ToList(),
                ["council"] = progressions.Keys.Order(StringComparer.Ordinal).ToList()
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing game engine primitive keys failed.");
            throw;
        }
    }

    /// <summary>
    /// Finds a primitive and logs unknown keys.
    /// </summary>
    /// <typeparam name="TPrimitive">The primitive kind.</typeparam>
    /// <param name="primitives">The primitives by key.</param>
    /// <param name="key">The requested key.</param>
    /// <param name="kind">The primitive kind for diagnostics.</param>
    /// <returns>The primitive, or <c>null</c>.</returns>
    private TPrimitive? Find<TPrimitive>(Dictionary<string, TPrimitive> primitives, string? key, string kind) where TPrimitive : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            if (primitives.TryGetValue(key, out var primitive))
                return primitive;
            logger.LogWarning("The game ruleset references unknown {PrimitiveKind} primitive {PrimitiveKey}.", kind, key);
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding {PrimitiveKind} primitive {PrimitiveKey} failed.", kind, key);
            throw;
        }
    }
}
