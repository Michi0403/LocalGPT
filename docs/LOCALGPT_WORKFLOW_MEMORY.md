# LocalGPT Workflow Memory

This file is a compact handoff for future agents and AI Council runs. Treat it as project memory, not as a substitute for current build output.

## Current Direction

LocalGPT is a Windows desktop-hosted Blazor/ASP.NET Core app inside a WinUI 3 WebView2 wrapper. The main app should stay in `LocalGPT`; the wrapper should stay thin and only host, navigate, and run desktop-shell diagnostics.

The main product direction is local AI-assisted Minecraft creation with Ollama:

- DXAiChat for normal single-model chat.
- AI Council for multiple local models that negotiate, correct each other, log, and save memory.
- Minecraft Builder for Fabric mods, NeoForge mods, Paper plugins, and vanilla datapacks.
- Bedrock add-ons should be a separate future behavior/resource pack exporter.

## Recent Commits

- `c224ed8` - Restore package manifest bytes after release stamping.
- `ef097ff` - Keep derived MSIX versions upgrade safe.
- `05c9570` - Clean wrapper release staging folders.
- `fa95cd9` - Harden package build exit checks.
- `eea64d4` - Stamp MSIX package version during releases.
- `dbf44ed` - Shorten WebView2 artifact smoke timeouts.
- `eb6f709` - Bundle Blazor static assets in Windows package.
- `6079846` - Add council implementation path polls.
- `1d4dabc` - Add whole solution council artifacts.
- `487df46` - Seed official DevExpress Microsoft council knowledge.

The old remote still accepted pushes but GitHub reported the repository moved to `https://github.com/Michi0403/LocalGPT.git`.

## Verified Builds And Smoke Tests

Verified before this memory note:

- `dotnet build LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Debug -p:Platform=x64` passed with 0 warnings and 0 errors.
- Visual Studio MSBuild package build for `LocalGPTWebviewWrapper (Package).wapproj` passed and emitted a debug x64 MSIX.
- `Setup-MinecraftModToolchain.ps1` found Microsoft OpenJDK 21 and LocalGPT Gradle 8.14.2.
- `Publish-LocalGptRelease.ps1 -Version smoke -Configuration Debug -Platforms x64 -SkipBuild` created a smoke x64 zip and release manifest.
- LocalGPT generated and built smoke workspaces through `GET /__diag/minecraft/workspace-smoke` for:
  - datapack: JSON validated and zip created
  - Paper: Gradle build succeeded
  - Fabric: Gradle build succeeded
  - NeoForge: Gradle build succeeded
- LocalGPT generated and validated the focused Living Cities datapack benchmark through `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4`:
  - latest checked workspace: `%LOCALAPPDATA%\LocalGPT\MinecraftModWorkspaces\LivingCitiesDatapackCouncil142323`
  - function files: 32
  - zip created under the workspace `build` folder
  - council knowledge entry saved as `ddc62518-95ef-432c-a141-4c6de4e1dbf2`
- Release `v0.1.1-ai-council.20260602` was published at `https://github.com/Michi0403/LocalGPT/releases/tag/v0.1.1-ai-council.20260602`.
- The release includes Windows x64 MSIX/WebView2 zip artifacts, platform web-app zips, release notes, and manifest JSON.
- The installed x64 package used derived MSIX identity version `1.0.1.2` so Windows would accept it as an upgrade over earlier local packages.
- Packaged WebView2 smoke passed after the MSIX payload included published Blazor/DevExpress static assets.
- Deterministic council artifact routes generated downloadable `.cs`, `.dll`, `.razor`, and whole-solution zip artifacts through `/__artifacts/council/`.
- The generated .NET 10 DevExpress solution zip and the Ollama lab solution zip built with `dotnet build` at the time of the release smoke.
- Current Ollama lab generation check: `/__diag/council/artifact-smoke?target=ollama` produced `OllamaDotNetLab20260602132224-c515c40d.zip`, the extracted generated solution built with `dotnet build`, served `/`, `/api-console`, `/model-downloads`, and `/settings`, and answered `/api/version`, `/api/tags`, `/api/pull`, and `/api/chat`. Real Ollama lightweight comparison showed Ollama `0.24.0`, four installed model tags, and zero running models without loading a model.
- A CPU-only live DXAiChat council feature-artifact smoke with `deepseek-r1:8b` timed out or produced too little final text. Treat that as model-output health, not as proof that deterministic backend artifact generation failed.

