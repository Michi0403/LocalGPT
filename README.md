# LocalGPT

LocalGPT is a local-first AI engineering workbench for Windows, .NET, DevExpress,
and Minecraft creation. It runs as a Blazor/ASP.NET Core app inside a WinUI 3
WebView2 desktop shell, uses local Ollama models by default, and turns chats into
memory, diagnostics, and downloadable build artifacts.

It is technical, but meant to feel calm: local context, clear tools, safe
downloads, and a council of models that can work together instead of guessing in
one giant prompt.

## Why The Council Matters

LocalGPT is strongest when several offline models work together. One model can be
fast, one can be careful, one can be better at code, and another can be better at
Windows, design, or long technical discussion. The AI Council turns that into a
shared conversation with memory, visible roles, user polls, and downloadable
artifacts.

AI agents such as Codex can also work with the council. A practical flow is:

- a user asks LocalGPT or the AI Council for a feature, diagnosis, design review,
  Minecraft datapack, or .NET solution
- the council discusses the path and records missing knowledge or missing
  LocalGPT functions
- Codex or another coding agent fixes LocalGPT, imports better knowledge, runs
  tests, commits, publishes, and documents the result
- the council uses the improved memory and functions in the next run

This is useful beyond coding. LocalGPT can host deeper technical discussions
about Windows setup, WebView2/MSIX deployment, DevExpress/Bootstrap design,
Minecraft tooling, EF/SQLite data models, local AI hosts, and system diagnostics.

## Current Capabilities

- **Local AI chat:** DXAiChat with Ollama profiles, visible thinking parsing,
  SQLite memory, resumable conversations, and optional cloud providers.
- **AI Council:** multiple selected models can discuss, correct, log, save
  memory, ask for user decisions when architecture choices are unclear, and work
  with coding agents as implementation helpers.
- **Offline engineering knowledge:** the council is fed from SQLite knowledge
  entries built from Microsoft .NET/C# compiler docs, Windows developer docs,
  DevExpress/Bootstrap guidance, EF/business-object rules, local learn-base
  projects, build logs, and setup diagnostics.
- **Downloadable generation:** LocalGPT can create safe `.cs`, `.razor`, `.dll`,
  whole .NET solution zips, AI-host control-plane zips, and Minecraft datapack
  zips through local HTTP download links.
- **Minecraft builder:** supports vanilla datapacks, Paper plugins, Fabric mods,
  and NeoForge mods. Current datapack guidance targets Minecraft Java 26.1;
  1.21.x/1.21.4 remains available for legacy comparison and starter work.
- **User-owned data:** chat memory, council knowledge, application logs, and live
  SQLite tables are inspectable and editable from the frontend.

See [docs/LOCALGPT_CAPABILITY_SNAPSHOT.md](docs/LOCALGPT_CAPABILITY_SNAPSHOT.md)
for the short capability map.

## What Is Inside

- `LocalGPTWebviewWrapper/LocalGPT`: Blazor server app, DevExpress UI, Ollama setup, DXAiChat, SQLite chat memory, AI Council, native command services, and Minecraft workspace generation.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`: WinUI 3/WebView2 host that launches the local server.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`: MSIX package project for Visual Studio deploy/debug.
- `docs`: AI-facing architecture notes, install notes, and Minecraft builder guidance.
- `AGENTS.md` and `llms.txt`: short context files for AI agents working in this repository.

## Security Model

LocalGPT is local-first, not risk-free. In the intended desktop/WebView2 mode it
keeps prompts, code, chat memory, logs, generated artifacts, and model calls on
the user's machine. That is a strong privacy advantage compared with cloud-only
coding agents.

The remaining risk is local capability risk: the app can generate code, write
local artifacts, store sensitive SQLite knowledge, and run native commands through
backend services. Do not expose the ASP.NET Core server to untrusted networks or
bind it to `0.0.0.0` unless the app is hardened as a normal web application with
auth, authorization, CSRF protection, rate limits, audit logs, command
restrictions, and workspace isolation.

Read [SECURITY.md](SECURITY.md) before hosting LocalGPT for coworkers, enabling
cloud providers, importing unreviewed knowledge, or running generated scripts.

