# Architecture for AI Contributors

## Short version

LocalGPT is a local-first AI engineering workbench. A WinUI 3 WebView2 shell
hosts a Blazor/ASP.NET Core server that owns the UI, DevExpress components,
AI/Ollama configuration, SQLite memory, council knowledge, downloadable artifact
routes, Minecraft generation, diagnostics, and native command services.

Current product shape:

- DXAiChat for local model chat with memory and visible thinking parsing.
- AI Council for multi-model review, correction, polls, logs, and memory.
- Test Lab for local diagnostics before loading heavy models.
- Downloadable `.cs`, `.razor`, `.dll`, solution, AI-host, and datapack artifacts.
- Minecraft Java support for current 26.1 datapacks plus Fabric, NeoForge, Paper,
  and 1.21.x legacy comparison/starter paths.
- Source-backed offline engineering knowledge from Microsoft .NET/C# compiler
  docs, Windows docs, DevExpress/Bootstrap guidance, EF rules, local learn-base
  imports, logs, and project workflow memory.

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
- `IncludeLocalGptPublishedPayload` defaults to `false` so Visual Studio Debug/F5 does not double-include published Blazor assets
- release and publish scripts may opt in with `IncludeLocalGptPublishedPayload=true`; keep that opt-in explicit to avoid APPX1111 duplicate payload entries
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
it summarizes LocalGPT/TacosPortalOpen server-interactive Razor patterns plus Bootstrap v5
layout, DevExpress template starting points, and navigation SVG icon style rules.
Use `GET /__diag/frontend-design-guidance` before generating a frontend from screenshots,
goal applications, or broad app-design prompts. It returns LocalGPT's compiled frontend
pattern library: archetypes, Windows/Fluent design principles, Bootstrap layout,
DevExpress/custom Razor components, services, and accessibility states.
When a user asks for DevExpress Office document generation, report generation, PDF export,
RichEdit/PdfViewer/Pivot integration, or generated downloadable files, place generation work in
ASP.NET Core/Blazor server backend services and expose safe download endpoints.
The frontend should call backend services and display status/download links.
Do not invent DevExpress APIs beyond the referenced package/version family;
mark uncertain APIs as `Needs verification`.
Use `GET /__diag/dotnet-sample-curriculum` before whole-solution generation,
backend service generation, CI/release advice, or .NET technician help. It
summarizes official `dotnet/samples`, Microsoft Learn paths, ASP.NET Core,
Blazor, EF Core, DevOps/testing/deployment, and architecture/Aspire boundaries.
Use `GET /__diag/ai-host-rebuild-guidance` before AI-host/control-plane generation.
That route includes the required .NET DI/options/hosted-service/plugin/native-runner
patterns, Python.NET/PowerShell adapter boundaries, and capability-gap rules.

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

Best results usually come from combining several compatible offline models
instead of treating one model as the whole team. A smaller fast model can
summarize context, a code model can draft implementation, a reasoning model can
review architecture, and a second model can check missing files or build risks.
Keep GPU pressure realistic and let the database carry shared memory instead of
stuffing every source file into every turn.

AI coding agents such as Codex are also part of the intended workflow. The
council can request missing LocalGPT functions, identify weak knowledge, or
produce benchmark evidence; an agent can implement those mechanics, run builds,
commit, publish, and feed verified results back into council knowledge. Treat the
human user as the decision owner and agents as maintainers of the LocalGPT body
around the models.

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

Knowledge lifecycle rule: council memory is maintained, not merely accumulated.
Each knowledge entry has a verification status, review status, optional expiry
date, last verified/used timestamps, optional supersession link, stale reason,
source hash, and source date. `Current` and user-approved/source-backed entries
are the strongest prompt evidence. `NeedsUserReview`, `NeedsSourceRefresh`, and
`NeedsDiagnosticVerification` may be shown with explicit caution. `Expired`,
`Deprecated`, `Superseded`, and `Archived` entries remain visible in the database
for humans but must not be injected into bootstrap as trusted facts.

If a model says it cannot complete a task because it lacks information or a
function, it should not stop at refusal. It should emit a capability-gap block
that names the missing language/framework/version/domain knowledge, local or
official sources needed, missing LocalGPT functions, and a safe artifact plan.
That gap becomes reviewable database knowledge and future product work.

Decision rule: if a participant is unavailable, the council cannot converge, or the final answer still needs human verification, the result should include a user decision poll. The poll is saved into memory and shown in the UI so the next council round can treat the user's choice as binding shared context.