Expected warning: Gradle 8.14.2 may report deprecated Gradle features for some generated Java builds. The starter builds still completed successfully.

## Preferred AI Test Paths

Prefer LocalGPT diagnostics over direct Ollama calls:

- `POST /__diag/dxaichat-smoke`
  - Exercises the configured `IChatClient` used by DXAiChat.
  - Returns raw text, visible text, extracted model thinking, and optional SQLite memory id.
  - Use this for single-model feedback about project progress and prompt behavior.
- `POST /__diag/council`
  - Exercises the AI Council through LocalGPT.
  - Logs under `%LOCALAPPDATA%\LocalGPT\CouncilLogs\`.
  - Saves council runs to SQLite memory when requested.
  - Saves every completed council run into the editable `CouncilKnowledgeEntries` table so later model calls can reuse grounded notes.
  - Can continue an older saved council conversation by sending `ContinueConversationId` or selecting the saved council memory in the frontend.
  - Every result step records the full council roster. Faulty members can be excluded from the next round by user action; models should propose exclusion only through a poll.
  - Development requests with unclear implementation ownership or scope should create an implementation-path poll. Offer concrete options such as sandbox prototype first, backend/data first, frontend UX first, or ask exact scope. The user can choose an option or type custom feedback, and the next council round must treat that decision as binding context.
  - `GenerateImplementationArtifact` creates sandbox artifacts under `%LOCALAPPDATA%\LocalGPT\CouncilArtifacts\` and returns safe `/__artifacts/council/{fileName}` download links. For Blazor/DevExpress frontend requests, it should emit a real `.razor` page artifact plus compileable `.cs` support code and a `.dll` when the support code builds.
  - Generated implementation ideas must stay as sandbox artifacts or temporary workspaces until the user explicitly permits integration. The council must never overrule a user decision that denies or limits self-expansion.
  - Use `MaxParallelModels = 1` for 20B/30B local models on 24 GB VRAM unless the user asks for heavier runs.
  - AMD 7900 XTX stability note: avoid full-auto GPU offload for qwen/gwen/gemma-class 27B/30B models after confirmed driver instability during some long 96%-100% load runs. Some black screens were later traced to display sleep, screen saver, or power-saving settings after system repair, so do not treat a black screen alone as proof of GPU failure. Prefer `OllamaNumGpu = 20`, `MaxParallelModels = 1`, `OllamaKeepAlive = "0s"`, short prompts, and short output budgets. Use full auto GPU only when Michi explicitly asks for the risk.
  - After a confirmed driver reset, high VRAM pressure, long 20B/30B stall, or black screen correlated with heavy model load, use low-resource council mode: `MaxRounds = 0`, `MaxOutputTokens = 1024`, `MaxContextTokens = 2048`, `OllamaKeepAlive = "0s"`, and `OllamaNumGpu = 0`. If the only symptom is a monitor sleep or power-saving wake issue, first check Windows/display power settings and recent logs. Smaller 256/512-token runs are useful for plumbing checks, but DeepSeek-style reasoning models may spend that whole budget on thinking.
- `GET /__diag/council/models`
  - Lists configured and installed Ollama models visible to LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`
  - Generates buildable smoke workspaces through the app service.
- `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4`
  - Generates the Living Cities vanilla datapack benchmark without loading Ollama.
  - Runs the generated `build-local.ps1`, validates JSON/function references, packages the zip, and saves a compact pinned `CouncilKnowledgeEntries` note.
  - Prefer this route plus the knowledge entry over sending the full Living Cities design document to every model.
- `GET /__diag/logs?minimumLevel=Warning&take=30`
  - Reads recent warnings/errors from the SQLite application log.
  - Returns the same short AI briefing that bootstrap context gives to DXAiChat and the AI Council.
  - Add `writeSmoke=true` to write and flush a harmless warning entry after logger changes.
- `GET /__diag/knowledge`
  - Reads editable council knowledge notes. These notes are included in AI bootstrap context as working memory, not absolute truth.
- `GET /__diag/sqlite/tables`
  - Lists live SQLite tables and row counts. The `/database` page uses the same table-editor service to let the frontend user inspect and edit local SQLite data with DevExpress controls.
