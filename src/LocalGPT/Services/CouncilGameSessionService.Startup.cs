using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Services;

/// <summary>Resolves the database-defined game content a new shared game session starts from.</summary>
public sealed partial class CouncilGameSessionService
{
    /// <summary>Normalizes a project-defined game key to the lower-case, dash-separated form used by sessions and teams.</summary>
    /// <param name="gameKey">Game key stored on the game project profile.</param>
    /// <returns>The normalized game key (at most 160 characters).</returns>
    /// <exception cref="ArgumentException">The key contains no letters or digits.</exception>
    private string NormalizeDefinitionKey(string? gameKey)
    {
        try
        {
            var normalized = (gameKey ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            normalized = new string(normalized.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("The game definition contains an invalid game key.", nameof(gameKey));
            return normalized.Length > 160 ? normalized[..160] : normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing game key {GameKey} failed.", gameKey);
            throw;
        }
    }

    /// <summary>
    /// Returns the ruleset a session runs with: the definition's composed ruleset, otherwise (definitions built before the
    /// declarative rule tables existed) the ruleset of its authoring profile, otherwise the default ruleset template named by
    /// the runtime policy value <c>GameDefaultRulesetTemplateKey</c>.
    /// </summary>
    /// <param name="definition">Built or resolved game definition.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The ruleset used by the new session.</returns>
    private async Task<GameRuleset> ResolveRulesetAsync(ProjectGameDefinition definition, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsEmptyRuleset(definition.Ruleset))
                return definition.Ruleset;
            if (definition.AuthoringProfileId != Guid.Empty)
            {
                try
                {
                    var authored = await rulesetStore.LoadRulesetAsync(definition.AuthoringProfileId, cancellationToken).ConfigureAwait(false);
                    if (!IsEmptyRuleset(authored))
                    {
                        logger.LogInformation("Game definition {GameKey} carried no ruleset; the ruleset of authoring profile {ProfileId} was loaded.", definition.GameKey, definition.AuthoringProfileId);
                        return authored;
                    }
                }
                catch (KeyNotFoundException exception)
                {
                    logger.LogWarning(exception, "The authoring profile {ProfileId} of game definition {GameKey} no longer exists.", definition.AuthoringProfileId, definition.GameKey);
                }
            }

            var templateKey = runtimePolicy.GetString("GameDefaultRulesetTemplateKey");
            var template = string.IsNullOrWhiteSpace(templateKey)
                ? null
                : await rulesetStore.ResolveDefinitionAsync(templateKey, cancellationToken).ConfigureAwait(false);
            if (template is not null && !IsEmptyRuleset(template.Ruleset))
            {
                logger.LogWarning("Game definition {GameKey} has no ruleset of its own; it runs with the default ruleset template {TemplateKey}.", definition.GameKey, templateKey);
                return template.Ruleset;
            }

            logger.LogWarning("Game definition {GameKey} has no ruleset and no default ruleset template is available; it starts with an empty ruleset.", definition.GameKey);
            return definition.Ruleset;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Resolving the ruleset of game {GameKey} was cancelled.", definition.GameKey);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the ruleset of game {GameKey} failed.", definition.GameKey);
            throw;
        }
    }

    /// <summary>Tells whether a ruleset defines nothing the engine could run.</summary>
    /// <param name="ruleset">Ruleset to inspect.</param>
    /// <returns><c>true</c> when the ruleset names no view and has no action or rule rows.</returns>
    private bool IsEmptyRuleset(GameRuleset? ruleset)
    {
        try
        {
            return ruleset is null
                || (string.IsNullOrWhiteSpace(ruleset.ViewKey) && ruleset.Actions.Count == 0 && ruleset.Rules.Count == 0);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inspecting a game ruleset failed.");
            throw;
        }
    }

    /// <summary>
    /// Finds the level index a session starts at: the level row whose number equals the requested level (1..99), otherwise
    /// the requested level read as a one-based position, clamped to the defined levels.
    /// </summary>
    /// <param name="ruleset">Ruleset whose level rows are searched.</param>
    /// <param name="startingLevel">Requested starting level number; <c>null</c> starts at level 1.</param>
    /// <returns>The zero-based level index, or 0 when the game defines no levels.</returns>
    private int ResolveStartingLevelIndex(GameRuleset ruleset, int? startingLevel)
    {
        try
        {
            var levels = ruleset.Levels.OrderBy(level => level.LevelIndex).ToList();
            if (levels.Count == 0)
                return 0;
            var requested = startingLevel ?? 1;
            var index = levels.FindIndex(level => level.LevelNumber == Math.Clamp(requested, 1, 99));
            return index >= 0 ? index : Math.Clamp(requested - 1, 0, levels.Count - 1);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the starting level {StartingLevel} failed.", startingLevel);
            throw;
        }
    }
}
