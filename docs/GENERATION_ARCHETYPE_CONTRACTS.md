# LocalGPT Generation Archetype Contracts

This file is a compact rulebook for AI Council project generation. It exists because a model can produce working-looking files while still failing the architectural request by using the same generic project shape for unrelated targets.

## Core Rule

Do not generate files before selecting a project archetype.

Every whole-project or whole-solution generation must start from this classification:

```json
{
  "project_kind": "fabric_mod | neoforge_mod | paper_plugin | datapack | localgpt_feature | dotnet_service",
  "target_platform": "minecraft_java | dotnet10_aspnetcore_blazor | winui_webview2 | backend_service",
  "complexity": "minimal | normal | advanced",
  "needs_datagen": true,
  "needs_tests": true,
  "needs_native_commands": false,
  "needs_index": true,
  "needs_version_resolver": true,
  "expected_entrypoints": []
}
```

The classification is not decorative. It decides the skeleton, metadata files, build commands, source/resource layout, validation checks, and what must not be copied from other archetypes.

## Required Files

Every generated whole project must include:

- `PROJECT_INDEX.md`
- `ARCHITECTURE.md`
- `BUILD_AND_RUN.md`
- `.localgpt-generation.json`
- A platform-correct source/resource layout

`PROJECT_INDEX.md` must list the purpose, entry points, generated files table, build/run commands, and validation status. It should be generated first because it forces the model to account for every folder and route.

`.localgpt-generation.json` must include the selected archetype, target platform, requested features, important versions, expected entry points, generated files, validation status, build/test result provenance, and safety notes.

## Validation Honesty

Do not claim build success without command output. Use these validation labels:

- `GeneratedOnly`
- `GeneratedOnlyContractValidated`
- `BuildPassed`
- `SmokePassed`
- `LaunchPassed`
- `NeedsVerification`

Generated projects must be rejected or regenerated when required files are missing, platform metadata is wrong, Java projects lack build files, datapacks lack `pack.mcmeta`, generated files are missing from the manifest, or validation status claims success without evidence.

## Archetype Deltas

### Fabric vs Paper

Fabric is a Java mod-loader project. It uses Gradle, `fabric.mod.json`, mod initializer entry points, resources under `src/main/resources`, and optional datagen output.

Paper is a server plugin. It uses Bukkit/Paper APIs, a Java plugin entry point, and `plugin.yml` or `paper-plugin.yml`. Do not generate Fabric metadata for Paper.

### Fabric vs Datapack

Fabric uses Java and Gradle. Datapacks use `pack.mcmeta`, `data/<namespace>/function`, tags, recipes, advancements, loot tables, and `.mcfunction` files. Do not generate Java source files for datapacks.

### NeoForge vs Fabric

NeoForge uses NeoForge-specific Gradle setup, metadata, event bus conventions, and registration patterns. Do not reuse Fabric loader metadata.

### LocalGPT Feature vs Ollama .NET Lab

A LocalGPT feature artifact should look like a LocalGPT/TacosPortalOpen feature sandbox: real `.razor` pages, DevExpress controls, backend services/routes, EF/SQLite when durable state is needed, artifact downloads, diagnostics, and user approval gates.

An Ollama .NET lab should look like an API-control-plane experiment:
Ollama-shaped route cataloging, model inventory, model download planning,
settings, compatibility notes, endpoint tests, and explicit native-runner
boundaries. It should expose representative route stubs for version, tags,
running models, show, pull, push, create, copy, delete, generate, chat, and
embed. It must not pretend to replace Ollama's GGML/GPU runner unless a real
approved backend exists.

## Platform Skeletons

### Datapack

- `pack.mcmeta`
- `data/minecraft/tags/function/load.json`
- `data/minecraft/tags/function/tick.json`
- `data/<namespace>/function/*.mcfunction`
- `data/<namespace>/advancement/`
- `data/<namespace>/recipe/`
- `data/<namespace>/loot_table/`
- `PROJECT_INDEX.md`
- `.localgpt-generation.json`

### LocalGPT Feature

