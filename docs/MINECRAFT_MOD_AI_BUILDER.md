# Minecraft Mod AI Builder

## Direction

LocalGPT should treat Minecraft Java Edition as the first-class mod target.

Reasons:

- Java mods can be generated, compiled, inspected, and tested with JDK 21 and Gradle.
- Fabric and NeoForge support normal Java source projects.
- Eclipse, IntelliJ IDEA, and VS Code can import generated Gradle projects.
- Bedrock content uses behavior/resource packs instead of Java mods and should be a separate exporter.

LocalGPT should also support vanilla datapacks for command/function-only systems and Paper plugins for server-side Java plugin workflows.

Grounding sources checked while creating this guide:

- Microsoft Learn: Microsoft Build of OpenJDK downloads and Java developer resources: https://learn.microsoft.com/en-us/java/openjdk/download
- Oracle Java SE 21 Language Specification for Java syntax and language rules: https://docs.oracle.com/javase/specs/jls/se21/html/index.html
- Fabric official documentation for Fabric as a lightweight Minecraft Java Edition modding toolchain: https://docs.fabricmc.net/develop/
- NeoForge official documentation for Java toolchain and modern Forge-style project setup: https://docs.neoforged.net/docs/gettingstarted/
- PaperMC official documentation for Paper plugin project setup: https://docs.papermc.io/paper/dev/project-setup/
- Minecraft Bedrock add-on documentation for the behavior-pack/resource-pack split: https://learn.microsoft.com/en-us/minecraft/creator/documents/gettingstarted

There is no separate "Microsoft Java syntax". Microsoft provides supported OpenJDK builds; Java syntax and language rules should be grounded in the Java Language Specification.

## Toolchain

Required for Java mod/plugin builds:

- Minecraft Java Edition installed and launched at least once for the target version.
- JDK 21 for the generated LocalGPT 1.21.x starter projects, preferably Microsoft OpenJDK 21 on this Windows setup.
- LocalGPT Gradle tool folder under `%LOCALAPPDATA%\LocalGPT\Tools\gradle-8.14.2`, or another working `gradle` on PATH.
- Eclipse IDE for Java Developers or another Gradle-aware IDE.
- Ollama running when AI planning, review, or council workflows are needed.

For newer Minecraft/loader versions, verify the exact JDK requirement from the loader's official docs before changing `JavaVersion`.

Setup helper:

```powershell
.\LocalGPTWebviewWrapper\build\Setup-MinecraftModToolchain.ps1 -Install -InstallGradle -InstallEclipse
```

The generated mod workspaces include:

```powershell
.\build-local.ps1
```

That script finds `JAVA_HOME`, falls back to the Microsoft OpenJDK 21 install path, then uses LocalGPT's local Gradle install when available.

## Loader Policy

Use Fabric when the user wants a lightweight, fast iteration target.

Use NeoForge when the user asks for Forge-style modding on modern Minecraft.

Use Paper Plugin when the user wants server-side Java plugin behavior without a modded client.

Use Datapack when the user wants vanilla-compatible commands, functions, scoreboards, loot tables, recipes, tags, or data-driven behavior without Java.

Use classic Forge only when the requested Minecraft version and dependencies require it.

Use Bedrock only through a future behavior/resource pack exporter. Do not mix Bedrock pack generation with Java mod code generation.

## AI Council Behavior

The AI Council should help with both code and setup.

When a user asks for Minecraft mod building, every council participant should check:

- target edition: Java or Bedrock
- output direction: Fabric mod, NeoForge mod, Paper plugin, datapack, classic Forge, or Bedrock pack exporter
- Minecraft version
- Java version
- Gradle availability
- IDE/import steps
- Ollama model availability
- whether the generated workspace has actually been built
- whether the requested system can be done as a vanilla datapack before using Java

If any setup is missing, the council should produce a short technical recovery poll instead of guessing.

Example poll options:

- Install toolchain first: JDK 21, Gradle, Eclipse, Minecraft launcher.
- Generate workspace first: create files and defer game launch.
- Ask council to choose target: compare Fabric, NeoForge, Paper, datapack, and Bedrock tradeoffs.
- Reduce scope: make a buildable starter item/command before simulation systems.

The selected choice should be saved into SQLite chat memory so later model calls see it.

## Living Cities Starter

For the Living Cities 0.1 sample, the first generated output should stay small and buildable:

- register one `city_charter` item
- add `/livingcities report` for Java mod/plugin targets
- add `load`, `tick`, `found_city`, and `report` functions for datapack targets
- write the full technical plan to `docs/living-cities-0.1-plan.md`
- avoid global world scans
- design city-level aggregate simulation before per-citizen entities

Next milestones:

1. City founding: banner plus torch.
2. State persistence: saved data or scoreboard/storage bridge.
3. Citizen registration.
4. Population aggregation.
5. Minimal town hall report.
6. Food/security simulation.
7. Personalities and chronicle.

## Missing-Feature Reports

If a model cannot complete a Minecraft workflow because LocalGPT lacks a feature, it should include a `Missing feature report` section and name:

- the blocked workflow
- the exact missing LocalGPT feature
- the smallest useful implementation
- whether the request belongs in backend services, frontend UI, setup scripts, or AI markup

Examples:

- version-aware dependency lookup for Fabric API, Yarn, NeoForge, and Minecraft
- one-click Gradle build from the generated workspace
- runClient/runServer launch harness
- datapack validation and zip packaging
- Paper plugin test server runner
- Minecraft logs/crash-report collector
- generated GameTest/JUnit test templates
- Bedrock behavior/resource pack exporter

## LocalGPT Test Helpers

Use LocalGPT's own diagnostic routes instead of direct Ollama calls when validating Minecraft/council behavior:

- `POST /__diag/dxaichat-smoke`: exercises the configured DXAiChat backend client, separates visible answer from model thinking, and can save the exchange to SQLite memory.
- `POST /__diag/council`: runs the multi-model council with logging and memory.
- `GET /__diag/council/models`: lists configured and installed Ollama models visible to LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`: generates a smoke workspace through `IMinecraftModWorkspaceService`.

For full desktop validation, run the WinUI wrapper from a registered/package identity or Visual Studio debug launch with:

```powershell
$env:LOCALGPT_WEBVIEW2_SMOKE = "1"
$env:LOCALGPT_WEBVIEW2_SMOKE_EXIT = "1"
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net10.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

The wrapper writes WebView2 page snapshots under `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`. Use these snapshots to verify that `/Chat` and `/minecraft-mod-builder` load inside the actual desktop shell.

## Truthfulness Rules

Do not say a mod was compiled, launched, or tested unless command output is available.

Mark version-sensitive dependency claims as `Needs verification` unless they came from official docs, the generated project, or a successful build.

Keep humans in control. The council can suggest commands, explain tradeoffs, and generate files, but it should ask before destructive cleanup or broad system changes.
