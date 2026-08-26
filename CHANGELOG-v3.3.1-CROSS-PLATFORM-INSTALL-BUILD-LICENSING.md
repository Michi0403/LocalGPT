# LocalGPT 3.3.1 — Cross-platform install, build and DevExpress licensing repair

## Why this release exists

LocalGPT 3.3.0 already targets Windows, Linux and macOS for the main web application, but a clean source checkout on macOS exposed two repository-level build blockers before normal compilation could begin: the Windows WinUI/WebView wrapper rejected non-Windows targeting, and the repository NuGet configuration required a local `./packages` source that is intentionally created only by package/release workflows.

The first-run/install experience also already contained provider bootstrap services for Ollama and LM Studio/llmster, but those capabilities were too easy to miss from the normal provider setup surface. 3.3.1 wires the existing service-backed setup assistant into the Install workflow more visibly and keeps platform-specific Ollama executable discovery behind a DI interface.

## Cross-platform source build entry points

- `LocalGPTWebviewWrapper.csproj` now sets `EnableWindowsTargeting=true`. The wrapper remains a Windows WinUI application, but restore/build of the Windows target is permitted from non-Windows developer machines.
- The normal `NuGet.Config` uses NuGet.org and no longer declares an unconditional repository-local `./packages` source. Build/release scripts continue to inject the locally packed wire-protocol package explicitly when package mode is requested.
- Maintained PowerShell build/validation scripts use platform-neutral `Join-Path` child paths instead of Windows-only backslash-delimited child path literals.
- `Build-LocalDevelopment.ps1` and `Build-Release.ps1` now perform an explicit `dotnet` availability check and DevExpress license preflight before the ordered build workflow.
- Documentation browser discovery now recognizes common macOS application bundles and common Linux Chromium-family executable names.
- Documentation Node.js fallback no longer attempts to download/run the Windows Node ZIP on macOS/Linux. Unix developers receive an actionable requirement to provide a supported Node.js version on `PATH` or through `PLAYWRIGHT_NODEJS_PATH`.

## DevExpress developer licensing

- Adds `build/Initialize-DevExpressLicense.ps1` to locate a registered DevExpress .NET key without displaying its value.
- Recognizes the official per-user default locations on Windows, macOS and Linux and the case-sensitive `DevExpress_LicensePath` / `DevExpress_License` variables.
- When a valid default file is present, the preflight exports its containing folder as `DevExpress_LicensePath` for child `dotnet` processes, which makes IDE/PowerShell build behavior deterministic on Unix-like hosts.
- Adds `build/Register-DevExpressLicense.ps1 -LicenseFile <path>` to copy a downloaded key into the correct per-user folder without placing it in the repository.
- No DevExpress license value or license file is shipped in this source archive.

## Install and local AI runtime setup

- The AI-provider workbench now contains a visible local-runtime installation guide for Ollama and LM Studio/llmster, including Windows PowerShell and macOS/Linux installation commands, standard loopback endpoints and model/server start guidance.
- A new **Open guided install actions** control takes the user directly to the existing `InitialSetupAssistantPanel`.
- The existing assistant remains the consequential-action boundary: provider install/start/model-download operations continue to run through `IAiProviderBootstrapService` and the shared bounded console only after explicit confirmation.
- The guide is localized through all six maintained LocalGPT catalogs.
- Provider-guide layout remains responsive and stacks its heading/action cleanly on narrow viewports.

## Platform service boundary

- Adds `IOllamaPlatformService` and Windows, macOS, Linux and generic implementations.
- `OllamaProcessService` no longer owns platform path probing itself; it consumes the OS-selected service through DI.
- Windows keeps its standard Ollama application/CLI locations and GUI executable handling.
- macOS searches Apple Silicon Homebrew, `/usr/local/bin`, user-local and `PATH` locations.
- Linux searches common system/user locations and `PATH`.
- Shared start/stop/restart/status behavior, logging, cancellation and process coordination remain in `OllamaProcessService`.

## Preserved behavior

- Existing LocalGPT persistence, Council, knowledge, RegEx, DXFunction, 1-Wire and logging behavior is retained.
- `@rendermode InteractiveServer` boundaries are unchanged from 3.3.0.
- DevExpress remains **25.2.9**.
- PublisherStudio is not modified by this LocalGPT archive.

## Version

LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are **3.3.1**. This stays within the repository's single-digit minor/patch convention.
