# GitHub Copilot Instructions for LocalGPT

## What this repository is

LocalGPT is a .NET 9 Blazor/ASP.NET Core application hosted inside a WinUI 3 WebView2 desktop wrapper.

It is designed as a local AI workstation with:

- DevExpress Blazor UI
- Ollama-hosted AI model selection
- setup/configuration persistence
- context-aware chat services
- backend native command execution
- future Minecraft Java mod generation workflows
- Windows MSIX/DesktopBridge packaging

## Coding expectations

When suggesting code:

- preserve the WebView2-hosted Blazor architecture
- keep the WinUI wrapper thin
- preserve DevExpress component usage
- preserve existing configuration section names where possible
- use backend services for privileged/native command execution
- prefer targeted fixes over broad refactors
- verify package behavior when touching `.wapproj` or runtime layout

## DevExpress expectations

The app intentionally uses DevExpress Blazor components.

Do not suggest replacing DevExpress with another UI library unless explicitly asked.

If DevExpress scripts or CSS fail in the packaged app, check `LocalGPT.staticwebassets.runtime.json` and the package output layout before changing component code.

## AI/Ollama expectations

The app should support multiple selectable Ollama profiles and smart context reuse.

Suggestions should:

- keep provider/model selection explicit
- keep configuration save/load durable
- separate connectivity checks from chat execution
- avoid hidden global model state

## Minecraft mod builder expectations

Minecraft Java mod generation will need project templates, filesystem writes, builds, and command execution.

Suggestions should keep those operations behind backend services and produce inspectable workspaces, logs, and recovery steps.

## Style expectations for suggestions

Prefer:

- explicit naming
- async-safe service code
- logging around failure paths
- comments for unusual package/static asset behavior
- small compatibility-preserving changes

Avoid:

- UI-driven native command execution
- deleting package targets without testing deployment
- assuming `dotnet build` alone validates MSIX packaging
- unnecessary abstraction or cosmetic churn
