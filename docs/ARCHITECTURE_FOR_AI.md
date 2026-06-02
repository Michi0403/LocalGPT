# Architecture for AI Contributors

## Short version

LocalGPT is a Windows desktop app shell around a Blazor/ASP.NET Core server. The server owns the UI, DevExpress components, AI/Ollama configuration, chat behavior, and native command services. The WinUI 3 wrapper exists to launch and display that server through WebView2.

## Key idea

The important architectural distinction is this:

**WinUI is the host shell; Blazor/ASP.NET Core is the real application.**

That means most feature work belongs in `LocalGPT`, not in the wrapper.

## Why the model looks heavier than a normal Blazor app

A normal Blazor app is launched by Kestrel or IIS and opened in an external browser.

This project intentionally goes further:

- a WinUI executable starts the app
- WebView2 displays the local server
- MSIX/DesktopBridge handles deployment/debugging
- static web assets must be copied into the packaged executable layout
- the app can expose local desktop-oriented capabilities while keeping a browser-debuggable UI

## Package and runtime model

The packaged app should not ask Edge to download a .NET desktop runtime during debug.

Current expectations:

- LocalGPT targets .NET 10 and the wrapper is framework-dependent on the installed .NET 10 Desktop Runtime
- the repair script installs `Microsoft.DotNet.DesktopRuntime.10` with winget when `-InstallMissingRuntime` is used
- the package project overlays the wrapper publish output into AppX
- Windows App SDK debug framework references are avoided in the manifest
- `LocalGPT.staticwebassets.runtime.json` is copied beside the packaged executable
- loose AppX image assets are copied into `bin\<platform>\<configuration>\AppX\Images`; missing images or stale package registrations can appear as `0x80070002`, `0x80073CF9`, or `DEP1000`
- `Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register` retries once by removing only the stale LocalGPT development package identity

## DevExpress static assets

DevExpress Blazor relies on ASP.NET Core static web assets.

If these fail:

- `/LocalGPT.styles.css`
- `/_content/DevExpress.Blazor.Resources/js/import-scripts.js`
- `/_content/DevExpress.Blazor.Themes/*.css`

then the package probably missed `LocalGPT.staticwebassets.runtime.json`.

For DevExpress 25, the main module path is:

```text
/_content/DevExpress.Blazor/modules/dx-blazor-all.js
```

The AI bootstrap includes a DevExpress inventory built from `LocalGPT.csproj`, `_Imports.razor`, `Program.cs`,
and loaded assemblies when available. Use `GET /__diag/devexpress` to inspect it.
Use `GET /__diag/blazor-devexpress-guidance` before asking the council to generate Blazor pages;
it summarizes LocalGPT and TacosPortalOpen server-interactive Razor patterns.
When a user asks for DevExpress Office document generation, report generation, PDF export,
RichEdit/PdfViewer/Pivot integration, or generated downloadable files, place generation work in
ASP.NET Core/Blazor server backend services and expose safe download endpoints.
The frontend should call backend services and display status/download links.
Do not invent DevExpress APIs beyond the referenced package/version family;
mark uncertain APIs as `Needs verification`.

## AI configuration model

The setup page is the user-facing control surface for AI configuration.

Contributors should preserve:

- multiple AI/Ollama profile support
- reliable save/load behavior
- non-null defaults for optional configuration sections
- clear error reporting when configuration cannot be persisted
- separation between connectivity probing and chat execution

## Ollama debugging model

Use Ollama as the first local debugging target for AI features.

The preferred model id is `gpt-oss:20b`. Do not use `gpt-oss-20b` in configuration; Ollama model ids use a colon tag.

Before investigating `DxAIChat`, run:

```powershell
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1
```

If the script shows that `/api/chat` or `/api/generate` returns empty output, fix or restart Ollama first. `DxAIChat` can only display what the configured `IChatClient` receives from Ollama.

For `gpt-oss:20b`, empty `content` with non-empty `thinking` and `done_reason: length` usually means the test budget was too small. Increase `-NumPredict` before assuming the model is broken.

## Grounded model feedback

It is useful to ask `gpt-oss:20b` for process feedback during larger changes, but it must be treated as a grounded reviewer rather than an authority.

Use:

```powershell
.\LocalGPTWebviewWrapper\build\Test-GptOssProcessReview.ps1 -Facts "dotnet build LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Debug passed with 0 warnings and 0 errors" -Facts "Commit ac9743b added chat memory UI and help surfaces"
```

The review prompt requires the model to use only supplied evidence, place unsupported ideas under "Needs verification", and avoid inventing file paths, test results, commits, or user decisions.

When the Blazor app is running, `/__diag/process-review` provides the same grounded review behavior through the configured `IChatClient` and includes recent saved chat memory as evidence.

## Multi-model council

The AI Council feature lets multiple local Ollama models collaborate on one prompt.

Current behavior:

