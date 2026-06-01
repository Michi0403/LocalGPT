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

AI guidance for this feature lives in [docs/MINECRAFT_MOD_AI_BUILDER.md](docs/MINECRAFT_MOD_AI_BUILDER.md).

## Diagnostics

Use LocalGPT diagnostics before direct Ollama calls:

- `POST /__diag/dxaichat-smoke`: configured DXAiChat backend smoke test with visible/thinking split and optional SQLite memory save.
- `POST /__diag/council`: multi-model council run through LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`: generated workspace smoke test.

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json`.

For desktop shell validation, run the WinUI wrapper from Visual Studio or a registered package with `LOCALGPT_WEBVIEW2_SMOKE=1`. It writes WebView2 snapshots to `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`.

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
.\LocalGPTWebviewWrapper\build\Publish-LocalGptRelease.ps1 -Configuration Release -Platforms x64,x86,arm64
```

The script writes zips and a SHA256 manifest under `artifacts\releases\`. Add `-CreateGitHubRelease` when `gh` is installed and authenticated.

## Developer Notes

Start with [LocalGPTWebviewWrapper/readme.md](LocalGPTWebviewWrapper/readme.md) for detailed setup and repair steps. Use [docs/ARCHITECTURE_FOR_AI.md](docs/ARCHITECTURE_FOR_AI.md) for system architecture and [AGENTS.md](AGENTS.md) for agent-specific working rules.

Keep claims grounded: do not say a generated mod, plugin, or datapack was built or launched unless the command output exists. When a feature is missing, write a clear missing-feature report so it can be saved to LocalGPT memory and picked up by the AI Council later.
