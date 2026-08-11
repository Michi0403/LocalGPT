using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Provides council runtime class service operations.
/// </summary>
public sealed class CouncilRuntimeClassService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<CouncilRuntimeClassService> logger) : ICouncilRuntimeClassService
{
    private const int CurrentSeedVersion = 4;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Gets definitions async.
    /// </summary>
    public async Task<IReadOnlyList<CouncilRuntimeClassDefinition>> GetDefinitionsAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSeedDataAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.CouncilRuntimeClassConfigurations.AsNoTracking();
            if (!includeDisabled)
                query = query.Where(item => item.IsEnabled);
            var rows = await query
                .OrderBy(item => item.Namespace)
                .ThenBy(item => item.DisplayName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows.Select(ToDefinition).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read Council runtime class definitions.");
            throw;
        }
    }

    /// <summary>
    /// Finds async.
    /// </summary>
    public async Task<CouncilRuntimeClassDefinition?> FindAsync(
        string? key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        try
        {
            await EnsureSeedDataAsync(cancellationToken).ConfigureAwait(false);
            var normalized = NormalizeLookupToken(key);
            var definitions = await GetDefinitionsAsync(includeDisabled: true, cancellationToken).ConfigureAwait(false);
            return definitions
                .Select(definition => new
                {
                    Definition = definition,
                    Rank = LookupRank(definition, normalized)
                })
                .Where(candidate => candidate.Rank < int.MaxValue)
                .OrderBy(candidate => candidate.Rank)
                .ThenBy(candidate => candidate.Definition.Key.Length)
                .Select(candidate => candidate.Definition)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not find Council runtime class {RuntimeClassKey}.", key);
            throw;
        }
    }

    /// <summary>
    /// Saves async.
    /// </summary>
    public async Task<CouncilRuntimeClassDefinition> SaveAsync(
        SaveCouncilRuntimeClassRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed)
            throw new InvalidOperationException("Explicit user confirmation is required before a runtime class is saved.");

        try
        {
            NormalizeAndValidate(request.Definition);
            await EnsureSeedDataAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.CouncilRuntimeClassConfigurations
                .SingleOrDefaultAsync(item => item.Key == request.Definition.Key, cancellationToken)
                .ConfigureAwait(false);
            if (row is null)
            {
                row = new CouncilRuntimeClassConfiguration
                {
                    Id = request.Definition.Id == Guid.Empty ? Guid.NewGuid() : request.Definition.Id,
                    Key = request.Definition.Key,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.CouncilRuntimeClassConfigurations.Add(row);
            }

            ApplyDefinition(row, request.Definition);
            row.IsSystemSeed = row.IsSystemSeed && request.Definition.IsSystemSeed;
            row.IsUserModified = true;
            row.IsEnabled = request.Definition.IsEnabled;
            row.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved Council runtime class {RuntimeClassKey}.", row.Key);
            return ToDefinition(row);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException)
        {
            logger.LogWarning(ex, "Council runtime class save was rejected.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not save Council runtime class; payload content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Ensures seed data async.
    /// </summary>
    private async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var existingRows = await db.CouncilRuntimeClassConfigurations
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var existing = existingRows.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
            var changed = false;

            foreach (var definition in CreateSeedDefinitions())
            {
                NormalizeAndValidate(definition);
                if (!existing.TryGetValue(definition.Key, out var row))
                {
                    row = new CouncilRuntimeClassConfiguration
                    {
                        Id = definition.Id,
                        Key = definition.Key,
                        IsSystemSeed = true,
                        IsUserModified = false,
                        IsEnabled = true,
                        SeedVersion = CurrentSeedVersion,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    ApplyDefinition(row, definition);
                    db.CouncilRuntimeClassConfigurations.Add(row);
                    existing.Add(row.Key, row);
                    changed = true;
                    continue;
                }

                if (row.IsSystemSeed && !row.IsUserModified && row.SeedVersion < CurrentSeedVersion)
                {
                    var enabled = row.IsEnabled;
                    ApplyDefinition(row, definition);
                    row.IsEnabled = enabled;
                    row.SeedVersion = CurrentSeedVersion;
                    row.UpdatedAtUtc = DateTime.UtcNow;
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Seeded or upgraded Council runtime class definitions to version {SeedVersion}.", CurrentSeedVersion);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(EnsureSeedDataAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(EnsureSeedDataAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates seed definitions.
    /// </summary>
    private IReadOnlyList<CouncilRuntimeClassDefinition> CreateSeedDefinitions()
    {
    try
    {
            var doomSource = "https://github.com/id-Software/DOOM";
            var dragonSource = "https://github.com/lotgd/lotgd";
            var cLanguageSource = "https://github.com/llvm/llvm-project";
            var phpLanguageSource = "https://github.com/php/doc-en";
            return
            [
                BuildDefinition("games.ascii.doom.session", "LocalGPT.Games.AsciiDoom", "ASCII DOOM session", RuntimeClassKind.Session,
                    "Turn-based Council session state. This does not start a traditional 3D renderer; it advances one meaningful action and one AI-authored ASCII frame per Council turn.",
                    [
                        Field("levelName", "Level name", "string", "E1M1-inspired", RuntimeFieldInputMode.Shared, true, true),
                        Field("turn", "Turn", "int", "0", RuntimeFieldInputMode.System, false, false),
                        Field("stepScale", "World step scale", "int", "4", RuntimeFieldInputMode.Shared, true, true),
                        Field("status", "Session status", "string", "Running", RuntimeFieldInputMode.System, false, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.start", "localgpt.game.session.get", "localgpt.knowledge.list"], [doomSource, cLanguageSource]),
                BuildDefinition("games.ascii.doom.map", "LocalGPT.Games.AsciiDoom", "ASCII DOOM map", RuntimeClassKind.Map,
                    "A generated room-and-corridor graph kept as authoritative turn state. The Council may study the open source code, but does not require or redistribute commercial WAD data.",
                    [
                        Field("width", "Map width", "int", "64", RuntimeFieldInputMode.Ai, true, false),
                        Field("height", "Map height", "int", "32", RuntimeFieldInputMode.Ai, true, false),
                        Field("roomsJson", "Rooms", "json", "[]", RuntimeFieldInputMode.Ai, true, false),
                        Field("currentRoomId", "Current room", "string", "start", RuntimeFieldInputMode.System, false, false),
                        Field("exitRoomId", "Exit room", "string", "exit", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.start", "localgpt.game.session.get"], [doomSource, cLanguageSource]),
                BuildDefinition("games.ascii.doom.player", "LocalGPT.Games.AsciiDoom", "ASCII DOOM player", RuntimeClassKind.Player,
                    "Player state and bounded actions. Human input is optional; required fields can block only the dependent round.",
                    [
                        Field("name", "Call sign", "string", "Marine", RuntimeFieldInputMode.HumanOptional, true, true),
                        Field("health", "Health", "int", "100", RuntimeFieldInputMode.System, false, false),
                        Field("armor", "Armor", "int", "0", RuntimeFieldInputMode.System, false, false),
                        Field("facing", "Facing", "string", "north", RuntimeFieldInputMode.Shared, true, true),
                        Field("action", "Next action", "string", "wait", RuntimeFieldInputMode.HumanOptional, true, true, false, false, "Space", "A")
                    ],
                    [
                        Binding("move-forward", "Move forward", "W", "LeftStickUp"),
                        Binding("move-back", "Move back", "S", "LeftStickDown"),
                        Binding("turn-left", "Turn left", "A", "LeftStickLeft"),
                        Binding("turn-right", "Turn right", "D", "LeftStickRight"),
                        Binding("attack", "Attack", "Space", "RightTrigger"),
                        Binding("use", "Use", "E", "X"),
                        Binding("duck", "Duck", "Ctrl", "B")
                    ], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.control", "localgpt.game.session.get"], [doomSource, cLanguageSource]),
                BuildDefinition("games.ascii.doom.controller", "LocalGPT.Games.AsciiDoom", "ASCII DOOM shared controller", RuntimeClassKind.Controller,
                    "The single input contract used by both the human player and the AI Player Controller. It preserves the 2.5D x/y facing experience without a separate AI-only shortcut.",
                    [
                        Field("action", "Action", "string", "wait", RuntimeFieldInputMode.Shared, true, true),
                        Field("axisX", "Movement X axis", "double", "0", RuntimeFieldInputMode.Shared, true, true),
                        Field("axisY", "Movement Y axis", "double", "0", RuntimeFieldInputMode.Shared, true, true),
                        Field("aimX", "Aim X", "int", "0", RuntimeFieldInputMode.Shared, true, true),
                        Field("aimY", "Aim Y", "int", "0", RuntimeFieldInputMode.Shared, true, true),
                        Field("source", "Control source", "string", "Human", RuntimeFieldInputMode.System, false, false)
                    ],
                    [
                        Binding("move-forward", "Move forward", "W", "LeftStickUp"),
                        Binding("move-backward", "Move backward", "S", "LeftStickDown"),
                        Binding("strafe-left", "Strafe left", "A", "LeftStickLeft"),
                        Binding("strafe-right", "Strafe right", "D", "LeftStickRight"),
                        Binding("turn-left", "Turn or aim left", "Q", "RightStickLeft"),
                        Binding("turn-right", "Turn or aim right", "R", "RightStickRight"),
                        Binding("shoot", "Shoot on current x/y facing ray", "Space", "RightTrigger"),
                        Binding("use", "Use door or switch", "E", "A"),
                        Binding("duck", "Duck or stand", "Ctrl", "B")
                    ], ["localgpt.game.control", "localgpt.game.session.get", "localgpt.runtime-class.resolve"], [doomSource, cLanguageSource]),
                BuildDefinition("games.ascii.doom.director", "LocalGPT.Games.AsciiDoom", "ASCII corridor GameDirector", RuntimeClassKind.Controller,
                    "The authoritative game-engine boundary. Controllers and actor Councils submit proposals; only this runtime class may approve one world-state transition.",
                    [
                        Field("mode", "Director mode", "string", "Deterministic", RuntimeFieldInputMode.Shared, true, true),
                        Field("modelName", "Optional low-parameter reviewer", "string", "qwen3.5:0.8b", RuntimeFieldInputMode.Shared, true, true),
                        Field("expectedTurn", "Expected turn", "long", "0", RuntimeFieldInputMode.System, false, false),
                        Field("decision", "Last decision", "json", "{}", RuntimeFieldInputMode.System, false, false)
                    ], [], ["localgpt.game.control.preview", "localgpt.game.control", "localgpt.game.session.get", "localgpt.runtime-class.resolve"], [cLanguageSource]),
                BuildDefinition("games.ascii.doom.creature", "LocalGPT.Games.AsciiDoom", "ASCII corridor creature Council member", RuntimeClassKind.Actor,
                    "One creature runtime instance assigned to one bounded Council member or creature subdirector. It predicts an intent but cannot mutate authoritative state.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("archetype", "Creature archetype", "string", "patrol", RuntimeFieldInputMode.Ai, true, false),
                        Field("councilRoleKey", "Council role key", "string", "creature-council", RuntimeFieldInputMode.System, false, false),
                        Field("roomId", "Room", "string", "start", RuntimeFieldInputMode.System, false, false),
                        Field("intent", "Proposed intent", "string", "observe", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.game.control.preview", "localgpt.game.session.get", "localgpt.runtime-class.resolve"], [cLanguageSource]),
                BuildDefinition("games.ascii.doom.reactive-object", "LocalGPT.Games.AsciiDoom", "ASCII corridor reactive map object", RuntimeClassKind.Actor,
                    "A door, switch, pickup, trigger or hazard with its own bounded runtime state. It proposes a reaction after range and trigger checks.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("objectType", "Object type", "string", "door", RuntimeFieldInputMode.Ai, true, false),
                        Field("state", "Object state", "string", "idle", RuntimeFieldInputMode.System, false, false),
                        Field("trigger", "Trigger contract", "string", "use-or-enter", RuntimeFieldInputMode.Shared, true, true),
                        Field("reaction", "Proposed reaction", "string", "none", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.game.control.preview", "localgpt.game.session.get", "localgpt.runtime-class.resolve"], [cLanguageSource]),
                BuildDefinition("games.ascii.doom.actor", "LocalGPT.Games.AsciiDoom", "ASCII DOOM world actor", RuntimeClassKind.Actor,
                    "One active enemy, ally, projectile abstraction, pickup, door or hazard owned by one Council member for the current turn.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("actorType", "Actor type", "string", "enemy", RuntimeFieldInputMode.Ai, true, false),
                        Field("glyph", "ASCII glyph", "string", "e", RuntimeFieldInputMode.Ai, true, false),
                        Field("roomId", "Room", "string", "start", RuntimeFieldInputMode.System, false, false),
                        Field("health", "Health", "int", "25", RuntimeFieldInputMode.System, false, false),
                        Field("intent", "Turn intent", "string", "observe", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [doomSource, cLanguageSource]),
                BuildDefinition("games.ascii.doom.frame", "LocalGPT.Games.AsciiDoom", "ASCII DOOM frame", RuntimeClassKind.Frame,
                    "Exactly one Council member authors the complete fixed-width frame after state resolution. It is a Matrix-ship-style terminal view, not a conventional 3D game frame.",
                    [
                        Field("width", "Columns", "int", "80", RuntimeFieldInputMode.Shared, true, true),
                        Field("height", "Rows", "int", "25", RuntimeFieldInputMode.Shared, true, true),
                        Field("frameText", "ASCII frame", "string", "", RuntimeFieldInputMode.Ai, true, false),
                        Field("turn", "Turn", "int", "0", RuntimeFieldInputMode.System, false, false),
                        Field("legend", "Legend", "string", "@ player, e enemy, + door, # wall", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.frame.submit", "localgpt.game.session.get"], [doomSource, cLanguageSource]),
                BuildDefinition("games.green-dragon.world", "LocalGPT.Games.GreenDragon", "Green Dragon world", RuntimeClassKind.World,
                    "Persistent role-play world state orchestrated by a Story Director. Locations, houses, NPCs and events remain separate runtime class instances.",
                    [
                        Field("chapter", "Chapter", "string", "Arrival", RuntimeFieldInputMode.Shared, true, true),
                        Field("day", "World day", "int", "1", RuntimeFieldInputMode.System, false, false),
                        Field("weather", "Weather", "string", "clear", RuntimeFieldInputMode.Ai, true, false),
                        Field("storyFlagsJson", "Story flags", "json", "{}", RuntimeFieldInputMode.System, false, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.start", "localgpt.game.session.get", "localgpt.knowledge.list"], [dragonSource, phpLanguageSource]),
                BuildDefinition("games.green-dragon.location", "LocalGPT.Games.GreenDragon", "Green Dragon location or house", RuntimeClassKind.Location,
                    "A village, forest, inn, house or other place. One Council member may act as the active location and expose its available actions.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("name", "Name", "string", "Village square", RuntimeFieldInputMode.Ai, true, false),
                        Field("kind", "Location kind", "string", "village", RuntimeFieldInputMode.Ai, true, false),
                        Field("description", "Description", "string", "", RuntimeFieldInputMode.Ai, true, false),
                        Field("exitsJson", "Exits", "json", "[]", RuntimeFieldInputMode.Ai, true, false),
                        Field("availableActionsJson", "Available actions", "json", "[]", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [dragonSource, phpLanguageSource]),
                BuildDefinition("games.green-dragon.npc", "LocalGPT.Games.GreenDragon", "Green Dragon NPC", RuntimeClassKind.Actor,
                    "One named NPC instance played by one Council member while active. The Story Director coordinates but does not overwrite the NPC's bounded decisions.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("name", "Name", "string", "Traveller", RuntimeFieldInputMode.Ai, true, false),
                        Field("role", "Role", "string", "villager", RuntimeFieldInputMode.Ai, true, false),
                        Field("mood", "Mood", "string", "neutral", RuntimeFieldInputMode.Ai, true, false),
                        Field("dialogueIntent", "Dialogue intent", "string", "greet", RuntimeFieldInputMode.Ai, true, false),
                        Field("locationId", "Location", "string", "village", RuntimeFieldInputMode.System, false, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [dragonSource, phpLanguageSource]),
                BuildDefinition("games.green-dragon.event", "LocalGPT.Games.GreenDragon", "Green Dragon event or encounter", RuntimeClassKind.Event,
                    "A bounded story beat, encounter or random event with explicit entry conditions, choices and completion state.",
                    [
                        Field("instanceId", "Instance id", "string", "", RuntimeFieldInputMode.System, false, false),
                        Field("title", "Title", "string", "A rustle in the forest", RuntimeFieldInputMode.Ai, true, false),
                        Field("trigger", "Trigger", "string", "enter forest", RuntimeFieldInputMode.Ai, true, false),
                        Field("choicesJson", "Choices", "json", "[]", RuntimeFieldInputMode.Ai, true, false),
                        Field("selectedChoice", "Selected choice", "string", "", RuntimeFieldInputMode.HumanOptional, true, true, false, false, "1", "A"),
                        Field("completed", "Completed", "bool", "false", RuntimeFieldInputMode.System, false, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [dragonSource, phpLanguageSource]),
                BuildDefinition("games.green-dragon.player", "LocalGPT.Games.GreenDragon", "Green Dragon player", RuntimeClassKind.Player,
                    "Player character and current choice. Human participation is optional unless a configured field is marked HumanRequired.",
                    [
                        Field("name", "Name", "string", "Adventurer", RuntimeFieldInputMode.HumanOptional, true, true),
                        Field("health", "Health", "int", "100", RuntimeFieldInputMode.System, false, false),
                        Field("gold", "Gold", "int", "0", RuntimeFieldInputMode.System, false, false),
                        Field("locationId", "Location", "string", "village", RuntimeFieldInputMode.System, false, false),
                        Field("action", "Next action", "string", "look", RuntimeFieldInputMode.HumanOptional, true, true, false, false, "Enter", "A")
                    ],
                    [
                        Binding("choice-1", "Choice 1", "1", "A"),
                        Binding("choice-2", "Choice 2", "2", "B"),
                        Binding("choice-3", "Choice 3", "3", "X"),
                        Binding("choice-4", "Choice 4", "4", "Y"),
                        Binding("look", "Look", "L", "RightStick")
                    ], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [dragonSource, phpLanguageSource]),
                BuildDefinition("games.green-dragon.frame", "LocalGPT.Games.GreenDragon", "Green Dragon ASCII scene", RuntimeClassKind.Frame,
                    "One AI-authored terminal scene per completed story turn, followed by concise narration and numbered choices.",
                    [
                        Field("width", "Columns", "int", "80", RuntimeFieldInputMode.Shared, true, true),
                        Field("height", "Rows", "int", "25", RuntimeFieldInputMode.Shared, true, true),
                        Field("frameText", "ASCII frame", "string", "", RuntimeFieldInputMode.Ai, true, false),
                        Field("caption", "Caption", "string", "", RuntimeFieldInputMode.Ai, true, false)
                    ], [], ["localgpt.runtime-class.get", "localgpt.runtime-class.resolve", "localgpt.game.session.get"], [dragonSource, phpLanguageSource])
            ];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(CreateSeedDefinitions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(CreateSeedDefinitions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds definition.
    /// </summary>
    private CouncilRuntimeClassDefinition BuildDefinition(
        string key,
        string runtimeNamespace,
        string displayName,
        RuntimeClassKind kind,
        string description,
        List<RuntimeClassFieldDefinition> fields,
        List<RuntimeInputBindingDefinition> inputBindings,
        List<string> recommendedDxFunctions,
        List<string> sourceReferences) {
    try
    {
        return new()
    {
        Id = DeterministicGuid(key),
        Key = key,
        Namespace = runtimeNamespace,
        DisplayName = displayName,
        Kind = kind,
        Description = description,
        Fields = fields,
        InputBindings = inputBindings,
        RecommendedDxFunctions = recommendedDxFunctions,
        SourceReferences = sourceReferences,
        IsEnabled = true,
        IsSystemSeed = true
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildDefinition)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the field operation.
    /// </summary>
    private RuntimeClassFieldDefinition Field(
        string name,
        string displayName,
        string dataType,
        string defaultValue,
        RuntimeFieldInputMode inputMode,
        bool aiAssignable,
        bool humanAssignable,
        bool blocks = false,
        bool required = false,
        string keyboardKey = "",
        string gamepadButton = "") {
    try
    {
        return new()
    {
        Name = name,
        DisplayName = displayName,
        DataType = dataType,
        DefaultValue = defaultValue,
        InputMode = inputMode,
        AiAssignable = aiAssignable,
        HumanAssignable = humanAssignable,
        BlocksNextRoundUntilHumanInput = blocks,
        IsRequired = required,
        KeyboardKey = keyboardKey,
        GamepadButton = gamepadButton
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(Field)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(Field)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the binding operation.
    /// </summary>
    private RuntimeInputBindingDefinition Binding(
        string action,
        string displayName,
        string keyboardKey,
        string gamepadButton) {
    try
    {
        return new()
    {
        Action = action,
        DisplayName = displayName,
        KeyboardKey = keyboardKey,
        GamepadButton = gamepadButton
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(Binding)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(Binding)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the deterministic guid operation.
    /// </summary>
    private Guid DeterministicGuid(string value)
    {
    try
    {
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return new Guid(hash.AsSpan(0, 16));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(DeterministicGuid)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(DeterministicGuid)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes and validate.
    /// </summary>
    private void NormalizeAndValidate(CouncilRuntimeClassDefinition definition)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentException.ThrowIfNullOrWhiteSpace(definition.Key);
            definition.Key = definition.Key.Trim().ToLowerInvariant();
            definition.Namespace = string.IsNullOrWhiteSpace(definition.Namespace) ? "LocalGPT.Runtime" : definition.Namespace.Trim();
            definition.DisplayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Key : definition.DisplayName.Trim();
            definition.Description = definition.Description?.Trim() ?? string.Empty;
            definition.Fields ??= [];
            definition.InputBindings ??= [];
            definition.Aliases ??= [];
            definition.RecommendedDxFunctions ??= [];
            definition.SourceReferences ??= [];
            if (definition.Fields.Count > 128)
                throw new InvalidOperationException("A runtime class can contain at most 128 fields.");
            if (definition.InputBindings.Count > 64)
                throw new InvalidOperationException("A runtime class can contain at most 64 input bindings.");

            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in definition.Fields)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(field.Name);
                field.Name = field.Name.Trim();
                if (!fieldNames.Add(field.Name))
                    throw new InvalidOperationException($"Runtime class field '{field.Name}' is duplicated.");
                field.DisplayName = string.IsNullOrWhiteSpace(field.DisplayName) ? field.Name : field.DisplayName.Trim();
                field.DataType = string.IsNullOrWhiteSpace(field.DataType) ? "string" : field.DataType.Trim();
                field.DefaultValue ??= string.Empty;
                field.Description ??= string.Empty;
                field.KeyboardKey = field.KeyboardKey?.Trim() ?? string.Empty;
                field.GamepadButton = field.GamepadButton?.Trim() ?? string.Empty;
                field.AllowedValuesJson = string.IsNullOrWhiteSpace(field.AllowedValuesJson) ? "[]" : field.AllowedValuesJson.Trim();
                if (field.BlocksNextRoundUntilHumanInput && !field.HumanAssignable)
                    throw new InvalidOperationException($"Runtime class field '{field.Name}' cannot block for human input when HumanAssignable is false.");
                if (field.InputMode == RuntimeFieldInputMode.HumanRequired)
                {
                    field.HumanAssignable = true;
                    field.IsRequired = true;
                    field.BlocksNextRoundUntilHumanInput = true;
                }
            }

            definition.Aliases = BuildAliases(definition).ToList();
            definition.RecommendedDxFunctions = definition.RecommendedDxFunctions
                .Select(item => item?.Trim() ?? string.Empty)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            definition.SourceReferences = definition.SourceReferences
                .Select(item => item?.Trim() ?? string.Empty)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(NormalizeAndValidate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(NormalizeAndValidate)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes lookup token.
    /// </summary>
    private string NormalizeLookupToken(string value) {
    try
    {
        return new(
        (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(NormalizeLookupToken)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(NormalizeLookupToken)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the lookup rank operation.
    /// </summary>
    private int LookupRank(CouncilRuntimeClassDefinition definition, string normalized)
    {
    try
    {
            if (normalized.Length == 0) return int.MaxValue;
            var candidates = BuildAliases(definition)
                .Select(NormalizeLookupToken)
                .Where(candidate => candidate.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (NormalizeLookupToken(definition.Key) == normalized) return 0;
            if (candidates.Contains(normalized, StringComparer.Ordinal)) return 1;
            if (candidates.Any(candidate => candidate.EndsWith(normalized, StringComparison.Ordinal))) return 2;
            if (candidates.Any(candidate => candidate.Contains(normalized, StringComparison.Ordinal))) return 3;
            return int.MaxValue;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(LookupRank)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(LookupRank)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds aliases.
    /// </summary>
    private IEnumerable<string> BuildAliases(CouncilRuntimeClassDefinition definition) {
    try
    {
        return BuildAliases(definition.Key, definition.Namespace, definition.DisplayName)
            .Concat(definition.Aliases ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildAliases)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildAliases)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds aliases.
    /// </summary>
    private IEnumerable<string> BuildAliases(string key, string runtimeNamespace, string displayName)
    {
    try
    {
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                key,
                runtimeNamespace,
                displayName,
                key.Replace('.', '-'),
                key.Replace('.', ' '),
                runtimeNamespace.Replace("LocalGPT.Games.", "games.", StringComparison.OrdinalIgnoreCase),
                runtimeNamespace.Replace("LocalGPT.", string.Empty, StringComparison.OrdinalIgnoreCase)
            };
            if (key.StartsWith("games.ascii.doom.", StringComparison.OrdinalIgnoreCase))
            {
                aliases.Add("doom." + key["games.ascii.doom.".Length..]);
                aliases.Add("ascii-doom-" + key["games.ascii.doom.".Length..]);
                aliases.Add("LocalGPT.Games.AsciiDoom." + key["games.ascii.doom.".Length..]);
            }
            if (key.StartsWith("games.green-dragon.", StringComparison.OrdinalIgnoreCase))
            {
                aliases.Add("lotgd." + key["games.green-dragon.".Length..]);
                aliases.Add("green-dragon-" + key["games.green-dragon.".Length..]);
                aliases.Add("LocalGPT.Games.GreenDragon." + key["games.green-dragon.".Length..]);
            }
            return aliases.Where(alias => !string.IsNullOrWhiteSpace(alias));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildAliases)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(BuildAliases)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies definition.
    /// </summary>
    private void ApplyDefinition(CouncilRuntimeClassConfiguration row, CouncilRuntimeClassDefinition definition)
    {
    try
    {
            row.Key = definition.Key;
            row.Namespace = definition.Namespace;
            row.DisplayName = definition.DisplayName;
            row.Kind = definition.Kind.ToString();
            row.Description = definition.Description;
            row.FieldsJson = JsonSerializer.Serialize(definition.Fields, jsonOptions);
            row.InputBindingsJson = JsonSerializer.Serialize(definition.InputBindings, jsonOptions);
            row.RecommendedDxFunctionsJson = JsonSerializer.Serialize(definition.RecommendedDxFunctions, jsonOptions);
            row.SourceReferencesJson = JsonSerializer.Serialize(definition.SourceReferences, jsonOptions);
            row.IsEnabled = definition.IsEnabled;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(ApplyDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(ApplyDefinition)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the to definition operation.
    /// </summary>
    private CouncilRuntimeClassDefinition ToDefinition(CouncilRuntimeClassConfiguration row)
    {
    try
    {
            if (!Enum.TryParse<RuntimeClassKind>(row.Kind, ignoreCase: true, out var kind))
                kind = RuntimeClassKind.State;
            return new CouncilRuntimeClassDefinition
            {
                Id = row.Id,
                Key = row.Key,
                Namespace = row.Namespace,
                DisplayName = row.DisplayName,
                Kind = kind,
                Description = row.Description,
                Fields = Deserialize<List<RuntimeClassFieldDefinition>>(row.FieldsJson) ?? [],
                InputBindings = Deserialize<List<RuntimeInputBindingDefinition>>(row.InputBindingsJson) ?? [],
                Aliases = BuildAliases(row.Key, row.Namespace, row.DisplayName).ToList(),
                RecommendedDxFunctions = Deserialize<List<string>>(row.RecommendedDxFunctionsJson) ?? [],
                SourceReferences = Deserialize<List<string>>(row.SourceReferencesJson) ?? [],
                IsEnabled = row.IsEnabled,
                IsSystemSeed = row.IsSystemSeed,
                IsUserModified = row.IsUserModified
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(ToDefinition)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeClassService)}.{nameof(ToDefinition)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the deserialize operation.
    /// </summary>
    private T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "A runtime class JSON column could not be read; an empty value is used.");
            return default;
        }
    }
}