- `GET /__diag/devexpress`
  - Reads DevExpress package references, Blazor imports, service registrations, and loaded assemblies when available.
  - Use this before asking the council for DevExpress Office/report/PDF/RichEdit/PdfViewer/Pivot features.
  - Office/report/file generation should be planned as ASP.NET Core backend services with safe download endpoints.
- `GET /__diag/blazor-devexpress-guidance`
  - Returns compact LocalGPT/TacosPortalOpen-derived guidance for generating real `.razor` pages with DevExpress Blazor components, Bootstrap v5 layout, DevExpress template starting points, and paired navigation SVG icon styles.
  - Use this before implementation-request artifact generation so the council does not produce C# classes that only return markup strings.
  - The same guidance is seeded into `CouncilKnowledgeEntries` as pinned, user-approved bootstrap knowledge.
- `GET /__diag/dotnet-sample-curriculum`
  - Returns official Microsoft/dotnet sample and Learn curriculum guidance for C#, .NET, ASP.NET Core, Blazor, EF Core, DevOps, architecture, and technician troubleshooting.
  - Use this before whole-solution generation, backend service generation, CI/release advice, or training/help prompts.
- `GET /__diag/build-debug-files?copy=true`
  - Lists and optionally copies `.pdb`, `.pdg`, and `.appxsym` build debug files into `%LOCALAPPDATA%\LocalGPT\BuildDebugFiles\`.
  - Use this for council diagnostics when source/reference usage is confusing, but do not treat symbol presence as proof of real feature usage.

## WebView2 Wrapper Diagnostics

The wrapper has a smoke mode for desktop-shell testing. Prefer running it from a registered/package identity or Visual Studio debug launch, because direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG` when the local runtime identity is not available.

For registered/package launches, create this flag before launching the app. The wrapper also checks the package-local `LocalCache\Local\LocalGPT\runtime` folder when running under MSIX identity:

```powershell
$runtime = "$env:LOCALAPPDATA\LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Set-Content -Path "$runtime\webview2-smoke.flag" -Value "exit" -Encoding utf8
```

The flag enables smoke mode once and `exit` asks the wrapper to close after writing snapshots.

```powershell
$app = Get-StartApps | Where-Object Name -like 'LocalGPT*' | Select-Object -First 1
Start-Process "shell:AppsFolder\$($app.AppID)"
```

It drives the embedded WebView2 through `/`, `/Chat`, and `/minecraft-mod-builder`, then writes JSON snapshots to:

```text
%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\
```

Use these snapshots to verify that the real desktop wrapper loads the Blazor app, the DXAiChat page, and the Minecraft Builder page.

## Known Gotchas

