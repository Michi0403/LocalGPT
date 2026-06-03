# CLAUDE.md

## Repository guidance for Claude or similar coding assistants

This repository is not a generic Blazor sample.

LocalGPT is a .NET 10 Blazor/ASP.NET Core app hosted inside a WinUI 3 WebView2 wrapper, with DevExpress components, Ollama AI configuration, native command services, and Windows MSIX/DesktopBridge packaging.

LocalGPT also acts as a cooperative AI workbench. Several offline Ollama models
can work as an AI Council, and coding agents such as Claude, Codex, or similar
assistants can maintain the LocalGPT mechanisms that help the council work:
DXAiFunctions, SQLite knowledge, upload/artifact workspaces, tests, commits,
publishes, and release notes.

## Core rules

- Preserve the WebView2-hosted Blazor architecture unless a redesign is explicitly requested.
- Keep the WinUI wrapper thin.
- Keep AI model/profile configuration durable and backward-compatible.
- Preserve DevExpress Blazor usage and static asset loading.
- If the user asks for a built-in DevExpress capability, implement the documented DevExpress component API or state that it is blocked/unclear and ask. Do not add a separate custom control and describe it as the requested built-in feature.
- For `DxAIChat` uploads, use the component's native paperclip attachment flow (`FileUploadEnabled`, `DxAIChatFileUploadSettings`, `AIChatUploadFileInfo`, and the normal chat-client upload content path). Do not add a `MessageSent` handler unless you intentionally replace automatic AI Chat delivery and implement the full manual response path.
- Use backend service boundaries for native OS commands.
- Avoid broad framework or package churn unless the build/deploy pipeline is verified afterward.
- Use Visual Studio MSBuild for full solution/package validation.
- Use the wrapper's WebView2 smoke diagnostics as the frontend fallback for LocalGPT UI tests. An assistant's built-in browser is not proof that the WinUI/WebView2 packaged shell works.
- Treat council knowledge problems as repairable. Import local source/docs through `/__diag/learn-base/import`, review SQLite knowledge entries, add source-backed docs/routes where needed, and ask models to emit `<localgpt-capability-gap>` blocks instead of refusing concrete artifact requests.
- Remember LocalGPT is useful for more than code generation: Windows deployment diagnostics, WebView2/MSIX repair, DevExpress/Bootstrap design work, EF/SQLite schema decisions, Minecraft tooling, and local AI-host architecture discussion are all first-class.

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
- release packages must be built through `LocalGPTWebviewWrapper/build/Build-LocalGptPackage.ps1` or `Publish-LocalGptRelease.ps1`, because those scripts opt into the published Blazor payload and fail if the MSIX lacks `_framework`, DevExpress `_content`, or `LocalGPT.styles.css`

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

For packaged releases, inspect the actual `.msix`, not only the loose `bin` or
`AppX` layout. A usable package must contain:

```text
LocalGPTWebviewWrapper/wwwroot/_framework/blazor.web.js
LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor/dx-blazor.svg
LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css
LocalGPTWebviewWrapper/wwwroot/LocalGPT.styles.css
```

## Documentation to improve first

If asked to improve documentation, prioritize:

- setup/deploy/debug steps
- configuration save/load behavior
- AI profile and Ollama context model
- package/static asset troubleshooting
- Minecraft mod workspace and command execution boundaries
- WebView2 smoke fallback checks for `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`