- `Components/Pages/<FeaturePage>.razor`
- `Services/<FeatureService>.cs`
- `Interfaces/I<FeatureService>.cs` when the service is injected across boundaries
- `BusinessObjects/<FeatureOptions>.cs` or model records when state is shared
- Backend route extension or diagnostic route when downloads, native commands, or health checks are involved
- `wwwroot/icons/nav/*-line.svg` and `wwwroot/icons/nav/*-solid.svg` when generated navigation needs icons
- `docs/<FEATURE>.md`
- Test, smoke route, or diagnostic route

### .NET Service Or Blazor Lab

- `.sln`
- SDK-style `.csproj`
- `Program.cs`
- `Components/App.razor`
- `Components/Routes.razor`
- `Components/Pages/Index.razor`
- Navigation component
- Service/model folders
- `wwwroot/app.css`
- `wwwroot/icons/nav/*-line.svg`
- `wwwroot/icons/nav/*-solid.svg`
- Required project docs and generation manifest

Use Bootstrap v5 for page grid, spacing, and flex layout. Use DevExpress
components for real application controls. Navigation icons should have paired
line and solid SVG styles so default, hover, active, and compact states are
available without regenerating assets.

## Microsoft .NET Architecture Grounding

Use Microsoft Learn architecture guidance for modern .NET decisions:

- .NET architecture overview: https://learn.microsoft.com/en-us/dotnet/architecture/
- Modern ASP.NET Core web apps: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/
- Modern Web App pattern for .NET: https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/enterprise-app-patterns/modern-web-app/dotnet/guidance
- Blazor for ASP.NET Web Forms developers: https://learn.microsoft.com/en-us/dotnet/architecture/blazor-for-web-forms-developers/
- Framework design guidelines: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/
- Library guidance: https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/

Translate those sources into these generation rules:

- When the selected target is a .NET web app, prefer a cohesive monolithic
  Blazor/ASP.NET Core app unless a real independent service boundary exists.
  For non-.NET-web targets, choose the target-specific archetype first.
- Use service-oriented separation for real boundaries: independent scaling, external runners, background work, downloads, report/document generation, or integrations.
- Keep UI interaction in Razor components and business/native/data work in services.
- Use dependency injection and testable service APIs instead of embedding logic in markup strings.
- Keep appsettings for bootstrap/runtime configuration and store user/application state in EF/SQLite when it must survive restarts.
- Include diagnostics or health checks for generated runtime features.
- Design APIs and models with clear naming, stable contracts, debuggability, and evolution in mind.
- Before implementing ambiguous architecture, ask the user with a concrete poll
  and stop before generating code or files. Polls should be created from the
  user's actual request and include only material tradeoffs such as target
  platform/runtime, language/framework, UI stack if any, single solution versus
  split frontend/API, server-rendered versus client-rendered UI, direct EF
  entities versus DTO/API boundaries, deployment target, artifact expectations,
  and reference-app fidelity versus functional prototype speed.
- For a "goal to recode" application, extract the reference app's product shape:
  navigation, index/landing behavior, model/data catalog, settings, API routes,
  downloads, logs, statuses, and primary workflows. The generated app must use
  the selected stack to recreate that product shape, not reuse a LocalGPT sample
  or force Blazor/DevExpress when the user did not choose it.

## Bad Output Signals

Reject or regenerate output when:

- Two different project kinds have the same folder structure.
- Fabric, Paper, datapack, LocalGPT, and Ollama lab outputs reuse each other's metadata.
- `PROJECT_INDEX.md` is missing or vague.
- There is no index/home route for a generated app.
- The generated Blazor page is only a C# class that returns markup as a string.
- The artifact lacks navigation or a user-visible first screen.
- The model says "build passed" without command output.
- Binary payloads are printed in chat instead of exposed through download routes.
- The generated app ignores the reference application's recognizable layout,
  navigation, routes, settings, or workflows.
- A model/provider selection test says one model in the UI/URL but sends the
  request to another runtime model.

## Pipeline

1. Classify project archetype.
2. Resolve versions and dependencies.
3. Select a platform skeleton.
4. Generate `PROJECT_INDEX.md`.
5. Generate `.localgpt-generation.json`.
6. Generate files.
7. Validate required files and platform metadata.
8. Optionally build or smoke test.
9. Save the result and validation status to Council memory.