- Stop running `LocalGPT.exe` or wrapper instances before rebuilding; they can lock `bin` and `obj` outputs.
- The package/deploy path must be tested with Visual Studio MSBuild because `.wapproj` is not a normal SDK-only project.
- Build package scripts must explicitly check `$LASTEXITCODE` after native commands. `ErrorActionPreference = Stop` does not make `dotnet`, `MSBuild`, or packaging tools fail the PowerShell script by itself.
- If packaged WebView2 renders unstyled HTML with plain links and duplicated navigation, check MSIX payload mapping for Blazor and DevExpress static assets before chasing Razor rendering bugs.
- The package project must carry published `_content`, `_framework`, scoped CSS, static web asset manifests, and `LocalGPT.deps.json` through `AppxPackagePayload`. A loose `AppX` copy that is not listed in `package.map.txt` is not enough.
- MSIX package identity versions must be four-part numeric and upgrade-safe. Restore the checked-in manifest after release stamping so source control does not keep machine/generated version churn.
- Same-version MSIX replacement is unreliable during local testing. Bump the package identity version or remove the old LocalGPT package before reinstalling.
- Authenticode signing can look valid while `Add-AppxPackage` still rejects trust. The certificate helper supports local-machine trust when the user approves the UAC/admin step.
- AppX registration can fail with `0x80070002`/`0x80073CF9` when Windows holds a stale LocalGPT development registration or when the loose `AppX` layout misses manifest assets. The package project now copies `Images\*.png` into `bin\<platform>\<configuration>\AppX\Images`, and the repair script retries once after removing only the stale LocalGPT package identity.
- LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current app URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json` instead of assuming a fixed port.
- Application warnings/errors are stored in SQLite table `ApplicationLogs` when `LoggingCore:DatabaseCore:CoreLogLevel` allows them. The database logger is queued/background-flushed and excludes EF categories to avoid recursive logging.
- Missing-feature reports under `%LOCALAPPDATA%\LocalGPT\AIReports\` now include helpful source requests. AI participants should ask for official docs, examples, specs, package references, or sample repositories when needed, without pretending those sources were verified.
- The `/database` page is the live database editor. It has a friendly Council Knowledge panel plus a generic SQLite table preview/editor. Primary-key columns are displayed but protected in the generic form; edits are still applied to the live local database.
- Do not treat raw model output as verified facts. The council should mark uncertain claims as `Needs verification`.
- Some models, especially reasoning models, may return thinking-only output when the token budget is too small. LocalGPT separates thinking from visible text and must close the model-thinking `<details><pre>` block before it surfaces the "no final answer" notice; otherwise the fallback looks like more hidden/thinking text instead of a stopped visible answer.
- Keep the council database-first: use pinned `CouncilKnowledgeEntries`, selected saved conversations, and route outputs as concise grounding. Avoid huge prompt blobs unless a model explicitly needs one targeted excerpt.
- Official DevExpress/Microsoft source knowledge is backed by `docs/COUNCIL_KNOWLEDGE_SEED.sql`. LocalGPT imports this file with `INSERT OR IGNORE`, so it restores missing source-backed rows into SQLite without overwriting user edits or approval flags.
- Knowledge trust is explicit. Use `VerificationStatus` (`SourceBacked`, `UserVerified`, `ModelSuggested`, `NeedsVerification`, `Archived`) together with confidence and approval flags. Current user decisions and runtime diagnostics outrank workflow memory and model suggestions.
- Native command execution is intentionally narrow: commands must run under the LocalGPT Minecraft workspace root, executables are allowlisted, PowerShell must use `-File` against a workspace `.ps1`, and attempts/results are logged in the `NativeCommandLogs` SQLite table.
  The ledger includes `CommandProfile` values such as `GradleBuildOnly`, `GradleRunClient`, `JavaVersionOnly`, `PowerShellWorkspaceScript`, and `CustomAllowlistedCommand`.
- Formatting hardening is not about editor soft-wrap. Audit raw newline characters and physical line length. `build/Assert-SourceFormatting.ps1` checks tracked `.cs`, `.razor`, `.md`, `.ps1`, and `.json` files for physical lines over 600 characters and verifies key files such as `Program.cs`, `NativeCommandRunner.cs`, `AiContextBootstrapService.cs`, and `README.md` cannot collapse back into tiny raw-line counts. `.github/workflows/source-hygiene.yml` runs this guard on push and pull request.
- Whole-solution artifact generation is a first-class council test path. Use `/__diag/council/artifact-smoke?target=solution` to create a downloadable .NET 10 Blazor/DevExpress solution zip with `.sln`, `.csproj`, `.razor`, CSS, service/model code, README, and manifest, without loading Ollama.
- Minecraft datapack generation through DXAiChat/council artifacts is also a first-class test path. Use `/__diag/council/artifact-smoke?target=datapack` to create a downloadable zip via `/__artifacts/council/`. The zip root must contain `pack.mcmeta` and `data/` directly. For Minecraft 1.21+ use singular `data/<namespace>/function` and `data/minecraft/tags/function`; reject wrapper folders, `.mcfunction.txt`, invalid JSON tags, broken function references, leading slash commands, and root `data remove storage` reset syntax.
- Living Cities is a useful named datapack benchmark, not a hidden default for all datapack requests. Use `/__diag/minecraft/datapack-benchmark` for that comparison path; use DXAiChat/council artifact requests for prompt-driven datapacks.
- The selected local learn-base importer lives at `/__diag/learn-base/import`. It stores compact architecture fingerprints from `C:\tmpselectedcodexlearnbaseforlocalgpt` into CouncilKnowledgeEntries, focusing on functionality, architecture, protocols, host wiring, libraries, Python.NET interop, DevExpress Web API/security, bot/microservice patterns, and solution topology rather than names.
- Use `/__diag/benchmark/engineering` for the five-task personal engineering benchmark. LocalGPT artifact lanes can be tested without GPU; raw Ollama and cloud lanes must remain `NotRun` until real transcripts are supplied.
- DevExpress/Bootstrap design generation has a dedicated guide in `docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md`. Use Bootstrap v5 for containers, grids, responsive gutters, spacing, and flex utility layout. Use DevExpress controls for grids, forms, navigation, toolbars, dialogs, upload, charts, reports, AI chat, and other real app interactions. Generated navigation should include two SVG styles per concept: line icons for the default state and solid icons for hover/active states.
- Official Microsoft sample/curriculum generation has a dedicated guide in `docs/MICROSOFT_DOTNET_SAMPLE_CURRICULUM.md`. Use `dotnet/samples` as focused sample evidence and Microsoft Learn as the developer/technician curriculum baseline before asking the council to generate .NET solutions, services, Blazor pages, EF data access, CI workflows, or release guidance.
- The Ollama .NET lab artifact is a controlled feasibility path. Use `/__diag/council/artifact-smoke?target=ollama` to create a downloadable .NET 10 ASP.NET Core and DevExpress Blazor zip with Ollama-style route stubs, model catalog UI, model download planning, and settings. Expected route families include version, tags, running models, show, pull, push, create, copy, delete, generate, chat, and embed. It must say native GGML/GPU inference is not implemented unless a real backend is attached and approved by the user.
- Thinking-only/non-substantive council runs still remain in logs/chat memory, but they are archived or skipped for active council knowledge briefings. Duplicate benchmark knowledge entries are deduplicated by topic/scope/source before entering the bootstrap prompt.
- Diagnostic and artifact routes now live outside startup code. `Program.cs` maps normal middleware and calls `MapLocalGptDiagnosticEndpoints()` plus `MapMinecraftDiagnosticEndpoints()`, while route details live in `Endpoints/LocalGptDiagnosticEndpointExtensions.cs` and `Endpoints/MinecraftDiagnosticEndpointExtensions.cs`.
- Minecraft Java workspace generation now has `MinecraftDependencyVersionCatalog` and `/__diag/minecraft/dependency-version`. Use this before workspace generation so Fabric/NeoForge/Paper/datapack version decisions are explicit and unknown mappings are marked `NeedsVerification`.
- Direct backend debugging can fail before `WebApplication.CreateBuilder` when `LocalGPT.staticwebassets.runtime.json` points at missing generated `obj/.../compressed` or `scopedcss/bundle` folders. LocalGPT now recreates only those generated static-web-asset roots before builder creation. Do not create fake NuGet or DevExpress package roots; missing package roots are real restore/install issues.
- The desktop HTTP host intentionally does not use HTTPS redirection because it binds a random loopback HTTP port for WebView2. `HttpsRedirectionMiddleware` warnings in `ApplicationLogs` are noise for this host, not a user setup failure.
- The generic SQLite table editor must validate required columns and primary-key rules before insert/update/delete. Wrap `SqliteException` as user-readable table/operation errors so the `/database` page is useful instead of scary.
- Harmony-format local models can stream channel markers such as analysis/commentary/final.
  LocalGPT should prompt them to keep analysis bounded and always emit final-channel answer text.
  Render model-supplied analysis/commentary as a visible model-thinking block and final text as the answer.
  If only thinking arrives, close the thinking panel first, then render a clear incomplete-answer notice outside it.
  A Stop action is a normal cancellation and must not surface as an unhandled `TaskCanceledException`.
  Decode HTML-encoded Harmony markers before parsing so `&lt;|channel|&gt;final` does not leak into DXAiChat.
- Explicit generation requests are enough scope for sandbox artifacts. If the user asks through DXAiChat for a Minecraft datapack/modpack zip, `.cs`/`.razor`/`.dll`, whole .NET solution zip, or Ollama-inspired .NET lab, the council should produce safe downloadable `/__artifacts/council/` links and mark staged follow-up work as `Needs verification`. Use a poll only when a material architecture decision is genuinely missing; never create a poll and then claim the user failed to answer it in the same response.
- DXAiFunctions now advertise read-only SQLite access: `/__diag/sqlite/tables`, `/__diag/sqlite/table/{tableName}`, `/__diag/memory`, `/__diag/logs`, and `/__diag/knowledge`. Models should use these compact routes before guessing about previous chats, logs, approved knowledge, or database content.

## Collaboration Notes

Michi0403 wants concrete progress more than careful-sounding hesitation. When he asks to fix, build, test, release, or push, do the work and report the evidence. He appreciates directness, compiler output, diagnostics, and small meaningful commits. He can be stubborn, but in this project that usually means the product requirement is not satisfied yet; turn the stubborn signal into a test, a diagnostic route, or council knowledge.

Do not overdramatize hardware or Windows instability. Separate confirmed GPU driver resets from display sleep, screen saver behavior, package deployment errors, model latency, and WebView2 frontend problems. When a design path is genuinely unclear, the council should offer a small implementation poll with concrete options and then treat the user's choice as binding.

Respect user autonomy around generated code. The AI Council can propose features, generate sandbox artifacts, and ask for missing sources, but it must not integrate self-expansion into LocalGPT without explicit user approval. If Michi says no or limits the scope, the council must preserve that decision.

## Legacy C# Learning Sources

Michi0403 values Rob Miles and the Exam Ref 70-483 C# learning path as part of his developer foundation. Record that as respected user context, not as an objective ranking claim. The user-provided `C:/Users/micha/Downloads/Sample-Code-master.zip` contains classic Visual Studio/.NET Framework examples such as PLINQ exception handling, task creation/running, and task factory samples.

Use this material only as legacy C# architecture memory unless the user explicitly asks for .NET Framework output. For modern LocalGPT generation, translate the lessons into .NET 8-10 patterns: SDK-style projects, dependency injection, options/appsettings, nullable annotations, analyzers, async APIs, tests, and current ASP.NET Core/Blazor structure. Do not copy the sample archive into git; it includes `.vs`, `bin`, `obj`, `.exe`, and `.pdb` build outputs.

## Generation Archetype Contracts

The Council must treat "two different generated apps look basically the same" as a failed generation, not a visual polish issue. Whole-project generation now starts with an archetype contract in `docs/GENERATION_ARCHETYPE_CONTRACTS.md` and matching pinned SQLite seed rows.

Every whole-project artifact must include `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `BUILD_AND_RUN.md`, `.localgpt-generation.json`, a platform-correct layout, a user-visible index/home route, and navigation. The artifact service validates those required files before zipping. LocalGPT feature artifacts should look like LocalGPT/TacosPortalOpen feature sandboxes; Ollama .NET lab artifacts should look like API-control-plane experiments with explicit native-runner boundaries.

