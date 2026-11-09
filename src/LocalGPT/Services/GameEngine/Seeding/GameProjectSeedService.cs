using System.Reflection;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.GameEngine.Seeding;

/// <summary>
/// Seeds the shipped system games from the embedded documents <c>LocalGPT.GameSeeds.*.json</c>. Each document holds data
/// only: the project, the authoring profile (primitive keys and parameters) and the stat, archetype, action, rule, level,
/// text and alias rows. Seeds never overwrite a system game the user has modified.
/// </summary>
/// <param name="parameters">Reads seed documents.</param>
/// <param name="logger">Receives seed diagnostics.</param>
public sealed class GameProjectSeedService(IGameParameterReader parameters, ILogger<GameProjectSeedService> logger) : IGameProjectSeedService
{
    /// <summary>
    /// The manifest resource prefix of the embedded seed documents.
    /// </summary>
    private const string ResourcePrefix = "LocalGPT.GameSeeds.";

    /// <inheritdoc />
    public async Task SeedAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var document in LoadDocuments())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SeedDocumentAsync(db, document, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Seeding the system games was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Seeding the system games failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task VerifyAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default)
    {
        try
        {
            var expected = LoadDocuments()
                .Select(document => parameters.ReadString(parameters.ReadObject(document, "profile"), "gameKey", string.Empty))
                .Where(key => key.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actual = (await db.LocalGptGameProjectProfiles.AsNoTracking()
                    .Where(profile => profile.IsSystemSeed)
                    .Select(profile => profile.GameKey)
                    .ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException($"System game seed incomplete: {expected.Except(actual).Count()} shipped game(s) are missing.");
            logger.LogInformation("Verified {Count} shipped system game project(s).", expected.Count);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Verifying the system games was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Verifying the system games failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads the embedded seed documents in resource-name order.
    /// </summary>
    /// <returns>The seed documents.</returns>
    private List<JsonElement> LoadDocuments()
    {
        try
        {
            var assembly = typeof(GameProjectSeedService).Assembly;
            var documents = new List<JsonElement>();
            foreach (var name in assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal))
            {
                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded game seed {name} could not be opened.");
                using var reader = new StreamReader(stream);
                var document = parameters.Parse(reader.ReadToEnd());
                if (!parameters.TryRead(document, "profile", out _))
                    throw new InvalidOperationException($"Embedded game seed {name} has no profile.");
                documents.Add(document);
            }
            logger.LogDebug("Loaded {Count} embedded system game seed document(s).", documents.Count);
            return documents;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the embedded system game seeds failed.");
            throw;
        }
    }

    /// <summary>
    /// Creates or refreshes one system game.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="document">The seed document.</param>
    /// <param name="cancellationToken">Cancels the seed.</param>
    /// <returns>A task completing when the game rows are staged.</returns>
    private async Task SeedDocumentAsync(LocalGptMemoryDbContext db, JsonElement document, CancellationToken cancellationToken)
    {
        try
        {
            var seedVersion = parameters.ReadInt(document, "seedVersion", 1);
            var projectSeed = parameters.ReadObject(document, "project");
            var profileSeed = parameters.ReadObject(document, "profile");
            var gameKey = parameters.ReadString(profileSeed, "gameKey", string.Empty);
            if (gameKey.Length == 0)
                throw new InvalidOperationException("A system game seed has no game key.");

            var profile = await db.LocalGptGameProjectProfiles
                .FirstOrDefaultAsync(item => item.IsSystemSeed && item.GameKey == gameKey, cancellationToken).ConfigureAwait(false);
            if (profile is not null && (profile.IsUserModified || profile.SeedVersion >= seedVersion))
            {
                logger.LogDebug("System game {GameKey} is current (seed {SeedVersion}) or user-modified ({UserModified}); seed rows were kept.", gameKey, profile.SeedVersion, profile.IsUserModified);
                return;
            }

            var now = DateTime.UtcNow;
            if (profile is null)
            {
                var projectId = Guid.Parse(parameters.ReadString(projectSeed, "id", Guid.Empty.ToString()));
                var project = await db.LocalGptProjects.FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false);
                if (project is null)
                {
                    project = new LocalGptProject
                    {
                        Id = projectId,
                        Name = parameters.ReadString(projectSeed, "name", gameKey),
                        Purpose = parameters.ReadString(projectSeed, "purpose", string.Empty),
                        ProjectType = "Game",
                        CurrentVersion = parameters.ReadString(projectSeed, "version", "1.0.0"),
                        Status = "Active",
                        RecommendGit = false,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    db.LocalGptProjects.Add(project);
                }
                profile = new LocalGptGameProjectProfile { ProjectId = project.Id, CreatedAtUtc = now, IsSystemSeed = true };
                db.LocalGptGameProjectProfiles.Add(profile);
            }
            else
            {
                await RemoveRowsAsync(db, profile.Id, cancellationToken).ConfigureAwait(false);
            }

            ApplyProfile(profile, profileSeed, gameKey);
            profile.SeedVersion = seedVersion;
            profile.UpdatedAtUtc = now;
            AddRows(db, profile.Id, document);
            logger.LogInformation("Seeded system game {GameKey} at seed version {SeedVersion}.", gameKey, seedVersion);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "Seeding a system game was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Seeding a system game failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies the profile values of a seed document.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="seed">The profile seed.</param>
    /// <param name="gameKey">The game key.</param>
    private void ApplyProfile(LocalGptGameProjectProfile profile, JsonElement seed, string gameKey)
    {
        try
        {
            profile.GameKey = gameKey;
            profile.DisplayName = parameters.ReadString(seed, "displayName", gameKey);
            profile.ViewKey = parameters.ReadString(seed, "viewKey", string.Empty);
            profile.ViewParametersJson = Raw(seed, "viewParameters");
            profile.TurnModelKey = parameters.ReadString(seed, "turnModelKey", "actions");
            profile.MapGeneratorKey = parameters.ReadString(seed, "mapGeneratorKey", string.Empty);
            profile.MapParametersJson = Raw(seed, "mapParameters");
            profile.AutoplayKey = parameters.ReadString(seed, "autoplayKey", string.Empty);
            profile.AutoplayParametersJson = Raw(seed, "autoplayParameters");
            profile.PlayerArchetypeKey = parameters.ReadString(seed, "playerArchetypeKey", string.Empty);
            profile.RulesetTemplateKey = parameters.ReadString(seed, "rulesetTemplateKey", string.Empty);
            profile.DefaultTeamKey = parameters.ReadString(seed, "defaultTeamKey", string.Empty);
            profile.DefaultControlMode = Enum.TryParse<CouncilGameControlMode>(parameters.ReadString(seed, "defaultControlMode", nameof(CouncilGameControlMode.Shared)), true, out var mode)
                ? mode : CouncilGameControlMode.Shared;
            profile.DirectorMode = Enum.TryParse<CouncilGameDirectorMode>(parameters.ReadString(seed, "directorMode", nameof(CouncilGameDirectorMode.Deterministic)), true, out var director)
                ? director : CouncilGameDirectorMode.Deterministic;
            profile.CreatureDirectorCount = parameters.ReadInt(seed, "creatureDirectorCount", 2);
            profile.AutoplayDelayMilliseconds = parameters.ReadInt(seed, "autoplayDelayMilliseconds", 1200);
            profile.FrameWidth = parameters.ReadInt(seed, "frameWidth", 80);
            profile.FrameHeight = parameters.ReadInt(seed, "frameHeight", 25);
            profile.AsciiColorMode = Enum.TryParse<CouncilAsciiColorMode>(parameters.ReadString(seed, "asciiColorMode", nameof(CouncilAsciiColorMode.TerminalDefault)), true, out var color)
                ? color : CouncilAsciiColorMode.TerminalDefault;
            profile.DefaultForegroundColor = parameters.ReadInt(seed, "defaultForegroundColor", 46);
            profile.DefaultBackgroundColor = parameters.ReadInt(seed, "defaultBackgroundColor", 0);
            profile.MapSeed = parameters.ReadInt(seed, "mapSeed", 0);
            profile.ScenarioPrompt = parameters.ReadString(seed, "scenarioPrompt", string.Empty);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying the seed profile of {GameKey} failed.", gameKey);
            throw;
        }
    }

    /// <summary>
    /// Stages the rule rows of a seed document.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="profileId">The profile.</param>
    /// <param name="document">The seed document.</param>
    private void AddRows(LocalGptMemoryDbContext db, Guid profileId, JsonElement document)
    {
        try
        {
            var order = 0;
            foreach (var row in parameters.ReadObjects(document, "stats"))
            {
                db.GameProjectStats.Add(new GameProjectStat
                {
                    ProfileId = profileId,
                    StatKey = parameters.ReadString(row, "key", string.Empty),
                    DisplayName = parameters.ReadString(row, "name", string.Empty),
                    MinimumValue = parameters.ReadInt(row, "min", 0),
                    MaximumValue = parameters.ReadInt(row, "max", 999),
                    InitialValue = parameters.ReadInt(row, "initial", 0),
                    SortOrder = order += 10
                });
            }
            order = 0;
            foreach (var row in parameters.ReadObjects(document, "archetypes"))
            {
                db.GameProjectArchetypes.Add(new GameProjectArchetype
                {
                    ProfileId = profileId,
                    ArchetypeKey = parameters.ReadString(row, "key", string.Empty),
                    DisplayName = parameters.ReadString(row, "name", string.Empty),
                    Role = parameters.ReadString(row, "role", string.Empty),
                    Glyph = parameters.ReadString(row, "glyph", string.Empty),
                    BehaviourKey = parameters.ReadString(row, "behaviour", string.Empty),
                    ParametersJson = Raw(row, "parameters"),
                    SortOrder = order += 10
                });
            }
            order = 0;
            foreach (var row in parameters.ReadObjects(document, "actions"))
            {
                db.GameProjectActions.Add(new GameProjectAction
                {
                    ProfileId = profileId,
                    ActionKey = parameters.ReadString(row, "key", string.Empty),
                    DisplayName = parameters.ReadString(row, "name", string.Empty),
                    KeyboardKey = parameters.ReadString(row, "keyboard", string.Empty),
                    GamepadButton = parameters.ReadString(row, "gamepad", string.Empty),
                    PrimitiveKey = parameters.ReadString(row, "primitive", string.Empty),
                    ParametersJson = Raw(row, "parameters"),
                    AiSelectable = parameters.ReadBool(row, "aiSelectable", true),
                    SortOrder = order += 10
                });
            }
            order = 0;
            foreach (var row in parameters.ReadObjects(document, "rules"))
            {
                db.GameProjectRules.Add(new GameProjectRule
                {
                    ProfileId = profileId,
                    RuleKey = parameters.ReadString(row, "key", string.Empty),
                    Trigger = parameters.ReadString(row, "trigger", string.Empty),
                    PrimitiveKey = parameters.ReadString(row, "primitive", string.Empty),
                    ParametersJson = Raw(row, "parameters"),
                    IsEnabled = parameters.ReadBool(row, "enabled", true),
                    SortOrder = order += 10
                });
            }
            var levelIndex = 0;
            foreach (var row in parameters.ReadObjects(document, "levels"))
            {
                db.GameProjectLevels.Add(new GameProjectLevel
                {
                    ProfileId = profileId,
                    LevelIndex = levelIndex++,
                    LevelNumber = parameters.ReadInt(row, "number", levelIndex),
                    Name = parameters.ReadString(row, "name", string.Empty),
                    Difficulty = parameters.ReadInt(row, "difficulty", 1),
                    MapWidth = parameters.ReadInt(row, "width", 28),
                    MapHeight = parameters.ReadInt(row, "height", 18),
                    ParametersJson = Raw(row, "parameters")
                });
            }
            var texts = parameters.ReadObject(document, "texts");
            foreach (var text in texts.EnumerateObject())
            {
                db.GameProjectTexts.Add(new GameProjectText
                {
                    ProfileId = profileId,
                    TextKey = text.Name,
                    Text = text.Value.ValueKind == JsonValueKind.Array
                        ? string.Join('\n', text.Value.EnumerateArray().Select(line => line.GetString() ?? string.Empty))
                        : parameters.ReadString(texts, text.Name, string.Empty)
                });
            }
            foreach (var row in parameters.ReadObjects(document, "aliases"))
            {
                db.GameProjectAliases.Add(new GameProjectAlias
                {
                    ProfileId = profileId,
                    AliasKind = parameters.ReadString(row, "kind", string.Empty),
                    Alias = parameters.ReadString(row, "alias", string.Empty),
                    Target = parameters.ReadString(row, "target", string.Empty)
                });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Staging the seed rows of game profile {ProfileId} failed.", profileId);
            throw;
        }
    }

    /// <summary>
    /// Removes the seeded rule rows of a profile before a refresh.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="profileId">The profile.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A task completing when the rows are staged for removal.</returns>
    private async Task RemoveRowsAsync(LocalGptMemoryDbContext db, Guid profileId, CancellationToken cancellationToken)
    {
        try
        {
            db.GameProjectStats.RemoveRange(await db.GameProjectStats.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectArchetypes.RemoveRange(await db.GameProjectArchetypes.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectActions.RemoveRange(await db.GameProjectActions.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectRules.RemoveRange(await db.GameProjectRules.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectLevels.RemoveRange(await db.GameProjectLevels.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectTexts.RemoveRange(await db.GameProjectTexts.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
            db.GameProjectAliases.RemoveRange(await db.GameProjectAliases.Where(item => item.ProfileId == profileId).ToListAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Removing the seed rows of game profile {ProfileId} failed.", profileId);
            throw;
        }
    }

    /// <summary>
    /// Gets the raw JSON of a nested parameter object.
    /// </summary>
    /// <param name="row">The seed row.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The object JSON, or <c>{}</c>.</returns>
    private string Raw(JsonElement row, string name)
    {
        try
        {
            return parameters.TryRead(row, name, out var value) && value.ValueKind == JsonValueKind.Object ? value.GetRawText() : "{}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading raw seed parameters {ParameterName} failed.", name);
            throw;
        }
    }
}