- model candidates are discovered from configured Ollama settings and `/api/tags`
- selected models run proposal phases in parallel
- later phases receive the transcript so far and correct/refine earlier work
- a consensus response is produced and optionally peer-verified
- each result and transcript step records the full council member list, not only the speaking model
- the full transcript, visible model reasoning notes, errors, and timings are written to a Markdown log under `%LOCALAPPDATA%\LocalGPT\CouncilLogs`
- the transcript is also saved into the existing SQLite chat memory as an `AI Council - model + model` conversation
- users can select a saved council memory conversation in the Council page and continue it; LocalGPT gives the latest saved messages to the next run as selected continuation context and saves the new round back into the same EF/SQLite conversation
- users can exclude a faulty, unavailable, too slow, or hallucination-prone member from the next round from the frontend; models may recommend exclusion only through a user decision poll
- users can start a dedicated implementation-request council chat.
  This enables sandbox implementation artifacts for .NET/Blazor/ASP.NET Core style feature ideas and exposes them through safe
  `/__artifacts/council/{fileName}` download links.
  For Blazor frontend requests, LocalGPT should generate a real `.razor` component/page artifact plus compileable `.cs` support code;
  a C# class that only builds strings is not enough unless the user asked for that shape.
  Requested features should be prototyped in a harmless sandbox artifact or temporary workspace and smoke-tested before integration
  into the real project structure.
  LocalGPT must never self-expand or integrate generated features without explicit user permission,
  and a user decision that denies or limits expansion must not be overruled.

Performance rule: default council scheduling runs one model inference at a time (`MaxParallelModels = 1`), keeps default prompts smaller (`MaxContextTokens = 4096` and `MaxOutputTokens = 1024` on new requests), applies a per-model timeout, and uses a short Ollama keep-alive when several large local models are selected. This avoids trying to keep multiple 20B/30B models resident in VRAM at once on machines like a 7900 XTX with 24 GB VRAM. Users can raise the parallelism or context in the UI when they know the loaded models fit together.

AMD 7900 XTX heavy-model guardrail: qwen/gwen/gemma-class 27B/30B models caused confirmed driver instability
during some long full-load runs, especially near 96%-100% GPU load. Some observed black screens were later traced
to display sleep, screen saver, or power-saving settings after system repair, so do not diagnose every black screen
as a GPU crash. Treat heavy 27B/30B models as limited-layer models by default, and escalate only when there is
supporting evidence such as driver reset, full-load timing, Ollama stall, logs, or user confirmation.
The backend council now applies a `num_gpu=20` guardrail for those model names when the caller did not explicitly set
`OllamaNumGpu`; helper scripts should also default to balanced GPU layers. Full auto GPU for those models is an
explicit user-risk override, not the default.

Low-resource rule: after a confirmed driver reset, high VRAM pressure, long 20B/30B stall, or black screen that
correlates with heavy model load, run with `MaxRounds = 0`, `MaxOutputTokens = 1024`, `MaxContextTokens = 2048`,
`OllamaKeepAlive = "0s"`, and `OllamaNumGpu = 0`. If the only symptom is a monitor sleep or power-saving wake issue,
first verify Windows/display power settings and recent logs before assuming GPU failure. LocalGPT forwards `num_gpu=0`
to Ollama and sends an unload request after zero-keepalive participant calls. This path is slower, but it keeps council
tests alive without forcing another large GPU residency cycle. Use 256 or 512 output tokens only for plumbing checks;
reasoning models can spend that entire budget before producing visible text.

Database-first rule: council bootstrap should prefer pinned `CouncilKnowledgeEntries`, selected saved council conversations, recent log health summaries, and deterministic diagnostic route output over huge pasted source/design contexts. If more detail is needed, ask for a targeted excerpt or create a smaller knowledge entry first.

Decision rule: if a participant is unavailable, the council cannot converge, or the final answer still needs human verification, the result should include a user decision poll. The poll is saved into memory and shown in the UI so the next council round can treat the user's choice as binding shared context.

Frustration rule: if the user's prompt sounds angry, blocked, or frustrated, the council must stay kind, avoid blame, and turn the emotion into a technical recovery poll. Poll options should cover stabilization, missing-feature implementation, and scope reduction. The selected path and any missing LocalGPT feature request should be saved into SQLite chat memory so later models can see it.

Design rule: the council can display provider-supplied visible thinking and model-written reasoning notes, but user-facing controls should make it clear which model produced each note. Treat council output as reviewed assistance, not as automatically true.

## SQLite application log awareness

LocalGPT stores recent application warnings and errors in the same SQLite database used for chat memory.

Current behavior:

- `LoggingCore:DatabaseCore:CoreLogLevel` controls the minimum persisted level.
- the database logger uses a bounded queue and background flushes so request/UI threads are not blocked by SQLite writes.
- the queue drops oldest entries if it is full; diagnostics should stay useful without freezing the app.
- Entity Framework categories are excluded from the database logger to avoid recursive logging while writing logs.
- existing user databases are upgraded with `CREATE TABLE IF NOT EXISTS` for `ApplicationLogs`, because the app uses `EnsureCreated` instead of EF migrations.
- `/__diag/logs?minimumLevel=Warning&take=30` returns recent persisted logs and the AI briefing text.
- `/__diag/logs?writeSmoke=true` writes a harmless warning entry and waits for a flush, which is useful after changing logging code.

