# Minecraft Source Knowledge For AI Council

This file is a compact source-backed briefing for LocalGPT's AI Council. Prefer the matching pinned SQLite knowledge entries during chat, and open this file only when the model needs the source map behind those entries.

## Java Mod And Plugin Toolchains

- Classic Forge: start from the Forge MDK, extract it into an empty directory, import/open it as a Gradle project, build with `gradlew build`, and test with generated run configurations or `gradlew runClient` / `gradlew runServer`. Dedicated-server tests require accepting the EULA in the run directory.
- NeoForge: treat it as the modern Forge-style path when the user wants Forge-like modding on current Minecraft versions. Verify exact dependency versions from NeoForge docs before claiming a generated workspace is current.
- Fabric: build with `./gradlew build` or `./gradlew.bat build`; use the shortest jar in `build/libs` outside development. Check the Java version used by the terminal and IDE because a mismatched default JDK can break builds.
- Paper: use Paper when the user wants server-side plugin behavior without a modded client. Generate `plugin.yml`, a Java plugin main class, and Gradle setup from Paper project guidance.
- Gradle/JDK: use Java toolchains or explicit IDE Gradle JVM settings so command-line and IDE builds use the same JDK. Java syntax belongs to the Java Language Specification and JDK docs; Microsoft OpenJDK is a supported JDK distribution, not a separate Java dialect.

Primary sources:

- Forge getting started: https://docs.minecraftforge.net/en/latest/gettingstarted/
- NeoForge getting started: https://docs.neoforged.net/docs/gettingstarted/
- Fabric building a mod: https://docs.fabricmc.net/develop/getting-started/building-a-mod
- Paper getting started: https://docs.papermc.io/paper/dev/getting-started/
- Gradle JVM toolchains: https://docs.gradle.org/current/userguide/toolchains.html
- Oracle JDK 21 documentation: https://docs.oracle.com/en/java/javase/21/

## Vanilla Java Datapacks

- Datapack root must contain `pack.mcmeta` and `data/`.
- `pack.mcmeta` must include `pack.pack_format`; the value is version-sensitive.
- Modern generated Minecraft 1.21-style function paths should use singular folders:
  - `data/<namespace>/function/...`
  - `data/minecraft/tags/function/load.json`
  - `data/minecraft/tags/function/tick.json`
- `minecraft:load` should call setup functions after server load or `/reload`.
- `minecraft:tick` runs each tick, so keep it tiny. For scalable designs, use scoreboard timers and call scheduled aggregate functions every few seconds instead of scanning the whole world.
- Function tag JSON must reference existing function IDs. LocalGPT generated datapacks should validate tag targets and `function namespace:path` references before zipping.
- `supported_formats` and overlays are available for multi-version packs, but simple generated starters should target one exact version unless the user asks for overlays.

Primary/reference sources:

- Minecraft Wiki data pack structure and `pack.mcmeta`: https://minecraft.wiki/w/Data_pack
- Minecraft Wiki Java function tags: https://minecraft.wiki/w/Function_tag_(Java_Edition)
- Minecraft Java snapshot 23w31a pack metadata `supported_formats` and overlays: https://feedback.minecraft.net/hc/en-us/articles/18619031671821-Minecraft-Java-Edition-Snapshot-23w31a
- Minecraft Wiki pack format table: https://minecraft.wiki/w/Pack_format

## Living Cities Benchmark

Use `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4` as the low-context benchmark before asking big local models to review Living Cities. It should generate a workspace, run `build-local.ps1`, validate JSON and function references, create a zip, and write a compact pinned council knowledge entry.

Compare against the user's early `living_cities.zip` for these preserved traits:

- namespace `living_cities`
- `pack.mcmeta`
- `data/minecraft/tags/function/load.json`
- `data/minecraft/tags/function/tick.json`
- `core/load` and `core/tick` entry points
- scoreboard objectives for year, population, food, security, prestige, birth year, menu trigger, scan timer, buildings, and temp values
- storage areas for city, chronicle, and personalities
- town hall/admin workflow

Acceptance rules:

- No `.mcfunction.txt` placeholders.
- No missing functions referenced from load/tick tags.
- No missing functions referenced by `function namespace:path` commands.
- The zip must download through a LocalGPT HTTP artifact route or appear as a real local path, not as raw zip text in chat.
- Do not claim in-game testing until Minecraft has actually run `/reload`, `/datapack list`, and a visible command such as `/function living_cities:ui/townhall`.