## Quick Start

Install Visual Studio with .NET desktop, ASP.NET/web, WinUI/Windows app tooling, Windows SDK, WebView2 runtime, and DevExpress Blazor package access.

From the repository root:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -Register -Launch
```

If Windows asks to download a .NET desktop runtime through Edge, run:

```powershell
.\LocalGPTWebviewWrapper\build\Repair-LocalGptDevEnvironment.ps1 -InstallMissingRuntime -Register -Launch
```

## Ollama Setup

LocalGPT can discover and use local Ollama models. Keep Ollama running before testing DXAiChat or the AI Council.

Useful local models for council testing:

```text
gpt-oss:20b
qwen3-coder:30b
gemma3:27b
deepseek-r1:8b
```

Check the model host:

```powershell
.\LocalGPTWebviewWrapper\build\Test-OllamaGptOss.ps1 -NumPredict 1024 -TimeoutSeconds 300
```

For big council models on consumer GPUs, prefer the **Low GPU Preset** on the AI Council page after any confirmed driver reset, sustained high VRAM/GPU pressure, or model stall. A black screen alone can also be display sleep, screen saver, or power-saving behavior; treat it as a clue, not proof of GPU failure. The preset runs one small proposal pass, caps context/output, sets `keep_alive=0s`, and can force Ollama `num_gpu=0` so the test is slower but less likely to stress the GPU.

## Minecraft Builder

The Minecraft Builder now supports several directions so the user and AI Council can choose the right target:

- Fabric mod: lightweight Java Edition mod iteration.
- NeoForge mod: modern Forge-style Java modding.
- Paper plugin: server-side Java plugins without a modded client.
- Vanilla datapack: command/function/data behavior without Java or NeoForge.
- Bedrock add-on: planned as a separate behavior/resource pack exporter.

Install or verify the Java modding toolchain:

```powershell
.\LocalGPTWebviewWrapper\build\Setup-MinecraftModToolchain.ps1 -Install -InstallGradle -InstallEclipse
```

Generated Java workspaces include `build-local.ps1` for Gradle builds. Generated datapacks include `build-local.ps1` for JSON validation and zip packaging.

The current Minecraft Java datapack benchmark can be regenerated without loading
Ollama:

```powershell
$server = Get-Content "$env:LOCALAPPDATA\LocalGPT\runtime\server.json" | ConvertFrom-Json
Invoke-RestMethod "$($server.BaseUrl)/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1"
```

For legacy comparison only, use:

```powershell
Invoke-RestMethod "$($server.BaseUrl)/__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4"
```

That route validates the datapack, creates a zip, and stores a compact council knowledge entry so later AI Council reviews can use database memory instead of a huge pasted prompt. The 1.21.4 route remains useful for legacy comparison only.

AI guidance for this feature lives in [docs/MINECRAFT_MOD_AI_BUILDER.md](docs/MINECRAFT_MOD_AI_BUILDER.md).

## Diagnostics

Use LocalGPT diagnostics before direct Ollama calls:

- `POST /__diag/dxaichat-smoke`: configured DXAiChat backend smoke test with visible/thinking split and optional SQLite memory save.
- `POST /__diag/council`: multi-model council run through LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`: generated workspace smoke test.
- `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=26.1`: focused current-Java datapack generation, validation, zip packaging, and council knowledge capture. Use `1.21.4` only for legacy comparison.
- `GET /__diag/logs?minimumLevel=Warning&take=30`: recent SQLite application logs and the AI briefing built from them. Add `writeSmoke=true` to write a harmless warning and verify the async database logger.
- `GET /__diag/knowledge`: editable council knowledge notes saved from council runs and manual user edits.
- `GET /__diag/sqlite/tables`: live SQLite table inventory for chat memory, thoughts, logs, and council knowledge.
- `GET /__diag/council/artifact-smoke?target=solution`: deterministic whole-solution artifact smoke test that emits a downloadable .NET 10 Blazor/DevExpress zip.
- `GET /__diag/council/artifact-smoke?target=ollama`: deterministic Ollama-inspired .NET/DevExpress control-plane lab zip. It includes Ollama-style route stubs, model catalog UI, model download planning, settings, and explicit native GGML/GPU non-implementation notes.
- `GET /__diag/council/artifact-smoke?target=datapack`: deterministic prompt-driven Minecraft datapack zip. Living Cities remains a separate named benchmark route, not the hidden default for all datapacks.
- `GET /__diag/learn-base/import`: import compact architecture fingerprints and documentation source maps from `C:\tmpselectedcodexlearnbaseforlocalgpt` into the council knowledge database.
- `GET /__diag/benchmark/engineering`: run the five-task personal benchmark for DevExpress/EF, CRUD dashboard, packaging diagnosis, datapack generation, and loader skeleton distinction.

