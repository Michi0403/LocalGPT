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

- `LocalGPTWebviewWrapper` publishes self-contained for RID builds
- the package project overlays the self-contained publish output into AppX
- Windows App SDK debug framework references are avoided in the manifest
- `LocalGPT.staticwebassets.runtime.json` is copied beside the packaged executable

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
- the full transcript, visible model reasoning notes, errors, and timings are written to a Markdown log under `%LOCALAPPDATA%\LocalGPT\CouncilLogs`
- the transcript is also saved into the existing SQLite chat memory as an `AI Council - model + model` conversation

Performance rule: default council scheduling runs one model inference at a time (`MaxParallelModels = 1`), caps Ollama context (`MaxContextTokens = 8192` by default), applies a per-model timeout, and uses a short Ollama keep-alive when several large local models are selected. This avoids trying to keep multiple 20B/30B models resident in VRAM at once on machines like a 7900 XTX with 24 GB VRAM. Users can raise the parallelism or context in the UI when they know the loaded models fit together.

Decision rule: if a participant is unavailable, the council cannot converge, or the final answer still needs human verification, the result should include a user decision poll. The poll is saved into memory and shown in the UI so the next council round can treat the user's choice as binding shared context.

Frustration rule: if the user's prompt sounds angry, blocked, or frustrated, the council must stay kind, avoid blame, and turn the emotion into a technical recovery poll. Poll options should cover stabilization, missing-feature implementation, and scope reduction. The selected path and any missing LocalGPT feature request should be saved into SQLite chat memory so later models can see it.

Design rule: the council can display provider-supplied visible thinking and model-written reasoning notes, but user-facing controls should make it clear which model produced each note. Treat council output as reviewed assistance, not as automatically true.

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

LocalGPT intentionally chooses a free loopback port at startup to avoid binding issues. Diagnostics should discover the current URL from:

```text
%LOCALAPPDATA%\LocalGPT\runtime\server.json
```

The WinUI wrapper supports a WebView2 smoke mode. Prefer a registered/package identity or Visual Studio debug launch for this final frontend check, because direct unpackaged exe launch can fail with WinUI activation error `REGDB_E_CLASSNOTREG`:

```powershell
$runtime = "$env:LOCALAPPDATA\LocalGPT\runtime"
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Set-Content -Path "$runtime\webview2-smoke.flag" -Value "exit" -Encoding utf8
```

The flag enables smoke mode once for registered/package launches.

```powershell
$env:LOCALGPT_WEBVIEW2_SMOKE = "1"
$env:LOCALGPT_WEBVIEW2_SMOKE_EXIT = "1"
.\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\bin\x64\Debug\net9.0-windows10.0.22621.0\win-x64\LocalGPTWebviewWrapper.exe
```

This drives the embedded WebView2 through `/`, `/Chat`, and `/minecraft-mod-builder` and writes JSON snapshots to:

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
