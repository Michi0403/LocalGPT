using LocalGPT.BusinessObjects;
using System.IO.Compression;
using System.Text.Json;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates minecraft datapack behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MinecraftDatapackService
    {
        /// <summary>
        /// Creates datapack admin book function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackAdminBookFunction(WorkspaceContext context)
        {
    try
    {
                var bookContent = "{title:\"Living Cities\",author:\"LocalGPT\",pages:[["
                    + "{text:\"=== Living Cities ===\\n\\n\",bold:true,color:\"gold\"},"
                    + "{text:\"[Found city]\\n\",color:\"green\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 1\"}},"
                    + "{text:\"\\n[Status]\\n\",color:\"aqua\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 2\"}},"
                    + "{text:\"\\n[Register banner]\\n\",color:\"yellow\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 3\"}},"
                    + "{text:\"\\n[Register house]\\n\",color:\"light_purple\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 4\"}},"
                    + "{text:\"\\n[Chronicle]\\n\",color:\"gold\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 5\"}},"
                    + "{text:\"\\n[Reset test city]\",color:\"red\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 6\"}}"
                    + "]]}";

                return $$"""
                    tag @s add lc_received_book
                    scoreboard players enable @s lc_menu
                    give @s written_book[written_book_content={{bookContent}}] 1
                    tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Admin book created. You can also run /function {{context.ModId}}:ui/townhall."}]
                    """;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackAdminBookFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackAdminBookFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack town hall function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackTownHallFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"=== Living Cities Town Hall ===","color":"gold","bold":true}]
            tellraw @s [{"text":"Found city","color":"green","click_event":{"action":"run_command","command":"/trigger lc_menu set 1"}},{"text":" | "},{"text":"Status","color":"aqua","click_event":{"action":"run_command","command":"/trigger lc_menu set 2"}},{"text":" | "},{"text":"Chronicle","color":"yellow","click_event":{"action":"run_command","command":"/trigger lc_menu set 5"}}]
            tellraw @s [{"text":"Direct report: /function {{{context.ModId}}}:ui/status","color":"gray"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackTownHallFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackTownHallFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack report function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackReportFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"=== Living Cities Status ===","color":"gold","bold":true}]
            execute if data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"City founded: ","color":"gray"},{"text":"yes","color":"green"}]
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"City founded: ","color":"gray"},{"text":"no","color":"red"}]
            tellraw @s [{"text":"Population: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"population"}]
            tellraw @s [{"text":"Food: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"food"}]
            tellraw @s [{"text":"Security: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"security"}]
            tellraw @s [{"text":"Houses: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"houses"}]
            tellraw @s [{"text":"Next: use the admin book or /function {{{context.ModId}}}:ui/townhall","color":"green"}]
            function {{{context.ModId}}}:citizens/status
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackReportFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackReportFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle UI function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackChronicleUiFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"=== Living Cities Chronicle ===","color":"gold","bold":true}]
            tellraw @s [{"storage":"{{{context.ModId}}}:chronicle","nbt":"events"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleUiFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackChronicleUiFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack quest update function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackQuestUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:quests/generate
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackQuestUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackQuestUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack quest generate function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackQuestGenerateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #houses lc_buildings matches ..0 run data merge storage {{{context.ModId}}}:city {quest:"Register at least one house."}
            execute if score #food lc_food matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Increase food production."}
            execute if score #security lc_security matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Improve security."}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackQuestGenerateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackQuestGenerateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack buildings init function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackBuildingsInitFunction() {
    try
    {
        return """
            scoreboard players set #houses lc_buildings 0
            scoreboard players set #workplaces lc_buildings 0
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildingsInitFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildingsInitFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack register house function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackRegisterHouseFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"Found a city before registering houses."}]
            execute if data storage {{{context.ModId}}}:city {founded:1b} run scoreboard players add #houses lc_buildings 1
            execute if data storage {{{context.ModId}}}:city {founded:1b} store result storage {{{context.ModId}}}:city houses int 1 run scoreboard players get #houses lc_buildings
            execute if data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"House registered for the current city."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackRegisterHouseFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackRegisterHouseFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack building debug list function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackBuildingDebugListFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"Registered houses: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"houses"}]
            tellraw @s [{"text":"Workplaces: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"workplaces"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildingDebugListFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildingDebugListFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack reset city function as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackResetCityFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            data modify storage {{{context.ModId}}}:city set value {}
            data modify storage {{{context.ModId}}}:chronicle set value {events:[]}
            data modify storage {{{context.ModId}}}:personalities set value {notables:[]}
            tag @e[type=minecraft:villager,tag=lc_citizen] remove lc_citizen
            tag @e[type=minecraft:villager,tag=lc_personality] remove lc_personality
            scoreboard players set #population lc_population 0
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #houses lc_buildings 0
            function {{{context.ModId}}}:core/load
            tellraw @s [{"text":"[Living Cities] ","color":"yellow"},{"text":"Test city state reset."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackResetCityFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackResetCityFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack build script as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackBuildScript(WorkspaceContext context) {
    try
    {
        return $$"""
            [CmdletBinding()]
            param(
                [string]$Configuration = "Release"
            )

            $ErrorActionPreference = "Stop"
            $root = Split-Path -Parent $MyInvocation.MyCommand.Path
            $buildDir = Join-Path $root "build"
            $zipPath = Join-Path $buildDir "{{context.ProjectName}}-datapack.zip"

            New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
            if (Test-Path $zipPath) {
                Remove-Item $zipPath -Force
            }

            function Get-LocalRelativePath {
                param(
                    [Parameter(Mandatory = $true)][string]$BasePath,
                    [Parameter(Mandatory = $true)][string]$Path
                )

                $baseFull = (Resolve-Path $BasePath).Path.TrimEnd("\", "/") + "\"
                $pathFull = (Resolve-Path $Path).Path
                $baseUri = [Uri]$baseFull
                $pathUri = [Uri]$pathFull
                return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace("/", "\")
            }

            $required = @(
                "pack.mcmeta",
                "data/minecraft/tags/function/load.json",
                "data/minecraft/tags/function/tick.json",
                "data/{{context.ModId}}/function/core/load.mcfunction",
                "data/{{context.ModId}}/function/core/tick.mcfunction",
                "data/{{context.ModId}}/function/city/create.mcfunction",
                "data/{{context.ModId}}/function/citizens/register.mcfunction",
                "data/{{context.ModId}}/function/food/update.mcfunction",
                "data/{{context.ModId}}/function/security/update.mcfunction",
                "data/{{context.ModId}}/function/ui/townhall.mcfunction",
                "data/{{context.ModId}}/function/ui/status.mcfunction"
            )

            foreach ($relative in $required) {
                $path = [IO.Path]::Combine($root, $relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
                if (-not (Test-Path $path)) {
                    throw "Missing datapack file: $relative"
                }
            }

            $wrapperMcmeta = Get-ChildItem $root -Directory | ForEach-Object { Join-Path $_.FullName "pack.mcmeta" } | Where-Object { Test-Path $_ }
            if ($wrapperMcmeta.Count -gt 0) {
                throw "Datapack wrapper folder detected. The zip root must contain pack.mcmeta directly, not a nested project folder."
            }

            $legacyFunctions = Get-ChildItem (Join-Path $root "data") -Recurse -Directory -Filter "functions"
            if ($legacyFunctions.Count -gt 0) {
                throw "Found legacy plural 'functions' folder. Minecraft 1.21+ datapacks use singular 'function'."
            }

            Get-Content (Join-Path $root "pack.mcmeta") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data/minecraft/tags/function/load.json") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data/minecraft/tags/function/tick.json") -Raw | ConvertFrom-Json | Out-Null

            $txtPlaceholders = Get-ChildItem (Join-Path $root "data") -Recurse -File -Filter "*.mcfunction.txt"
            if ($txtPlaceholders.Count -gt 0) {
                throw "Found .mcfunction.txt placeholders. Rename or implement them as .mcfunction files before packaging."
            }

            $functionIds = @{}
            $functionFiles = Get-ChildItem (Join-Path $root "data") -Recurse -File -Filter "*.mcfunction"
            foreach ($file in $functionFiles) {
                $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                $parts = $relativePath -split "[\\/]"
                $functionIndex = [Array]::IndexOf($parts, "function")
                if ($parts.Length -lt 4 -or $parts[0] -ne "data" -or $functionIndex -lt 2) {
                    throw "Invalid function path: $relativePath"
                }

                $namespace = $parts[1]
                $pathParts = $parts[($functionIndex + 1)..($parts.Length - 1)]
                $functionPath = ($pathParts -join "/") -replace "\.mcfunction$", ""
                $functionIds["${namespace}:$functionPath"] = $relativePath
            }

            $tagFiles = Get-ChildItem $minecraftTagRoot -File -Filter "*.json"
            foreach ($tag in $tagFiles) {
                $json = Get-Content $tag.FullName -Raw | ConvertFrom-Json
                foreach ($value in $json.values) {
                    if (-not $functionIds.ContainsKey([string]$value)) {
                        throw "Function tag $($tag.Name) references missing function: $value"
                    }
                }
            }

            $referencePattern = [regex]'(?<![#/])\bfunction\s+([a-z0-9_.-]+:[a-z0-9_./-]+)'
            foreach ($file in $functionFiles) {
                $content = Get-Content $file.FullName -Raw
                if ($content -match "(?m)^\s*/") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath contains a leading slash command. Remove leading / inside .mcfunction files."
                }

                if ($content -match "\bdata\s+remove\s+storage\b") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath uses 'data remove storage'. Use 'data modify storage <id> set value ...' for root storage reset."
                }

                if ($content -match "\bstore\s+result\s+storage\s+[a-z0-9_.-]+:[a-z0-9_/-]+\.[a-z0-9_.-]+\s+(byte|short|int|long|float|double)\b") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath appears to put an NBT path into the storage id. Use 'storage namespace:id path int 1', for example 'storage living_cities:city year int 1'."
                }

                foreach ($match in $referencePattern.Matches($content)) {
                    $id = $match.Groups[1].Value
                    if (-not $functionIds.ContainsKey($id)) {
                        $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                        throw "Function $relativePath references missing function: $id"
                    }
                }
            }

            Compress-Archive -Path (Join-Path $root "pack.mcmeta"), (Join-Path $root "data") -DestinationPath $zipPath
            Write-Host "Validated $($functionFiles.Count) mcfunction files."
            Write-Host "Created datapack: $zipPath"
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildScript)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBuildScript)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack benchmark notes as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackBenchmarkNotes(WorkspaceContext context) {
    try
    {
        return $$"""
            # Living Cities Reference Benchmark

            This generated datapack was shaped against the provided early `living_cities.zip` reference.

            Reference traits preserved:

            - namespace: `living_cities`
            - singular Minecraft 1.21 datapack folders: `data/<namespace>/function` and `data/minecraft/tags/function`
            - `core/load` and `core/tick` entry points
            - scoreboard objectives for year, population, food, security, prestige, birth year, menu triggers, scan timer, and buildings
            - storage areas for city, chronicle, and personalities
            - trigger/menu-driven administration book
            - no full-world scans in the tick path; scheduled city-local checks only

            Improvements over the early reference:

            - no `.mcfunction.txt` placeholder files
            - build helper validates root zip layout, function tags, singular `function` folders, leading slash mistakes, root storage reset syntax, and `function namespace:path` references
            - generated output includes food, security, chronicle, quests, and building functions as real `.mcfunction` files
            - town hall UI is available through both the admin book and `/function {{context.ModId}}:ui/townhall`
            - `city/register_banner` includes a visible `say LC register_banner loaded` smoke line so testers can separate discovery problems from command behavior

            Remaining needs before your friend tests in a real world:

            - confirm the exact Minecraft Java version and pack format
            - run `/reload`, `/datapack list`, and `/function {{context.ModId}}:ui/townhall`
            - decide whether banner registration should stay menu-based or become a stricter raycast/block-position workflow
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBenchmarkNotes)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackBenchmarkNotes)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack readme as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateDatapackReadme(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
            # {{context.ProjectName}} Datapack

            Generated by LocalGPT as a vanilla Minecraft Java datapack.

            ## Build

            ```powershell
            pwsh ./build-local.ps1
            ```

            The build helper validates JSON files and creates `build/{{context.ProjectName}}-datapack.zip`.

            ## Install

            Copy the zip into a world's `datapacks` folder, then run:

            ```mcfunction
            /reload
            /function {{context.ModId}}:ui/townhall
            ```

            If `/function {{context.ModId}}:city/register_banner` is not offered by autocomplete, debug discovery before command syntax:

            - unzip the datapack and ensure `pack.mcmeta` is at zip root
            - for Minecraft 1.21+ ensure folders are `data/<namespace>/function` and `data/minecraft/tags/function`
            - run `/reload`, `/datapack list`, then `/function {{context.ModId}}:city/register_banner`
            - ensure no file ends in `.mcfunction.txt`
            - run `pwsh ./build-local.ps1` to validate references before copying the zip

            ## Structure

            Minecraft 1.21 uses the singular `function` registry folder:

            - `data/minecraft/tags/function/load.json`
            - `data/minecraft/tags/function/tick.json`
            - `data/{{context.ModId}}/function/core/*.mcfunction`
            - `data/{{context.ModId}}/function/city/*.mcfunction`
            - `data/{{context.ModId}}/function/citizens/*.mcfunction`
            - `data/{{context.ModId}}/function/food/*.mcfunction`
            - `data/{{context.ModId}}/function/security/*.mcfunction`
            - `data/{{context.ModId}}/function/ui/*.mcfunction`

            ## Living Cities Starter

            This datapack implements the first Living Cities 0.1 vertical slice: scoreboards, storage, city founding, citizen registration, aggregate population, food, security, chronicle, basic quests, and a town hall/admin-book UI. Keep tick work tiny; scale the real system through scheduled, city-scoped functions and stored aggregate values.
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackReadme)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(CreateDatapackReadme)} failed.");
        throw;
    }
}

    }
}