## Helping The Council Learn

LocalGPT improves fastest when missing knowledge is treated as a repairable
system issue, not as a model failure.

Use these loops:

- **Import source or docs:** place source trees or documentation under
  `C:\tmpselectedcodexlearnbaseforlocalgpt`, then run `/__diag/learn-base/import`.
  Known corpora such as Windows developer docs, Microsoft .NET docs,
  DevExpress samples, and local project examples become compact SQLite knowledge
  entries instead of huge prompts.
- **Approve good knowledge:** open **SQLite Database** and review council
  knowledge rows. Source-backed or user-approved entries should outrank
  model-suggested notes.
- **Ask for a capability-gap report:** when the council lacks a function, source,
  version map, or domain detail, ask it to include a structured
  `<localgpt-capability-gap>` block. LocalGPT stores that as a fix list for users,
  agents, and future council runs.
- **Use agents as maintainers:** Codex or another coding agent can read those
  gaps, add routes/functions/docs, run tests, commit, publish, and push. The AI
  Council then sees the new capabilities through bootstrap memory and DXAiFunctions.
- **Keep evidence small:** prefer `/__diag/...` routes, SQLite rows, build logs,
  and upload workspaces over pasting whole repositories into chat.

The AI Council stores transcripts in SQLite chat memory and also writes a reusable entry into the editable council knowledge database.
In the Council page, choose an older council memory to continue the thread, or start a new thread.
Each run and step records the full council member list; faulty or unavailable members can be excluded from the next round by the user,
while models must propose that through a poll instead of removing peers on their own.
Use **Feature Request Chat** for implementation ideas; it enables a CodeDOM-generated C# example file and exposes it through a download link in the council result.

Open **SQLite Database** in the navigation to edit council knowledge with DevExpress controls and inspect live SQLite tables. The generic editor protects primary-key columns in the form, but it still edits the live local database, so use it as an administrative tool.
Native Minecraft builder commands are restricted to the LocalGPT Minecraft workspace, checked against an executable policy, and written to the `NativeCommandLogs` table with stdout/stderr artifact paths.
The ledger records a `CommandProfile` such as `GradleBuildOnly`, `GradleRunClient`, `JavaVersionOnly`, or `PowerShellWorkspaceScript` so diagnostics can distinguish build, run, setup, and script paths.

For DevExpress-related feature requests, use `GET /__diag/devexpress` to inspect referenced package versions, imported namespaces, registered services, and loaded assemblies. DevExpress Office/report/PDF generation should be implemented in backend services with safe download links, while the Blazor frontend handles controls, status, and navigation.

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json`.

For desktop shell validation, run the WinUI wrapper from Visual Studio or a registered package with `LOCALGPT_WEBVIEW2_SMOKE=1`, or create `%LOCALAPPDATA%\LocalGPT\runtime\webview2-smoke.flag` containing `exit` before launching the registered app. This is the preferred frontend fallback for LocalGPT usability tests because it exercises the real WebView2 wrapper. It writes route snapshots for Chat, AI Council, SQLite Database, and Minecraft Builder to `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`.

At startup, LocalGPT writes the active loopback endpoint to `%LOCALAPPDATA%\LocalGPT\runtime\server.json`. Set `LOCALGPT_STARTUP_TRACE=1` for opt-in console startup phase traces when diagnosing packaged or published launches.

If package registration/deploy reports `0x80070002` or `DEP1000` for a loose AppX layout, rebuild the package and re-run `Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register`. The package project copies AppX image assets into the loose `AppX\Images` layout, and the repair script retries once after removing a stale LocalGPT development registration.

## Build

Check physical source formatting before reviewing or committing:

```powershell
.\build\Assert-SourceFormatting.ps1
```

This guard fails if tracked human-maintained source, docs, scripts, project, or workflow files collapse into giant physical lines.
It covers `.cs`, `.razor`, `.md`, `.ps1`, `.json`, `.yml`, `.yaml`, `.csproj`, and `.wapproj` files while excluding build output folders.
The same check runs in GitHub Actions through `.github/workflows/source-hygiene.yml`.

Build the Blazor project:

```powershell
dotnet build .\LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj -c Debug -p:Platform=x64
```

Build the WinUI/MSIX package with Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  ".\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper (Package)\LocalGPTWebviewWrapper (Package).wapproj" `
  /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /v:minimal