Implementation-path rule: when a user asks for development work and the ownership, scope, or implementation path is unclear,
the council should not silently choose one path. It should offer concrete implementation possibilities in a user decision poll,
such as sandbox prototype first, backend/data first, frontend UX first, or ask exact scope. The user can choose an option
or type custom feedback; the next council round must treat that decision as binding context.

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

Capability gaps are stronger than ordinary missing-feature notes. When the user says a model refused,
lacked knowledge, missed a framework/version, produced the wrong artifact shape, or needs better functions,
treat that as approved product feedback. Emit a `Capability gap report` and a `<localgpt-capability-gap>`
block that names requested languages, frameworks, versions, domain knowledge, local sources, external sources,
missing LocalGPT functions, safe workflow, and artifact plan.
LocalGPT stores these blocks as unapproved SQLite knowledge entries and also writes report files,
so later council runs can improve instead of rediscovering the same gap.

Local sources come first: DXAiFunctions, SQLite knowledge/logs/memory, local docs, learn-base imports, generated artifacts, build logs, Test Lab output, and WebView2 diagnostics. External sources are separate work: official docs, official GitHub repositories, package/version docs, provider API docs, version manifests, or user-approved source imports. Do not silently download or trust external material.

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
- have the AI Council help with user setup as well as mod code: Java 25 for current Minecraft Java 26.x, Java 21 for 1.21.x legacy targets, Gradle, Eclipse/IDE import, Minecraft launcher, Ollama models, and generated workspace build steps

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

Use `GET /__diag/council/development-feedback-talk` for the regular minimum-two-member Council feedback talk about
LocalGPT development process, missing features, benchmark gaps, and needed DXAiFunctions or knowledge sources. This is
also exposed from the Test Lab frontend as `Council Feedback`. It should save memory/knowledge and include a capability
gap report when the Council sees missing functionality. Do not treat 4K/8K as serious source-generation context; they
are tiny smoke-test budgets. Local Ollama coding runs should default around 32K context, use 64K or more for larger
source/solution generation, and allow up to 256K when the model/runtime supports it.

Use `GET /__diag/benchmark/engineering?taskSet=replacement&validateBuildableArtifacts=true` for the replacement benchmark. It should generate and score downloadable LocalGPT-style, TacosPortalOpen-style, provider-compatible AI-host, and simple bot-backend solution zips through LocalGPT's artifact path, then build-check .NET solution artifacts. Raw Ollama and cloud comparison lanes must use real transcripts; do not fake them.

Use `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge` to generate a buildable workspace through the app service, then run the generated `build-local.ps1`.

Use `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=26.1` for the current datapack benchmark. It does not load Ollama; it creates the datapack workspace, runs the local validator/zip script, and saves a compact council knowledge entry for later model review. Use `minecraftVersion=1.21.4` only when intentionally comparing against legacy 1.21.x behavior.

Use `GET /__diag/logs?minimumLevel=Warning&take=30` to inspect persisted app health before asking the AI Council for setup advice.

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Diagnostics should discover the current URL from:

```text
%LOCALAPPDATA%\LocalGPT\runtime\server.json
```

The WinUI wrapper supports a WebView2 smoke mode. Prefer a registered/package identity or Visual Studio debug launch for this final frontend check, because direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG`:

This WebView2 smoke path is the preferred fallback for human-usability checks of LocalGPT itself. Do not treat an external or assistant-provided browser as enough evidence for desktop-shell behavior; use WebView2 diagnostics when validating the real wrapped UI.

For Council/product-workflow validation, the WebView2 UI must pass a visible
post-prerender preflight before work starts unless the user explicitly labels the
task documentation-only. The real window must be visible, the target page must be
interactive after prerendering, overlays must be gone, the route must stay stable
after observation, and the needed controls must be visible and enabled. Agents must
not substitute sandbox browsers, backend routes, direct model calls, diagnostic URL
state changes, or rapid command bursts. LocalGPT product workflow automation should
advance at human pace: one visible action, wait for render/control state to settle,
inspect, then continue.

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

LocalGPT writes its active loopback endpoint to:

```text
%LOCALAPPDATA%\LocalGPT\runtime\server.json
```

Use that file when smoke-testing published backend builds because LocalGPT chooses a free port at startup. Set `LOCALGPT_STARTUP_TRACE=1` only while diagnosing startup; it prints phase markers around configuration, service registration, middleware, and endpoint-file creation.

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
