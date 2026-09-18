using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>Owns configurable ASCII DOOM campaign profiles resolved from database-backed runtime classes.</summary>
public sealed partial class CouncilGameSessionService
{
    private sealed record DoomCampaignSettings(
        string RuntimeClassKey,
        IReadOnlyList<CouncilGameLevelProfile> Levels,
        int StartingLevelIndex,
        bool AutoAdvanceLevels);

    private readonly JsonSerializerOptions campaignJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Resolves one copyable runtime-class campaign, preferring an explicit override and then the campaign assigned to the selected Council team.</summary>
    private async Task<DoomCampaignSettings> ResolveDoomCampaignAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string starterKey = "games.ascii.doom.campaign";
            using var scope = scopeFactory.CreateScope();
            var runtimeClassCatalog = scope.ServiceProvider.GetRequiredService<ICouncilRuntimeClassService>();
            var definitions = await runtimeClassCatalog.GetDefinitionsAsync(includeDisabled: false, cancellationToken).ConfigureAwait(false);
            var definitionsByKey = definitions
                .Where(definition => !string.IsNullOrWhiteSpace(definition.Key))
                .ToDictionary(definition => definition.Key, StringComparer.OrdinalIgnoreCase);

            CouncilRuntimeClassDefinition? definition = null;
            var explicitKey = request.CampaignRuntimeClassKey?.Trim() ?? string.Empty;
            if (explicitKey.Length > 0)
            {
                if (definitionsByKey.TryGetValue(explicitKey, out var explicitDefinition)
                    && IsDoomCampaignDefinition(explicitDefinition))
                {
                    definition = explicitDefinition;
                }
                else
                {
                    logger.LogWarning(
                        "Requested ASCII DOOM campaign runtime class {RuntimeClassKey} was not found or does not expose the campaign field contract; team/default assignment will be used.",
                        explicitKey);
                }
            }

            if (definition is null && !string.IsNullOrWhiteSpace(request.TeamKey))
            {
                var teamCatalog = scope.ServiceProvider.GetRequiredService<ICouncilTeamConfigurationService>();
                var team = await teamCatalog.FindTeamAsync(request.TeamKey, cancellationToken).ConfigureAwait(false);
                if (team is not null)
                {
                    var assignedCampaigns = team.Roles
                        .SelectMany(role => role.RuntimeClassKeys ?? [])
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(key => definitionsByKey.TryGetValue(key, out var assigned) ? assigned : null)
                        .Where(assigned => assigned is not null && IsDoomCampaignDefinition(assigned))
                        .Cast<CouncilRuntimeClassDefinition>()
                        .OrderBy(assigned => assigned.IsSystemSeed ? 1 : 0)
                        .ToList();
                    definition = assignedCampaigns.FirstOrDefault();
                }
            }

            if (definition is null
                && definitionsByKey.TryGetValue(starterKey, out var starterDefinition)
                && IsDoomCampaignDefinition(starterDefinition))
            {
                definition = starterDefinition;
            }

            if (definition is null)
                throw new InvalidOperationException($"No enabled ASCII DOOM campaign runtime class is assigned to team '{request.TeamKey}' and the maintained starter class '{starterKey}' is unavailable.");

            var levels = new List<CouncilGameLevelProfile>();
            var startingLevel = 1;
            var autoAdvance = true;
            var resolvedKey = definition.Key;
            var fields = definition.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
            if (fields.TryGetValue("startingLevel", out var startingLevelField)
                && int.TryParse(startingLevelField.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var configuredStartingLevel))
            {
                startingLevel = configuredStartingLevel;
            }

            if (fields.TryGetValue("autoAdvanceLevels", out var autoAdvanceField)
                && bool.TryParse(autoAdvanceField.DefaultValue, out var configuredAutoAdvance))
            {
                autoAdvance = configuredAutoAdvance;
            }

            if (fields.TryGetValue("levelProfilesJson", out var profilesField)
                && !string.IsNullOrWhiteSpace(profilesField.DefaultValue))
            {
                try
                {
                    var configuredLevels = JsonSerializer.Deserialize<List<CouncilGameLevelProfile>>(
                        profilesField.DefaultValue,
                        campaignJsonOptions);
                    if (configuredLevels is { Count: > 0 })
                        levels = NormalizeLevelProfiles(configuredLevels);
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(
                        exception,
                        "ASCII DOOM campaign runtime class {RuntimeClassKey} contains invalid levelProfilesJson.",
                        definition.Key);
                    throw new InvalidOperationException($"ASCII DOOM campaign runtime class '{definition.Key}' contains invalid levelProfilesJson.", exception);
                }
            }

            if (levels.Count == 0)
                throw new InvalidOperationException($"ASCII DOOM campaign runtime class '{definition.Key}' must contain at least one level profile.");

            if (request.StartingLevel is int requestedStartingLevel)
                startingLevel = requestedStartingLevel;
            if (request.AutoAdvanceLevels is bool requestedAutoAdvance)
                autoAdvance = requestedAutoAdvance;

            levels = NormalizeLevelProfiles(levels);
            var startingIndex = levels.FindIndex(level => level.Level == Math.Clamp(startingLevel, 1, 99));
            if (startingIndex < 0)
                startingIndex = Math.Clamp(startingLevel - 1, 0, levels.Count - 1);

