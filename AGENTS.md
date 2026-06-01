# AGENTS.md

## Purpose

This repository contains LocalGPT, a Windows desktop-hosted Blazor/ASP.NET Core application for local AI workflows. It combines:

- Blazor Server interactive UI
- DevExpress Blazor components
- Ollama-hosted AI model configuration
- context-aware chat/client services
- backend native command execution services
- a WinUI 3/WebView2 desktop wrapper
- MSIX/DesktopBridge packaging for Windows deploy/debug

This file is intended for AI coding agents and contributors so they can work with the repository without fighting the hosting model.

## High-level structure

### `LocalGPTWebviewWrapper/LocalGPT`

Main ASP.NET Core and Blazor application. This project contains:

- Blazor UI and pages
- DevExpress component usage
- AI model configuration and chat services
- Ollama connectivity checks
- configuration save/load services
- Minecraft mod workspace helpers
- native command execution abstraction

### `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`

WinUI 3 desktop wrapper. It launches the local ASP.NET Core server and hosts it in WebView2.

### `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`

Windows package project for deploy/debug. This project must preserve the self-contained wrapper output and the Blazor static web assets manifest in the AppX layout.

### `LocalGPTWebviewWrapper/build`

Developer repair and build scripts. Keep these scripts sanitized, small, and suitable for GitHub.

## Architectural intent

Treat the system as a local desktop shell around a real ASP.NET Core application.

The WinUI layer should remain thin:

- start/host the server
- display the UI
- handle desktop integration

The Blazor/server layer should own:

- AI setup and model selection
- Ollama connectivity
- context reuse
- Minecraft mod generation workflows
- native command orchestration
- configuration persistence

## DevExpress guidance

The UI intentionally uses DevExpress Blazor.

Important rules:

- do not replace DevExpress with another UI stack unless explicitly requested
- preserve DevExpress package references and static asset loading
- check generated static web asset manifests when DevExpress JavaScript or CSS files 404
- DevExpress 25 module assets are under `/_content/DevExpress.Blazor/modules/`

## Packaging guidance

The package project is not a normal SDK-style project. Use Visual Studio MSBuild, not only `dotnet build`, for full package/debug verification.

Important package behavior:

- `LocalGPTWebviewWrapper` publishes self-contained when a runtime identifier is present
- the package project overlays that publish output into the AppX layout
- `LocalGPT.staticwebassets.runtime.json` must exist beside the packaged executable
- missing static web assets cause DevExpress module errors and blank/broken UI

## Configuration expectations

Configuration is user-facing through the setup page and should be durable.

Rules for changes:

- do not silently rename configuration sections
- preserve existing AI profile data where possible
- prefer additive migrations over destructive rewrites
- handle missing optional sections with non-null defaults
- make save/load failures visible through logs or UI messages

## AI and Ollama guidance

The app should be able to save and select multiple Ollama AI profiles and reuse context intelligently.

When changing AI behavior:

- keep model/provider selection explicit
- avoid global hidden state for active models
- persist user choices through the configuration writer
- separate connectivity probing from chat execution
- keep context reuse bounded and explainable

## Minecraft mod building direction

The target workflow is complex Java Minecraft mod creation on command.

Agent guidance:

- keep filesystem and OS command execution in backend services
- use `INativeCommandRunner` or a similar service boundary for native commands
- keep frontend JavaScript for client-only helpers, not privileged execution
- design generated mod workspaces so they can be inspected and rebuilt
- prefer explicit project templates and build logs over opaque generation

## How AI should modify code here

When implementing or editing features:

1. Preserve the WebView2-hosted Blazor architecture.
2. Keep the WinUI wrapper thin.
3. Prefer service boundaries over UI-driven command execution.
4. Preserve DevExpress compatibility.
5. Keep configuration backward-compatible.
6. Build the full solution with Visual Studio MSBuild when packaging is touched.
7. Verify static assets when DevExpress UI behavior changes.

## Good contribution patterns

Good changes include:

- improving setup/config save and load reliability
- documenting packaging and runtime fixes
- tightening null handling around options
- making AI profile selection explicit
- improving command execution safety and logs
- adding focused repair scripts

Risky changes include:

- moving server responsibilities into the WinUI wrapper
- introducing unbounded command execution from UI code
- deleting package targets that copy publish/static asset output
- replacing DevExpress components casually
- broad retargeting across .NET versions without a full package verification

## Recommended first-read areas for an AI agent

Start here:

- `LocalGPT/Program.cs`
- configuration business objects
- configuration writer service
- chat client factory and composite chat client
- setup/install pages
- `INativeCommandRunner`
- package `.wapproj`
- `build/Repair-LocalGptDevEnvironment.ps1`

If a behavior seems unusual, assume it may be related to:

- WebView2 desktop hosting
- DevExpress static assets
- Windows App SDK packaging
- local AI model configuration
- future Minecraft mod build automation
