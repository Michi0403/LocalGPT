# LocalGPT

LocalGPT is a Windows desktop hosted Blazor and ASP.NET Core app wrapped by WinUI 3 and WebView2. The goal is to keep the AI workflow local-first with Ollama, make DevExpress/Blazor debugging easier, and expose careful backend services for native tasks such as building Minecraft workspaces.

## What Is Inside

- `LocalGPTWebviewWrapper/LocalGPT`: Blazor server app, DevExpress UI, Ollama setup, DXAiChat, SQLite chat memory, AI Council, native command services, and Minecraft workspace generation.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`: WinUI 3/WebView2 host that launches the local server.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`: MSIX package project for Visual Studio deploy/debug.
- `docs`: AI-facing architecture notes, install notes, and Minecraft builder guidance.
- `AGENTS.md` and `llms.txt`: short context files for AI agents working in this repository.

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

The Living Cities datapack benchmark can be regenerated without loading Ollama:

```powershell
$server = Get-Content "$env:LOCALAPPDATA\LocalGPT\runtime\server.json" | ConvertFrom-Json
Invoke-RestMethod "$($server.BaseUrl)/__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4"
```

That route validates the datapack, creates a zip, and stores a compact council knowledge entry so later AI Council reviews can use database memory instead of a huge pasted prompt.

AI guidance for this feature lives in [docs/MINECRAFT_MOD_AI_BUILDER.md](docs/MINECRAFT_MOD_AI_BUILDER.md).

## Diagnostics

Use LocalGPT diagnostics before direct Ollama calls:

- `POST /__diag/dxaichat-smoke`: configured DXAiChat backend smoke test with visible/thinking split and optional SQLite memory save.
- `POST /__diag/council`: multi-model council run through LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`: generated workspace smoke test.
- `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4`: focused Living Cities datapack generation, validation, zip packaging, and council knowledge capture.
- `GET /__diag/logs?minimumLevel=Warning&take=30`: recent SQLite application logs and the AI briefing built from them. Add `writeSmoke=true` to write a harmless warning and verify the async database logger.
- `GET /__diag/knowledge`: editable council knowledge notes saved from council runs and manual user edits.
- `GET /__diag/sqlite/tables`: live SQLite table inventory for chat memory, thoughts, logs, and council knowledge.
- `GET /__diag/council/artifact-smoke?target=solution`: deterministic whole-solution artifact smoke test that emits a downloadable .NET 10 Blazor/DevExpress zip.
- `GET /__diag/council/artifact-smoke?target=ollama`: deterministic Ollama-inspired .NET/DevExpress control-plane lab zip. It includes selected Ollama-style API stubs and explicitly does not implement native GGML/GPU inference.

The AI Council stores transcripts in SQLite chat memory and also writes a reusable entry into the editable council knowledge database.
In the Council page, choose an older council memory to continue the thread, or start a new thread.
Each run and step records the full council member list; faulty or unavailable members can be excluded from the next round by the user,
while models must propose that through a poll instead of removing peers on their own.
Use **Feature Request Chat** for implementation ideas; it enables a CodeDOM-generated C# example file and exposes it through a download link in the council result.

Open **SQLite Database** in the navigation to edit council knowledge with DevExpress controls and inspect live SQLite tables. The generic editor protects primary-key columns in the form, but it still edits the live local database, so use it as an administrative tool.
Native Minecraft builder commands are restricted to the LocalGPT Minecraft workspace, checked against an executable policy, and written to the `NativeCommandLogs` table with stdout/stderr artifact paths.

For DevExpress-related feature requests, use `GET /__diag/devexpress` to inspect referenced package versions, imported namespaces, registered services, and loaded assemblies. DevExpress Office/report/PDF generation should be implemented in backend services with safe download links, while the Blazor frontend handles controls, status, and navigation.

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json`.

For desktop shell validation, run the WinUI wrapper from Visual Studio or a registered package with `LOCALGPT_WEBVIEW2_SMOKE=1`, or create `%LOCALAPPDATA%\LocalGPT\runtime\webview2-smoke.flag` containing `exit` before launching the registered app. This is the preferred frontend fallback for LocalGPT usability tests because it exercises the real WebView2 wrapper. It writes route snapshots for Chat, AI Council, SQLite Database, and Minecraft Builder to `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`.

If package registration/deploy reports `0x80070002` or `DEP1000` for a loose AppX layout, rebuild the package and re-run `Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register`. The package project copies AppX image assets into the loose `AppX\Images` layout, and the repair script retries once after removing a stale LocalGPT development registration.

## Build

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
