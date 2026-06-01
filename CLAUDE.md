# CLAUDE.md

## Repository guidance for Claude or similar coding assistants

This repository is not a generic Blazor sample.

LocalGPT is a .NET 10 Blazor/ASP.NET Core app hosted inside a WinUI 3 WebView2 wrapper, with DevExpress components, Ollama AI configuration, native command services, and Windows MSIX/DesktopBridge packaging.

## Core rules

- Preserve the WebView2-hosted Blazor architecture unless a redesign is explicitly requested.
- Keep the WinUI wrapper thin.
- Keep AI model/profile configuration durable and backward-compatible.
- Preserve DevExpress Blazor usage and static asset loading.
- Use backend service boundaries for native OS commands.
- Avoid broad framework or package churn unless the build/deploy pipeline is verified afterward.
- Use Visual Studio MSBuild for full solution/package validation.
- Use the wrapper's WebView2 smoke diagnostics as the frontend fallback for LocalGPT UI tests. An assistant's built-in browser is not proof that the WinUI/WebView2 packaged shell works.

## Important projects

- `LocalGPTWebviewWrapper/LocalGPT`: Blazor/ASP.NET Core app, AI services, DevExpress UI.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`: WinUI 3 WebView2 host.
- `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`: MSIX package/deploy project.
- `LocalGPTWebviewWrapper/build`: local build and repair scripts.

## Practical editing guidance

Prefer:

- small targeted changes
- explicit configuration migration/fallback behavior
- clear logging for setup/save/load failures
- service-layer changes for command execution and AI features
- package verification after `.wapproj` edits

Avoid:

- moving server logic into WinUI code
- invoking native commands directly from components
- deleting apparently odd package targets without testing deployment
- replacing DevExpress UI with another component stack
- assuming `dotnet build` alone validates the package project

Loose AppX deploy notes:

- image assets must be present under `bin/<platform>/<configuration>/AppX/Images`
- `0x80070002`, `0x80073CF9`, or `DEP1000` can mean missing manifest images or a stale LocalGPT package registration
- use `LocalGPTWebviewWrapper/build/Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register`; it removes only the stale LocalGPT development package identity and retries once

## Static asset mental model

Blazor and DevExpress assets depend on `LocalGPT.staticwebassets.runtime.json`.

If DevExpress scripts or themes 404 in the packaged app, first verify that this file exists beside the packaged executable in:

```text
LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)/bin/x64/Debug/LocalGPTWebviewWrapper/
```

DevExpress 25 serves the main module from:

```text
/_content/DevExpress.Blazor/modules/dx-blazor-all.js
```

## Documentation to improve first

If asked to improve documentation, prioritize:

- setup/deploy/debug steps
- configuration save/load behavior
- AI profile and Ollama context model
- package/static asset troubleshooting
- Minecraft mod workspace and command execution boundaries
- WebView2 smoke fallback checks for `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`