AI bootstrap includes a short warning/error briefing. Treat it as a health signal: if it mentions missing Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, package registration, or model setup, explain likely local fixes and mark uncertain details as `Needs verification`.

## Missing feature reports

When AI output identifies a missing LocalGPT capability, LocalGPT writes a report under `%LOCALAPPDATA%\LocalGPT\AIReports\`. Reports should include a `Helpful sources requested` section when the model would benefit from official docs, examples, versioned package references, specs, tutorials, or sample repositories. Source requests are not verification; models must say `Needs verification` until those sources are actually supplied or inspected.

## Build debug symbols

LocalGPT exposes a build symbol inventory at `GET /__diag/build-debug-files`. It lists `.pdb`, `.pdg`, and `.appxsym` files from current output paths. Add `copy=true` to copy the current symbols under `%LOCALAPPDATA%\LocalGPT\BuildDebugFiles\` for council diagnostics. These files are not committed to git. Treat them as build/debug evidence only; symbol presence, generated references, or component imports are not proof that source code uses a feature.

## Minecraft mod generation model

The intended direction is to let LocalGPT create complex Java Minecraft mods on command.

Java Edition is the first-class target. Fabric is the fast iteration mod target, NeoForge is the modern Forge-style mod target, Paper is the server-side Java plugin target, and vanilla datapacks are the no-Java command/data target. Bedrock should be handled later as a separate behavior/resource pack exporter.

Recommended architecture:

- keep generation state in a workspace service
- keep native commands behind `INativeCommandRunner`
- store logs and generated files where users can inspect them
- make build steps repeatable through scripts or service methods
- keep frontend JavaScript limited to client-side helper behavior
- have the AI Council help with user setup as well as mod code: JDK 21, Gradle, Eclipse/IDE import, Minecraft launcher, Ollama models, and generated workspace build steps

Feature wishlist gathered from the local `gpt-oss:20b` debug model:

- mod/plugin/datapack template library for Fabric, NeoForge, Paper, vanilla datapacks, and future Bedrock exports
- dependency resolver for Fabric API, Yarn, NeoForge, Paper API, Minecraft versions, and datapack pack formats
- version sync between metadata, Gradle, generated code, and assets
- generated README/changelog/API docs
- JUnit test generation for common block/item/command behavior
- static safety analysis for risky reflection, network, filesystem, or command patterns
- sandboxed run/build workflow before deploying generated mods

Treat these as product direction, not as already-implemented behavior.

The detailed Minecraft builder rules live in `docs/MINECRAFT_MOD_AI_BUILDER.md`. The bootstrap prompt includes that file so chat and council participants can explain setup and report missing builder features consistently.

## Diagnostic test paths

Prefer testing through LocalGPT services before calling Ollama directly.

Use `POST /__diag/dxaichat-smoke` to exercise the configured `IChatClient` used by the DXAiChat page. It returns raw text, visible text, extracted model thinking, and optionally saves the exchange to SQLite memory.

Use `POST /__diag/council` to exercise the AI Council through LocalGPT. Keep `MaxParallelModels = 1` for 20B/30B local models on consumer GPUs unless the user explicitly wants a heavier run.

Use `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge` to generate a buildable workspace through the app service, then run the generated `build-local.ps1`.

Use `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4` for the Living Cities datapack benchmark. It does not load Ollama; it creates the datapack workspace, runs the local validator/zip script, and saves a compact council knowledge entry for later model review.

Use `GET /__diag/logs?minimumLevel=Warning&take=30` to inspect persisted app health before asking the AI Council for setup advice.

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Diagnostics should discover the current URL from:

```text
%LOCALAPPDATA%\LocalGPT\runtime\server.json
```

The WinUI wrapper supports a WebView2 smoke mode. Prefer a registered/package identity or Visual Studio debug launch for this final frontend check, because direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG`:

This WebView2 smoke path is the preferred fallback for human-usability checks of LocalGPT itself. Do not treat an external or assistant-provided browser as enough evidence for desktop-shell behavior; use WebView2 diagnostics when validating the real wrapped UI.

```powershell
$runtime = "$env:LOCALAPPDATA\LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Set-Content -Path "$runtime\webview2-smoke.flag" -Value "exit" -Encoding utf8
```

The flag enables smoke mode once for registered/package launches.

```powershell
$env:LOCALGPT_WEBVIEW2_SMOKE = "1"
$env:LOCALGPT_WEBVIEW2_SMOKE_EXIT = "1"
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net10.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

This drives the embedded WebView2 through `/`, `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`, clicks the Council page's implementation-request chat starter, and writes JSON snapshots to:

```text
%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\
```

Use this when validating that the native shell can load the real Blazor app, not just the ASP.NET server endpoint.

## If you are changing code

Ask yourself:

- Does this preserve the Blazor/server ownership of app behavior?
- Does this keep the WinUI wrapper thin?
- Does this preserve DevExpress static asset loading?
- Does this keep configuration compatible with existing user data?
- Does this route native execution through backend services?
- Does the full package still build and deploy?

If not, rethink the change before editing.
