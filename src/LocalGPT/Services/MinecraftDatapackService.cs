using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>Owns Minecraft datapack content generation and the version-to-pack-format catalog.</summary>
    public sealed partial class MinecraftDatapackService
    {
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="MinecraftDatapackService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;
        /// <summary>
        /// Stores the council text pattern data service dependency used by <see cref="MinecraftDatapackService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilTextPatternDataService patterns;
        /// <summary>
        /// Stores the council text service dependency used by <see cref="MinecraftDatapackService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilTextService _text;
        /// <summary>
        /// Stores the JSON text service dependency used by <see cref="MinecraftDatapackService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IJsonTextService jsonText;
        /// <summary>
        /// Stores the logger used by <see cref="MinecraftDatapackService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<MinecraftDatapackService> serviceLogger;

        /// <summary>Creates the Minecraft datapack domain service with persisted pattern and JSON policy collaborators.</summary>
        public MinecraftDatapackService(
            LocalGptCatalogService catalog,
            ICouncilTextPatternDataService patterns,
            CouncilTextService text,
            IJsonTextService jsonText,
            ILogger<MinecraftDatapackService> serviceLogger)
        {
            this.catalog = catalog;
            this.patterns = patterns;
            _text = text;
            this.jsonText = jsonText;
            this.serviceLogger = serviceLogger;
        }

        /// <summary>
        /// Creates city charter model as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateCityCharterModel() {
    try
    {
        return """
            {
              "parent": "minecraft:item/generated",
              "textures": {
                "layer0": "minecraft:item/paper"
              }
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateCityCharterModel)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateCityCharterModel)} failed.");
        throw;
    }
}
        /// <summary>
        /// Retrieves pack format JSON value as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minecraftVersion">Minecraft version value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GetPackFormatJsonValue(string minecraftVersion, ILogger logger)
        {
            try
            {
                var packFormat = MinecraftDatapackVersionInfoResolve(minecraftVersion, logger).PackFormat;
                return packFormat.Contains('.', StringComparison.Ordinal)
                    ? $"\"{packFormat}\""
                    : packFormat;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetPackFormatJsonValue minecraftVersion {minecraftVersion.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates function tag as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modId">Identifier of the mod to use for this operation.</param>
        /// <param name="functionName">Function name value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFunctionTag(string modId, string functionName) {
    try
    {
        return $$"""
            {
              "values": [
                "{{modId}}:{{functionName}}"
              ]
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateFunctionTag)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateFunctionTag)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack load function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackLoadFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            # Living Cities 0.1 - core load
            # Re-running /reload may print "already exists" warnings for objectives; that is harmless.
            scoreboard objectives add lc_year dummy "LC Year"
            scoreboard objectives add lc_population dummy "LC Population"
            scoreboard objectives add lc_food dummy "LC Food"
            scoreboard objectives add lc_security dummy "LC Security"
            scoreboard objectives add lc_prestige dummy "LC Prestige"
            scoreboard objectives add lc_birth_year dummy "LC Birth Year"
            scoreboard objectives add lc_scan_timer dummy "LC Scan Timer"
            scoreboard objectives add lc_menu trigger "Living Cities"
            scoreboard objectives add lc_buildings dummy "LC Buildings"
            scoreboard objectives add lc_tmp dummy "LC Temp"

            scoreboard players set #year lc_year 1
            scoreboard players set #population lc_population 0
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #prestige lc_prestige 0
            scoreboard players set #tick lc_scan_timer 0
            scoreboard players set #houses lc_buildings 0
            scoreboard players set #workplaces lc_buildings 0
            scoreboard players set #registered_this_scan lc_population 0

            data merge storage {{{context.ModId}}}:city {founded:0b,year:1,population:0,food:100,security:100,prestige:0,houses:0,workplaces:0}
            data merge storage {{{context.ModId}}}:chronicle {events:[]}
            data merge storage {{{context.ModId}}}:personalities {notables:[]}

            function {{{context.ModId}}}:buildings/init
            function {{{context.ModId}}}:city/register_banner
            tellraw @a [{"text":"[Living Cities] ","color":"green"},{"text":"Datapack loaded. Use /function {{{context.ModId}}}:ui/townhall or the admin book."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackLoadFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackLoadFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack tick function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackTickFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            # Living Cities tick stays small: menu handling every tick, simulation every 5 seconds.
            execute as @a[tag=!lc_received_book] run function {{{context.ModId}}}:ui/give_admin_book
            scoreboard players enable @a lc_menu

            execute as @a[scores={lc_menu=1}] at @s run function {{{context.ModId}}}:city/create
            execute as @a[scores={lc_menu=2}] at @s run function {{{context.ModId}}}:ui/status
            execute as @a[scores={lc_menu=3}] at @s run function {{{context.ModId}}}:city/register_banner
            execute as @a[scores={lc_menu=4}] at @s run function {{{context.ModId}}}:buildings/register_house
            execute as @a[scores={lc_menu=5}] at @s run function {{{context.ModId}}}:ui/chronicle
            execute as @a[scores={lc_menu=6}] at @s run function {{{context.ModId}}}:debug/reset_city
            scoreboard players set @a[scores={lc_menu=1..}] lc_menu 0

            scoreboard players add #tick lc_scan_timer 1
            execute if score #tick lc_scan_timer matches 100.. as @a[limit=1,sort=nearest] at @s run function {{{context.ModId}}}:core/schedule
            execute if score #tick lc_scan_timer matches 100.. run scoreboard players set #tick lc_scan_timer 0
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackTickFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackTickFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack schedule function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackScheduleFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            # Scheduled aggregate simulation. Keep this local to the selected city area.
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            function {{{context.ModId}}}:citizens/register
            function {{{context.ModId}}}:city/update_population
            function {{{context.ModId}}}:food/update
            function {{{context.ModId}}}:security/update
            function {{{context.ModId}}}:quests/update
            function {{{context.ModId}}}:chronicle/update
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackScheduleFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackScheduleFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city create function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCityCreateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/already_exists
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/check_villagers
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCreateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCreateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city check villagers function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCityCheckVillagersFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            scoreboard players set #nearby_villagers lc_tmp 0
            execute store result score #nearby_villagers lc_tmp if entity @e[type=minecraft:villager,distance=..96]
            execute if score #nearby_villagers lc_tmp matches 2.. run function {{{context.ModId}}}:city/create_new
            execute unless score #nearby_villagers lc_tmp matches 2.. run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"At least 2 villagers must be within 96 blocks before founding a city."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCheckVillagersFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCheckVillagersFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city create new function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCityCreateNewFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            data merge storage {{{context.ModId}}}:city {founded:1b,year:1,population:0,food:100,security:100,prestige:0,houses:0,workplaces:0,founder:{x:0,y:0,z:0},banner:{x:0,y:0,z:0}}
            execute store result storage {{{context.ModId}}}:city year int 1 run scoreboard players get #year lc_year
            execute store result storage {{{context.ModId}}}:city founder.x int 1 run data get entity @s Pos[0] 1
            execute store result storage {{{context.ModId}}}:city founder.y int 1 run data get entity @s Pos[1] 1
            execute store result storage {{{context.ModId}}}:city founder.z int 1 run data get entity @s Pos[2] 1
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #prestige lc_prestige 0
            function {{{context.ModId}}}:citizens/register
            function {{{context.ModId}}}:city/update_population
            function {{{context.ModId}}}:chronicle/add_event
            tellraw @a [{"text":"[Living Cities] ","color":"gold"},{"text":"A city was founded. Register the banner from the town hall menu next."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCreateNewFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityCreateNewFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city already exists function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCityAlreadyExistsFunction() {
    try
    {
        return """
            tellraw @s [{"text":"[Living Cities] ","color":"yellow"},{"text":"A city already exists in this starter datapack. Use reset only in a test world."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityAlreadyExistsFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCityAlreadyExistsFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack register banner function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackRegisterBannerFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            say LC register_banner loaded
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.x int 1 run data get entity @s Pos[0] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.y int 1 run data get entity @s Pos[1] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.z int 1 run data get entity @s Pos[2] 1
            execute if entity @s[type=minecraft:player] run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Town banner position registered at your current location."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackRegisterBannerFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackRegisterBannerFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack update population function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackUpdatePopulationFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            scoreboard players set #population lc_population 0
            execute store result score #population lc_population if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96]
            execute store result storage {{{context.ModId}}}:city population int 1 run scoreboard players get #population lc_population
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackUpdatePopulationFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackUpdatePopulationFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen register function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCitizenRegisterFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            scoreboard players set #registered_this_scan lc_population 0
            execute as @e[type=minecraft:villager,distance=..96,tag=!lc_citizen] at @s run function {{{context.ModId}}}:citizens/detect_new
            function {{{context.ModId}}}:citizens/aging
            function {{{context.ModId}}}:citizens/personalities
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenRegisterFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenRegisterFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen detect new function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCitizenDetectNewFunction() {
    try
    {
        return """
            tag @s add lc_citizen
            scoreboard players operation @s lc_birth_year = #year lc_year
            scoreboard players add #registered_this_scan lc_population 1
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenDetectNewFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenDetectNewFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen aging function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCitizenAgingFunction() {
    try
    {
        return """
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp = #year lc_year
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp -= @s lc_birth_year
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenAgingFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenAgingFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen personalities function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCitizenPersonalitiesFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #population lc_population matches 5.. as @e[type=minecraft:villager,tag=lc_citizen,tag=!lc_personality,limit=1,sort=random] run tag @s add lc_personality
            execute store result storage {{{context.ModId}}}:personalities count int 1 if entity @e[type=minecraft:villager,tag=lc_personality]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenPersonalitiesFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenPersonalitiesFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen status function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackCitizenStatusFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"Registered citizens: ","color":"gold"},{"score":{"name":"#population","objective":"lc_population"}}]
            tellraw @s [{"text":"Personalities: ","color":"light_purple"},{"storage":"{{{context.ModId}}}:personalities","nbt":"count"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenStatusFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackCitizenStatusFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food update function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackFoodUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:food/production
            function {{{context.ModId}}}:food/consumption
            scoreboard players operation #food lc_food += #food_production lc_tmp
            scoreboard players operation #food lc_food -= #food_consumption lc_tmp
            execute if score #food lc_food matches ..0 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Food stores are empty. Growth and migration should pause in the next milestone."}]
            execute store result storage {{{context.ModId}}}:city food int 1 run scoreboard players get #food lc_food
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food production function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackFoodProductionFunction() {
    try
    {
        return """
            scoreboard players set #food_production lc_tmp 0
            scoreboard players set #food_counter lc_tmp 0
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:farmer"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:fisherman"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:butcher"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:shepherd"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodProductionFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodProductionFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food consumption function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackFoodConsumptionFunction() {
    try
    {
        return """
            scoreboard players operation #food_consumption lc_tmp = #population lc_population
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodConsumptionFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackFoodConsumptionFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security update function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackSecurityUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:security/golems
            function {{{context.ModId}}}:security/nightwatch
            execute store result storage {{{context.ModId}}}:city security int 1 run scoreboard players get #security lc_security
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security golems function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackSecurityGolemsFunction() {
    try
    {
        return """
            scoreboard players set #golems lc_tmp 0
            scoreboard players set #security_factor lc_tmp 20
            execute store result score #golems lc_tmp if entity @e[type=minecraft:iron_golem,distance=..96]
            scoreboard players operation #security lc_security = #golems lc_tmp
            scoreboard players operation #security lc_security *= #security_factor lc_tmp
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityGolemsFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityGolemsFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security nightwatch function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackSecurityNightwatchFunction() {
    try
    {
        return """
            execute if score #security lc_security matches ..19 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Security is low. Build defenses or protect villagers at night."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityNightwatchFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackSecurityNightwatchFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle add event function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackChronicleAddEventFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            data modify storage {{{context.ModId}}}:chronicle events append value {type:"city_founded",text:"A city was founded.",year:1}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleAddEventFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleAddEventFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle update function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackChronicleUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #registered_this_scan lc_population matches 1.. run data modify storage {{{context.ModId}}}:chronicle events append value {type:"citizens_registered",text:"New citizens were registered.",year:1}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleUpdateFunction)} failed.");
        throw;
    }
}


    }
}