            return new DoomCampaignSettings(resolvedKey, levels, startingIndex, autoAdvance);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the configurable ASCII DOOM campaign runtime class failed.");
            throw;
        }
    }

    /// <summary>Recognizes the maintained field contract instead of inventing game difficulty from arbitrary State classes.</summary>
    private bool IsDoomCampaignDefinition(CouncilRuntimeClassDefinition definition)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (definition.Kind != RuntimeClassKind.State)
                return false;
            var names = definition.Fields
                .Select(field => field.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return names.Contains("startingLevel")
                && names.Contains("autoAdvanceLevels")
                && names.Contains("levelProfilesJson");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inspecting an ASCII DOOM campaign runtime-class contract failed.");
            throw;
        }
    }

    /// <summary>Normalizes user-edited runtime-class level profiles into deterministic, bounded engine inputs.</summary>
    private List<CouncilGameLevelProfile> NormalizeLevelProfiles(IEnumerable<CouncilGameLevelProfile> source)
    {
        try
        {
            var normalized = source
                .Where(level => level is not null)
                .Select(CloneLevelProfile)
                .OrderBy(level => level.Level)
                .Take(32)
                .ToList();
            if (normalized.Count == 0)
                throw new InvalidOperationException("An ASCII DOOM campaign must contain at least one level profile.");

            for (var index = 0; index < normalized.Count; index++)
            {
                var level = normalized[index];
                level.Level = Math.Clamp(level.Level <= 0 ? index + 1 : level.Level, 1, 99);
                level.Name = string.IsNullOrWhiteSpace(level.Name) ? $"Corridor {level.Level:00}" : level.Name.Trim();
                if (level.Name.Length > 80)
                    level.Name = level.Name[..80];
                level.Difficulty = Math.Clamp(level.Difficulty <= 0 ? Math.Min(10, index + 1) : level.Difficulty, 1, 10);
                level.MapWidth = Math.Clamp(level.MapWidth, 20, 96);
                level.MapHeight = Math.Clamp(level.MapHeight, 14, 64);
                level.RoomCount = Math.Clamp(level.RoomCount, 3, 18);
                level.EnemyDensity = Math.Clamp(level.EnemyDensity, .10d, 3.00d);
                level.EnemyHealthMultiplier = Math.Clamp(level.EnemyHealthMultiplier, .25d, 4.00d);
                level.EnemyDamageMultiplier = Math.Clamp(level.EnemyDamageMultiplier, .25d, 4.00d);
                level.StartingHealth = Math.Clamp(level.StartingHealth, 25, 250);
                level.StartingAmmo = Math.Clamp(level.StartingAmmo, 1, 250);
            }
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing ASCII DOOM campaign level profiles failed.");
            throw;
        }
    }

    /// <summary>Creates an independent level-profile copy so runtime state never mutates stored class definitions.</summary>
    private CouncilGameLevelProfile CloneLevelProfile(CouncilGameLevelProfile source)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            return new CouncilGameLevelProfile
            {
                Level = source.Level,
                Name = source.Name,
                Difficulty = source.Difficulty,
                MapWidth = source.MapWidth,
                MapHeight = source.MapHeight,
                RoomCount = source.RoomCount,
                EnemyDensity = source.EnemyDensity,
                EnemyHealthMultiplier = source.EnemyHealthMultiplier,
                EnemyDamageMultiplier = source.EnemyDamageMultiplier,
                StartingHealth = source.StartingHealth,
                StartingAmmo = source.StartingAmmo
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying an ASCII DOOM campaign level profile failed.");
            throw;
        }
    }

    /// <summary>Gets the active normalized level profile, falling back to the first starter level for legacy sessions.</summary>
    private CouncilGameLevelProfile GetCurrentLevelProfile(CouncilGameSessionState session)
    {
        try
        {
            if (session.LevelProfiles.Count == 0)
                throw new InvalidOperationException("The active ASCII DOOM campaign session has no configured level profiles.");
            session.CurrentLevelIndex = Math.Clamp(session.CurrentLevelIndex, 0, session.LevelProfiles.Count - 1);
            return session.LevelProfiles[session.CurrentLevelIndex];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the active ASCII DOOM campaign level failed.");
            throw;
        }
    }

    /// <summary>Derives a stable per-level map seed from the campaign seed, level number and optional scenario.</summary>
    private int DeriveLevelSeed(CouncilGameSessionState session, CouncilGameLevelProfile level)
    {
        try
        {
            var campaignSeed = session.CampaignSeed > 0
                ? session.CampaignSeed
                : session.MapSeed > 0
                    ? session.MapSeed
                    : BitConverter.ToInt32(session.Id.ToByteArray(), 0) & int.MaxValue;
            var material = $"{Math.Max(1, campaignSeed)}|{level.Level}|{session.ScenarioPrompt}";
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material));
            return Math.Max(1, BitConverter.ToInt32(bytes, 0) & int.MaxValue);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deriving the deterministic ASCII DOOM level seed failed.");
            throw;
        }
    }
}
