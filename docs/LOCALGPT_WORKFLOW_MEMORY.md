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

- `e6ac15f` - Harden package deploy and release packaging.
- `7e207b9` - Add Minecraft builder target choices.
- `28becc5` - Add LocalGPT diagnostic smoke workflows.
- `6238bb0` - Add package-friendly WebView2 smoke trigger.

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
  - `GenerateImplementationArtifact` creates sandbox artifacts under `%LOCALAPPDATA%\LocalGPT\CouncilArtifacts\` and returns safe `/__artifacts/council/{fileName}` download links. For Blazor/DevExpress frontend requests, it should emit a real `.razor` page artifact plus compileable `.cs` support code and a `.dll` when the support code builds.
  - Generated implementation ideas must stay as sandbox artifacts or temporary workspaces until the user explicitly permits integration. The council must never overrule a user decision that denies or limits self-expansion.
  - Use `MaxParallelModels = 1` for 20B/30B local models on 24 GB VRAM unless the user asks for heavier runs.
  - AMD 7900 XTX stability note: avoid full-auto GPU offload for qwen/gwen/gemma-class 27B/30B models after the driver showed black-screen instability under long 96%-100% load. Prefer `OllamaNumGpu = 20`, `MaxParallelModels = 1`, `OllamaKeepAlive = "0s"`, short prompts, and short output budgets. Use full auto GPU only when Michi explicitly asks for the risk.
  - After a driver reset, black screen, or high VRAM pressure, use low-resource council mode: `MaxRounds = 0`, `MaxOutputTokens = 1024`, `MaxContextTokens = 2048`, `OllamaKeepAlive = "0s"`, and `OllamaNumGpu = 0`. This is slower, but it keeps the GPU out of the test run and explicitly unloads the model after each participant. Smaller 256/512-token runs are useful for plumbing checks, but DeepSeek-style reasoning models may spend that whole budget on thinking.
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
  - Returns compact LocalGPT/TacosPortalOpen-derived guidance for generating real `.razor` pages with DevExpress Blazor components.
  - Use this before implementation-request artifact generation so the council does not produce C# classes that only return markup strings.
  - The same guidance is seeded into `CouncilKnowledgeEntries` as pinned, user-approved bootstrap knowledge.
- `GET /__diag/build-debug-files?copy=true`
  - Lists and optionally copies `.pdb`, `.pdg`, and `.appxsym` build debug files into `%LOCALAPPDATA%\LocalGPT\BuildDebugFiles\`.
  - Use this for council diagnostics when source/reference usage is confusing, but do not treat symbol presence as proof of real feature usage.

## WebView2 Wrapper Diagnostics

The wrapper has a smoke mode for desktop-shell testing. Prefer running it from a registered/package identity or Visual Studio debug launch, because direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG` when the local runtime identity is not available.

For registered/package launches, create this flag before launching the app:

```powershell
$runtime = "$env:LOCALAPPDATA\LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Set-Content -Path "$runtime\webview2-smoke.flag" -Value "exit" -Encoding utf8
```

The flag enables smoke mode once and `exit` asks the wrapper to close after writing snapshots.