The artifact service also parses `.localgpt-generation.json` and `LocalGPT.GenerationManifest.json` before zipping. Required contract fields include `project_kind`, `target_platform`, complexity flags, expected entry points, generated files, validation status, and build/test result provenance. A zip may say `GeneratedOnlyContractValidated`, but it must not claim build success unless the command output exists.

Ground modern .NET generation in Microsoft Learn architecture guidance: cohesive Blazor/ASP.NET Core apps by default, service boundaries only when real deployment/scaling/integration boundaries exist, UI in Razor, business/native/data work in services, durable state in EF/SQLite, and diagnostics for runtime features.

For EF Core entity generation, use `docs/EF_DEVEXPRESS_BUSINESS_OBJECTS.md` before emitting business objects. Ask whether the target is DevExpress Web API/XAF/OData-compatible or a plain EF backend, then decide attribute metadata versus ModelBuilder, explicit FK/navigations versus shadow properties, lazy/loading/change-tracking style, delete behavior, naming constraints, and nullable-first migration strategy for existing data.

## Next Useful Checks

- Rerun `POST /__diag/dxaichat-smoke` after restarting LocalGPT from a fresh build.
- Rerun a short AI Council feedback prompt only after checking `ollama ps`. If the machine recently showed confirmed GPU pressure, a driver reset, or a black screen correlated with heavy model load, use one model with `OllamaNumGpu = 0`, `OllamaKeepAlive = "0s"`, `MaxRounds = 0`, `MaxContextTokens = 2048`, and `MaxOutputTokens = 1024` for reasoning models.
- Check `/__diag/logs?minimumLevel=Warning&take=30` before asking the council for setup advice; recent Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, or package errors should be treated as actionable health signals.
- Run the WebView2 smoke mode from a registered/package identity or Visual Studio debug launch and inspect `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`. Use this as the preferred frontend fallback for LocalGPT usability checks instead of relying on an assistant built-in browser; it exercises the real wrapper routes, including `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`.
- Commit and push diagnostic changes in small slices.
