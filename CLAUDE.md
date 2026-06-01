# CLAUDE.md

## Repository guidance for Claude or similar coding assistants

This repository is not a generic Blazor sample.

LocalGPT is a .NET 9 Blazor/ASP.NET Core app hosted inside a WinUI 3 WebView2 wrapper, with DevExpress components, Ollama AI configuration, native command services, and Windows MSIX/DesktopBridge packaging.

## Core rules

- Preserve the WebView2-hosted Blazor architecture unless a redesign is explicitly requested.
- Keep the WinUI wrapper thin.
- Keep AI model/profile configuration durable and backward-compatible.
- Preserve DevExpress Blazor usage and static asset loading.
- Use backend service boundaries for native OS commands.
- Avoid broad framework or package churn unless the build/deploy pipeline is verified afterward.
- Use Visual Studio MSBuild for full solution/package validation.

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
