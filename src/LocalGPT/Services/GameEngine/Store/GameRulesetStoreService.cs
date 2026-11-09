using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces;
using LocalGPT.Interfaces.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.GameEngine.Store;

/// <summary>
/// Persistence of game rulesets in the project system. A profile's empty primitive keys and empty row tables inherit
/// from the system game named by <see cref="LocalGptGameProjectProfile.RulesetTemplateKey"/>, so user projects reuse
/// shipped rules without copying them.
/// </summary>
/// <param name="dbContextFactory">Creates database contexts.</param>
/// <param name="databaseInitializer">Makes sure migrations and seeds ran.</param>
/// <param name="texts">Reads launcher texts.</param>
/// <param name="logger">Receives store diagnostics.</param>
public sealed class GameRulesetStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IGameTextService texts,
    ILogger<GameRulesetStoreService> logger) : IGameRulesetStoreService
{
    /// <inheritdoc />
    public async Task<GameRuleset> LoadRulesetAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profile = await db.LocalGptGameProjectProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == profileId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Game profile {profileId} was not found.");
            return await ComposeAsync(db, profile, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Loading the ruleset of game profile {ProfileId} was cancelled.", profileId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the ruleset of game profile {ProfileId} failed.", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProjectGameDefinition?> ResolveDefinitionAsync(string gameKeyOrAlias, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = (gameKeyOrAlias ?? string.Empty).Trim();
            if (key.Length == 0)
                return null;
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profile = await FindProfileAsync(db, key, cancellationToken).ConfigureAwait(false);
            if (profile is null)
            {
                var alias = await db.GameProjectAliases.AsNoTracking()
                    .Where(item => item.AliasKind == "game" && item.Alias.ToLower() == key.ToLower())
                    .Select(item => item.Target)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(alias))
                    profile = await FindProfileAsync(db, alias, cancellationToken).ConfigureAwait(false);
            }
            if (profile is null)
            {
                logger.LogInformation("No game project matches game key or alias {GameKey}.", key);
                return null;
            }

            var ruleset = await ComposeAsync(db, profile, cancellationToken).ConfigureAwait(false);
            return new ProjectGameDefinition
            {
                ProjectId = profile.ProjectId,
                AuthoringProfileId = profile.Id,
                GameKey = profile.GameKey,
                DisplayName = profile.DisplayName,
                Ruleset = ruleset,
                DefaultTeamKey = profile.DefaultTeamKey,
                DefaultControlMode = profile.DefaultControlMode,
                DirectorMode = profile.DirectorMode,
                GameDirectorModelName = profile.GameDirectorModelName,
                CreatureDirectorCount = profile.CreatureDirectorCount,
                AutoplayDelayMilliseconds = profile.AutoplayDelayMilliseconds,
                FrameWidth = profile.FrameWidth,
                FrameHeight = profile.FrameHeight,
                AsciiColorMode = profile.AsciiColorMode,
                DefaultForegroundColor = profile.DefaultForegroundColor,
                DefaultBackgroundColor = profile.DefaultBackgroundColor,
                MapSeed = profile.MapSeed,
                ScenarioPrompt = profile.ScenarioPrompt
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Resolving game {GameKey} was cancelled.", gameKeyOrAlias);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving game {GameKey} failed.", gameKeyOrAlias);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameCatalogEntry>> ListCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profiles = await db.LocalGptGameProjectProfiles.AsNoTracking()
                .Where(item => item.GameKey != string.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var entries = new List<GameCatalogEntry>();
            foreach (var profile in profiles)
            {
                var ruleset = await ComposeAsync(db, profile, cancellationToken).ConfigureAwait(false);
                var order = texts.Get(ruleset, "launcher.order");
                entries.Add(new GameCatalogEntry
                {
                    GameKey = profile.GameKey,
                    DisplayName = profile.DisplayName,
                    ProjectId = profile.ProjectId,
                    DefaultTeamKey = profile.DefaultTeamKey,
                    IsSystemSeed = profile.IsSystemSeed,
                    ViewKey = ruleset.ViewKey,
                    TurnModelKey = ruleset.TurnModelKey,
                    LaunchLabel = ruleset.Texts.Any(text => string.Equals(text.TextKey, "launcher.label", StringComparison.OrdinalIgnoreCase))
                        ? texts.Get(ruleset, "launcher.label")
                        : profile.DisplayName,
                    SortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : int.MaxValue,
                    ShowInLauncher = ruleset.Texts.Any(text => string.Equals(text.TextKey, "launcher.order", StringComparison.OrdinalIgnoreCase)),
                    FrameWidth = profile.FrameWidth,
                    FrameHeight = profile.FrameHeight
                });
            }
            return entries
                .OrderByDescending(entry => entry.IsSystemSeed)
                .ThenBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Listing the game catalog was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing the game catalog failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameCatalogEntry?> FindForTeamAsync(string teamKey, IReadOnlyList<string> runtimeClassKeys, CancellationToken cancellationToken = default)
    {
        try
        {
            var catalog = await ListCatalogAsync(cancellationToken).ConfigureAwait(false);
            var byTeam = catalog.FirstOrDefault(entry => string.Equals(entry.DefaultTeamKey, teamKey, StringComparison.OrdinalIgnoreCase));
            if (byTeam is not null)
                return byTeam;
            if (runtimeClassKeys.Count == 0)
                return null;

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var prefixes = await db.GameProjectAliases.AsNoTracking()
                .Where(item => item.AliasKind == "runtime-class-prefix")
                .Select(item => new { item.Alias, item.Target })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var match = prefixes
                .Where(prefix => runtimeClassKeys.Any(key => key.StartsWith(prefix.Alias, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(prefix => prefix.Alias.Length)
                .FirstOrDefault();
            return match is null
                ? null
                : catalog.FirstOrDefault(entry => string.Equals(entry.GameKey, match.Target, StringComparison.OrdinalIgnoreCase) && entry.IsSystemSeed)
                    ?? catalog.FirstOrDefault(entry => string.Equals(entry.GameKey, match.Target, StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Finding the game of team {TeamKey} was cancelled.", teamKey);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding the game of team {TeamKey} failed.", teamKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GameRuleset> SaveRulesetAsync(Guid projectId, SaveGameRulesetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Replacing game rules requires explicit user confirmation.");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profile = await db.LocalGptGameProjectProfiles
                .SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Game project {projectId} has no game profile.");

            db.GameProjectStats.RemoveRange(db.GameProjectStats.Where(item => item.ProfileId == profile.Id));
            db.GameProjectArchetypes.RemoveRange(db.GameProjectArchetypes.Where(item => item.ProfileId == profile.Id));
            db.GameProjectActions.RemoveRange(db.GameProjectActions.Where(item => item.ProfileId == profile.Id));
            db.GameProjectRules.RemoveRange(db.GameProjectRules.Where(item => item.ProfileId == profile.Id));
            db.GameProjectLevels.RemoveRange(db.GameProjectLevels.Where(item => item.ProfileId == profile.Id));
            db.GameProjectTexts.RemoveRange(db.GameProjectTexts.Where(item => item.ProfileId == profile.Id));
            db.GameProjectAliases.RemoveRange(db.GameProjectAliases.Where(item => item.ProfileId == profile.Id));

            db.GameProjectStats.AddRange(request.Stats.Select((item, index) => new GameProjectStat
            {
                ProfileId = profile.Id, StatKey = item.StatKey.Trim(), DisplayName = item.DisplayName.Trim(),
                MinimumValue = item.MinimumValue, MaximumValue = item.MaximumValue, InitialValue = item.InitialValue, SortOrder = (index + 1) * 10
            }));
            db.GameProjectArchetypes.AddRange(request.Archetypes.Select((item, index) => new GameProjectArchetype
            {
                ProfileId = profile.Id, ArchetypeKey = item.ArchetypeKey.Trim(), DisplayName = item.DisplayName.Trim(), Role = item.Role.Trim(),
                Glyph = item.Glyph, BehaviourKey = item.BehaviourKey.Trim(), ParametersJson = NormalizeJson(item.ParametersJson), SortOrder = (index + 1) * 10
            }));
            db.GameProjectActions.AddRange(request.Actions.Select((item, index) => new GameProjectAction
            {
                ProfileId = profile.Id, ActionKey = item.ActionKey.Trim(), DisplayName = item.DisplayName.Trim(), KeyboardKey = item.KeyboardKey.Trim(),
                GamepadButton = item.GamepadButton.Trim(), PrimitiveKey = item.PrimitiveKey.Trim(), ParametersJson = NormalizeJson(item.ParametersJson),
                AiSelectable = item.AiSelectable, SortOrder = (index + 1) * 10
            }));
            db.GameProjectRules.AddRange(request.Rules.Select((item, index) => new GameProjectRule
            {
                ProfileId = profile.Id, RuleKey = item.RuleKey.Trim(), Trigger = item.Trigger.Trim(), PrimitiveKey = item.PrimitiveKey.Trim(),
                ParametersJson = NormalizeJson(item.ParametersJson), IsEnabled = item.IsEnabled, SortOrder = (index + 1) * 10
            }));
            db.GameProjectLevels.AddRange(request.Levels.Select((item, index) => new GameProjectLevel
            {
                ProfileId = profile.Id, LevelIndex = index, LevelNumber = item.LevelNumber, Name = item.Name.Trim(), Difficulty = item.Difficulty,
                MapWidth = item.MapWidth, MapHeight = item.MapHeight, ParametersJson = NormalizeJson(item.ParametersJson)
            }));
            db.GameProjectTexts.AddRange(request.Texts.Select(item => new GameProjectText { ProfileId = profile.Id, TextKey = item.TextKey.Trim(), Text = item.Text ?? string.Empty }));
            db.GameProjectAliases.AddRange(request.Aliases.Select(item => new GameProjectAlias
            {
                ProfileId = profile.Id, AliasKind = item.AliasKind.Trim(), Alias = item.Alias.Trim(), Target = item.Target.Trim()
            }));
            if (profile.IsSystemSeed)
                profile.IsUserModified = true;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Replaced the game rules of project {ProjectId}: {Stats} stat(s), {Archetypes} archetype(s), {Actions} action(s), {Rules} rule(s), {Levels} level(s), {Texts} text(s), {Aliases} alias(es).",
                projectId, request.Stats.Count, request.Archetypes.Count, request.Actions.Count, request.Rules.Count, request.Levels.Count, request.Texts.Count, request.Aliases.Count);
            return await ComposeAsync(db, profile, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Saving the game rules of project {ProjectId} was cancelled.", projectId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving the game rules of project {ProjectId} failed.", projectId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<SaveGameRulesetRequest> GetOwnRowsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profile = await db.LocalGptGameProjectProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Game project {projectId} has no game profile.");
            var rows = await LoadRowsAsync(db, profile.Id, cancellationToken).ConfigureAwait(false);
            return new SaveGameRulesetRequest
            {
                Stats = rows.Stats,
                Archetypes = rows.Archetypes,
                Actions = rows.Actions,
                Rules = rows.Rules,
                Levels = rows.Levels,
                Texts = rows.Texts,
                Aliases = rows.Aliases
            };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Reading the game rules of project {ProjectId} was cancelled.", projectId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the game rules of project {ProjectId} failed.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Finds a profile by game key, preferring the shipped system game.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="gameKey">The game key.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The profile, or <c>null</c>.</returns>
    private async Task<LocalGptGameProjectProfile?> FindProfileAsync(LocalGptMemoryDbContext db, string gameKey, CancellationToken cancellationToken)
    {
        try
        {
            var lowered = gameKey.Trim().ToLowerInvariant();
            return await db.LocalGptGameProjectProfiles.AsNoTracking()
                .Where(item => item.GameKey.ToLower() == lowered)
                .OrderByDescending(item => item.IsSystemSeed)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding game profile {GameKey} failed.", gameKey);
            throw;
        }
    }

    /// <summary>
    /// Composes a ruleset from a profile and its template game.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="profile">The profile.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The ruleset.</returns>
    private async Task<GameRuleset> ComposeAsync(LocalGptMemoryDbContext db, LocalGptGameProjectProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            var own = await LoadRowsAsync(db, profile.Id, cancellationToken).ConfigureAwait(false);
            LocalGptGameProjectProfile? template = null;
            if (!string.IsNullOrWhiteSpace(profile.RulesetTemplateKey) &&
                !string.Equals(profile.RulesetTemplateKey, profile.GameKey, StringComparison.OrdinalIgnoreCase))
            {
                template = await FindProfileAsync(db, profile.RulesetTemplateKey, cancellationToken).ConfigureAwait(false);
                if (template is null)
                    logger.LogWarning("Game profile {ProfileId} names the missing ruleset template {TemplateKey}; only its own rows are used.", profile.Id, profile.RulesetTemplateKey);
            }
            var inherited = template is null ? null : await LoadRowsAsync(db, template.Id, cancellationToken).ConfigureAwait(false);
            return new GameRuleset
            {
                ViewKey = Pick(profile.ViewKey, template?.ViewKey),
                ViewParametersJson = PickJson(profile.ViewParametersJson, template?.ViewParametersJson),
                TurnModelKey = Pick(profile.TurnModelKey, template?.TurnModelKey),
                MapGeneratorKey = Pick(profile.MapGeneratorKey, template?.MapGeneratorKey),
                MapParametersJson = PickJson(profile.MapParametersJson, template?.MapParametersJson),
                AutoplayKey = Pick(profile.AutoplayKey, template?.AutoplayKey),
                AutoplayParametersJson = PickJson(profile.AutoplayParametersJson, template?.AutoplayParametersJson),
                PlayerArchetypeKey = Pick(profile.PlayerArchetypeKey, template?.PlayerArchetypeKey),
                Stats = own.Stats.Count > 0 || inherited is null ? own.Stats : inherited.Stats,
                Archetypes = own.Archetypes.Count > 0 || inherited is null ? own.Archetypes : inherited.Archetypes,
                Actions = own.Actions.Count > 0 || inherited is null ? own.Actions : inherited.Actions,
                Rules = own.Rules.Count > 0 || inherited is null ? own.Rules : inherited.Rules,
                Levels = own.Levels.Count > 0 || inherited is null ? own.Levels : inherited.Levels,
                Texts = own.Texts.Count > 0 || inherited is null ? own.Texts : inherited.Texts,
                Aliases = own.Aliases.Count > 0 || inherited is null ? own.Aliases : inherited.Aliases
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Composing the ruleset of game profile {ProfileId} failed.", profile.Id);
            throw;
        }
    }

    /// <summary>
    /// Loads the own rule rows of a profile in authoring order.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="profileId">The profile.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows.</returns>
    private async Task<GameRuleset> LoadRowsAsync(LocalGptMemoryDbContext db, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            return new GameRuleset
            {
                Stats = await db.GameProjectStats.AsNoTracking().Where(item => item.ProfileId == profileId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false),
                Archetypes = await db.GameProjectArchetypes.AsNoTracking().Where(item => item.ProfileId == profileId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false),
                Actions = await db.GameProjectActions.AsNoTracking().Where(item => item.ProfileId == profileId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false),
                Rules = await db.GameProjectRules.AsNoTracking().Where(item => item.ProfileId == profileId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false),
                Levels = await db.GameProjectLevels.AsNoTracking().Where(item => item.ProfileId == profileId).OrderBy(item => item.LevelIndex).ToListAsync(cancellationToken).ConfigureAwait(false),
                Texts = await db.GameProjectTexts.AsNoTracking().Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false),
                Aliases = await db.GameProjectAliases.AsNoTracking().Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the rule rows of game profile {ProfileId} failed.", profileId);
            throw;
        }
    }

    /// <summary>
    /// Picks the own key, or the template's when the own key is empty.
    /// </summary>
    /// <param name="own">The profile value.</param>
    /// <param name="inherited">The template value.</param>
    /// <returns>The effective value.</returns>
    private string Pick(string? own, string? inherited)
    {
        try
        {
            return string.IsNullOrWhiteSpace(own) ? inherited ?? string.Empty : own;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Picking a ruleset key failed.");
            throw;
        }
    }

    /// <summary>
    /// Picks the own parameter document, or the template's when the own document is empty.
    /// </summary>
    /// <param name="own">The profile document.</param>
    /// <param name="inherited">The template document.</param>
    /// <returns>The effective document.</returns>
    private string PickJson(string? own, string? inherited)
    {
        try
        {
            var trimmed = (own ?? string.Empty).Trim();
            return trimmed.Length == 0 || trimmed == "{}" ? inherited ?? "{}" : own!;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Picking ruleset parameters failed.");
            throw;
        }
    }

    /// <summary>
    /// Validates a parameter document and stores <c>{}</c> for empty or malformed input.
    /// </summary>
    /// <param name="json">The parameter document.</param>
    /// <returns>The stored document.</returns>
    private string NormalizeJson(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{}";
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(json);
                return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object ? json.Trim() : "{}";
            }
            catch (System.Text.Json.JsonException exception)
            {
                logger.LogWarning(exception, "Malformed rule parameters were stored as an empty object.");
                return "{}";
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing rule parameters failed.");
            throw;
        }
    }
}
