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
  - Can continue an older saved council conversation by sending `ContinueConversationId` or selecting the saved council memory in the frontend.
  - Every result step records the full council roster. Faulty members can be excluded from the next round by user action; models should propose exclusion only through a poll.
  - `GenerateImplementationArtifact` creates a CodeDOM C# starter file under `%LOCALAPPDATA%\LocalGPT\CouncilArtifacts\` and returns a safe `/__artifacts/council/{fileName}` download link.
  - Generated implementation ideas must stay as sandbox artifacts or temporary workspaces until the user explicitly permits integration. The council must never overrule a user decision that denies or limits self-expansion.
  - Use `MaxParallelModels = 1` for 20B/30B local models on 24 GB VRAM unless the user asks for heavier runs.
- `GET /__diag/council/models`
  - Lists configured and installed Ollama models visible to LocalGPT.
- `GET /__diag/minecraft/workspace-smoke?loader=datapack|paper|fabric|neoforge`
  - Generates buildable smoke workspaces through the app service.
- `GET /__diag/logs?minimumLevel=Warning&take=30`
  - Reads recent warnings/errors from the SQLite application log.
  - Returns the same short AI briefing that bootstrap context gives to DXAiChat and the AI Council.
  - Add `writeSmoke=true` to write and flush a harmless warning entry after logger changes.
- `GET /__diag/devexpress`
  - Reads DevExpress package references, Blazor imports, service registrations, and loaded assemblies when available.
  - Use this before asking the council for DevExpress Office/report/PDF/RichEdit/PdfViewer/Pivot features.
  - Office/report/file generation should be planned as ASP.NET Core backend services with safe download endpoints.
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
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net9.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

It drives the embedded WebView2 through `/`, `/Chat`, and `/minecraft-mod-builder`, then writes JSON snapshots to:

```text
%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\
```

Use these snapshots to verify that the real desktop wrapper loads the Blazor app, the DXAiChat page, and the Minecraft Builder page.

## Known Gotchas

- Stop running `LocalGPT.exe` or wrapper instances before rebuilding; they can lock `bin` and `obj` outputs.
- The package/deploy path must be tested with Visual Studio MSBuild because `.wapproj` is not a normal SDK-only project.
- LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Discover the current app URL from `%LOCALAPPDATA%\LocalGPT\runtime\server.json` instead of assuming a fixed port.
- Application warnings/errors are stored in SQLite table `ApplicationLogs` when `LoggingCore:DatabaseCore:CoreLogLevel` allows them. The database logger is queued/background-flushed and excludes EF categories to avoid recursive logging.
- Missing-feature reports under `%LOCALAPPDATA%\LocalGPT\AIReports\` now include helpful source requests. AI participants should ask for official docs, examples, specs, package references, or sample repositories when needed, without pretending those sources were verified.
- Do not treat raw model output as verified facts. The council should mark uncertain claims as `Needs verification`.
- Some models, especially reasoning models, may return thinking-only output when the token budget is too small. LocalGPT now separates thinking from visible text and should surface a clear placeholder if no final answer appears.

## Next Useful Checks

- Rerun `POST /__diag/dxaichat-smoke` after restarting LocalGPT from a fresh build.
- Rerun a short AI Council feedback prompt with `gpt-oss:20b` plus one other model after the formatter fix is loaded.
- Check `/__diag/logs?minimumLevel=Warning&take=30` before asking the council for setup advice; recent Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, or package errors should be treated as actionable health signals.
- Run the WebView2 smoke mode from a registered/package identity or Visual Studio debug launch and inspect `%LOCALAPPDATA%\LocalGPT\WebView2Diagnostics\`.
- Commit and push diagnostic changes in small slices.
