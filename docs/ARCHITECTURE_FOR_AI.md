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

## Minecraft mod generation model

The intended direction is to let LocalGPT create complex Java Minecraft mods on command.

Recommended architecture:

- keep generation state in a workspace service
- keep native commands behind `INativeCommandRunner`
- store logs and generated files where users can inspect them
- make build steps repeatable through scripts or service methods
- keep frontend JavaScript limited to client-side helper behavior

Feature wishlist gathered from the local `gpt-oss:20b` debug model:

- mod template library for Fabric, Forge, and Quilt
- dependency resolver for Fabric API and related libraries
- version sync between metadata, Gradle, generated code, and assets
- generated README/changelog/API docs
- JUnit test generation for common block/item/command behavior
- static safety analysis for risky reflection, network, filesystem, or command patterns
- sandboxed run/build workflow before deploying generated mods

Treat these as product direction, not as already-implemented behavior.

## If you are changing code

Ask yourself:

- Does this preserve the Blazor/server ownership of app behavior?
- Does this keep the WinUI wrapper thin?
- Does this preserve DevExpress static asset loading?
- Does this keep configuration compatible with existing user data?
- Does this route native execution through backend services?
- Does the full package still build and deploy?

If not, rethink the change before editing.