```powershell
$env:LOCALGPT_WEBVIEW2_SMOKE = "1"
$env:LOCALGPT_WEBVIEW2_SMOKE_EXIT = "1"
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net10.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

It drives the embedded WebView2 through `/`, `/Chat`, and `/minecraft-mod-builder`, then writes JSON snapshots to:

```text
%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\
```

Use these snapshots to verify that the real desktop wrapper loads the Blazor app, the DXAiChat page, and the Minecraft Builder page.

## Known Gotchas

- Stop running `LocalGPT.exe` or wrapper instances before rebuilding; they can lock `bin` and `obj` outputs.
- The package/deploy path must be tested with Visual Studio MSBuild because `.wapproj` is not a normal SDK-only project.
- AppX registration can fail with `0x80070002`/`0x80073CF9` when Windows holds a stale LocalGPT development registration or when the loose `AppX` layout misses manifest assets. The package project now copies `Images\*.png` into `bin\<platform>\<configuration>\AppX\Images`, and the repair script retries once after removing only the stale LocalGPT package identity.
- LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current app URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json` instead of assuming a fixed port.
- Application warnings/errors are stored in SQLite table `ApplicationLogs` when `LoggingCore:DatabaseCore:CoreLogLevel` allows them. The database logger is queued/background-flushed and excludes EF categories to avoid recursive logging.
- Missing-feature reports under `%LOCALAPPDATA%\LocalGPT\AIReports\` now include helpful source requests. AI participants should ask for official docs, examples, specs, package references, or sample repositories when needed, without pretending those sources were verified.
- The `/database` page is the live database editor. It has a friendly Council Knowledge panel plus a generic SQLite table preview/editor. Primary-key columns are displayed but protected in the generic form; edits are still applied to the live local database.
- Do not treat raw model output as verified facts. The council should mark uncertain claims as `Needs verification`.
- Some models, especially reasoning models, may return thinking-only output when the token budget is too small. LocalGPT now separates thinking from visible text and should surface a clear placeholder if no final answer appears.
- Keep the council database-first: use pinned `CouncilKnowledgeEntries`, selected saved conversations, and route outputs as concise grounding. Avoid huge prompt blobs unless a model explicitly needs one targeted excerpt.
- Official DevExpress/Microsoft source knowledge is backed by `docs/COUNCIL_KNOWLEDGE_SEED.sql`. LocalGPT imports this file with `INSERT OR IGNORE`, so it restores missing source-backed rows into SQLite without overwriting user edits or approval flags.
- Knowledge trust is explicit. Use `VerificationStatus` (`SourceBacked`, `UserVerified`, `ModelSuggested`, `NeedsVerification`, `Archived`) together with confidence and approval flags. Current user decisions and runtime diagnostics outrank workflow memory and model suggestions.
- Native command execution is intentionally narrow: commands must run under the LocalGPT Minecraft workspace root, executables are allowlisted, PowerShell must use `-File` against a workspace `.ps1`, and attempts/results are logged in the `NativeCommandLogs` SQLite table.
- Formatting hardening is not about editor soft-wrap. Audit raw newline characters and physical line length. Current threshold check: no tracked `.cs`, `.razor`, or `.md` source/docs outside build outputs should contain physical lines over 600 characters.
- Whole-solution artifact generation is a first-class council test path. Use `/__diag/council/artifact-smoke?target=solution` to create a downloadable .NET 10 Blazor/DevExpress solution zip with `.sln`, `.csproj`, `.razor`, CSS, service/model code, README, and manifest, without loading Ollama.
- The Ollama .NET lab artifact is a controlled feasibility path. Use `/__diag/council/artifact-smoke?target=ollama` to create a downloadable .NET 10 ASP.NET Core and DevExpress Blazor zip with selected Ollama-style API stubs and model catalog UI. It must say native GGML/GPU inference is not implemented unless a real backend is attached and approved by the user.
- Thinking-only/non-substantive council runs still remain in logs/chat memory, but they are archived or skipped for active council knowledge briefings. Duplicate benchmark knowledge entries are deduplicated by topic/scope/source before entering the bootstrap prompt.

## Next Useful Checks

- Rerun `POST /__diag/dxaichat-smoke` after restarting LocalGPT from a fresh build.
- Rerun a short AI Council feedback prompt only after checking `ollama ps`. If the machine recently showed a black screen or GPU pressure, use one model with `OllamaNumGpu = 0`, `OllamaKeepAlive = "0s"`, `MaxRounds = 0`, `MaxContextTokens = 2048`, and `MaxOutputTokens = 1024` for reasoning models.
- Check `/__diag/logs?minimumLevel=Warning&take=30` before asking the council for setup advice; recent Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, or package errors should be treated as actionable health signals.
- Run the WebView2 smoke mode from a registered/package identity or Visual Studio debug launch and inspect `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`. Use this as the preferred frontend fallback for LocalGPT usability checks instead of relying on an assistant built-in browser; it exercises the real wrapper routes, including `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`.
- Commit and push diagnostic changes in small slices.