```

The package project keeps `IncludeLocalGptPublishedPayload` defaulted to `false` so Visual Studio Debug/F5 does not double-include the Blazor payload.
Release and publish scripts intentionally opt in with `IncludeLocalGptPublishedPayload=true` after publishing the LocalGPT web project.
Do not make that opt-in unconditional; duplicate entries for `LocalGPT.deps.json`, `LocalGPT.staticwebassets.endpoints.json`,
or published `wwwroot` files can reintroduce APPX1111 package-map failures.

## Release Packages

Create zip packages for multiple architectures:

```powershell
.\LocalGPTWebviewWrapper\build\Publish-LocalGptRelease.ps1 `
  -Version "0.1.0-ai-council.20260602" `
  -Configuration Release `
  -Platforms x64,x86,arm64 `
  -BackendRuntimeIdentifiers win-x64,linux-x64,osx-x64,osx-arm64
```

The script writes zips and a SHA256 manifest under `artifacts\releases\`. Windows wrapper packages are WebView2/MSIX-only, while Linux and macOS use the backend-only ASP.NET Core/Blazor zips. Use `-SkipWrapper` or `-SkipBackend` when rebuilding only one side. Add `-CreateGitHubRelease` when `gh` is installed and authenticated.

## Developer Notes

Start with [LocalGPTWebviewWrapper/readme.md](LocalGPTWebviewWrapper/readme.md) for detailed setup and repair steps. Use [docs/ARCHITECTURE_FOR_AI.md](docs/ARCHITECTURE_FOR_AI.md) for system architecture and [AGENTS.md](AGENTS.md) for agent-specific working rules.

Keep claims grounded: do not say a generated mod, plugin, or datapack was built or launched unless the command output exists. When a feature is missing, write a clear missing-feature report so it can be saved to LocalGPT memory and picked up by the AI Council later.

## Personal Engineering Benchmark

Use the benchmark route to compare raw Ollama, LocalGPT with DX functions/memory, cloud assistants, and a manual expected-output lane across five repeatable tasks.
The deterministic LocalGPT lane can run without loading Ollama; raw Ollama and cloud lanes stay marked `NotRun` until real transcripts are supplied.

```powershell
$server = Get-Content "$env:LOCALAPPDATA\LocalGPT\runtime\server.json" | ConvertFrom-Json
Invoke-RestMethod "$($server.BaseUrl)/__diag/learn-base/import?maxProjects=40&saveToKnowledge=true"
Invoke-RestMethod "$($server.BaseUrl)/__diag/benchmark/engineering?importLearnBaseFirst=false&saveToKnowledge=true"
```

The learn-base importer records architecture fingerprints: host shapes, protocols, libraries, solution topology, DevExpress Web API/security, Python.NET interop, bot/microservice patterns, and Blazor/non-Blazor hosting.
It deliberately does not teach project names as important facts.
It reads source/documentation-like files, skips noisy build/cache folders, counts but does not store binaries/installers/certificates/PDFs, and uses stable knowledge-entry IDs so re-importing the same source updates the same SQLite rows instead of creating duplicate knowledge.
Known documentation corpora get special source-map entries, including Microsoft .NET docs, C# language/compiler diagnostics, C# 12-era language guidance, modern .NET architecture, Windows developer docs, WebView2, MSIX, design, and accessibility.
