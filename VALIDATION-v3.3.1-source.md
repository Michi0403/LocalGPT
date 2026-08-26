# LocalGPT 3.3.1 source validation

This handoff is **source-only**. The preparation environment does not provide `dotnet`, MSBuild, `pwsh`, or Windows build tooling, so no restore, compile, publish, PowerShell execution, application launch, or runtime test is claimed. No GitHub repository access was used.

## Source assertions completed before packaging

A final source-side validation pass completed **64/64 assertions** successfully. The checks covered:

- all three project files and `NuGet.Config` parse as XML;
- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are all version **3.3.1**;
- the Windows wrapper contains `EnableWindowsTargeting=true`;
- normal NuGet restore uses NuGet.org and no longer requires an unconditional `./packages` source;
- all six localization JSON catalogs parse and contain the same **2,026 keys**, including the new guided-install action;
- `/install` retains `@rendermode InteractiveServer`;
- the existing `InitialSetupAssistantPanel` remains wired into the Setup Guide and the provider workbench exposes a direct guided-action transition;
- Ollama and LM Studio/llmster Windows plus macOS/Linux installation guidance is present;
- `IOllamaPlatformService` is registered with Windows, macOS, Linux and generic implementations and consumed by `OllamaProcessService`;
- the shared Ollama process coordinator no longer contains operating-system path probing;
- DevExpress license preflight/registration helpers contain the intended Windows/macOS/Linux per-user paths and exact case-sensitive variable names;
- no `DevExpress_License.txt` or `LCXv1...`-shaped license value is present in the source tree;
- `Build-LocalDevelopment.ps1` and `Build-Release.ps1` invoke the PowerShell compatibility and DevExpress-license preflights and verify that `dotnet` exists;
- maintained build/helper `Join-Path` calls contain no Windows-only backslash-delimited child path literals;
- documentation browser discovery includes common macOS application bundles and Linux Chromium-family executable names;
- non-Windows documentation generation no longer attempts the Windows-only Node ZIP fallback and instead requires an installed supported Node.js when needed;
- the complete Razor `@rendermode` map is identical to the supplied LocalGPT 3.3.0 source: **20 render-mode-owning component files, no removals or changes**;
- no merge-conflict markers were found in maintained text source;
- coarse structural brace checks for the new/changed platform-service C# units are balanced.

## DevExpress licensing contract reviewed

The repository helpers follow the DevExpress 25.2 .NET licensing contract used by the project:

- Windows: `%AppData%/DevExpress/DevExpress_License.txt`
- macOS: `$HOME/Library/Application Support/DevExpress/DevExpress_License.txt`
- Linux: `$HOME/.config/DevExpress/DevExpress_License.txt`
- custom folder: case-sensitive `DevExpress_LicensePath`
- direct key variable: case-sensitive `DevExpress_License`

The helper never prints the license value and the source package deliberately contains no personal license material.

## Build/runtime verification still required on a developer machine

Because no .NET/PowerShell toolchain is present in the preparation environment, the following remain the developer-machine gate:

1. `dotnet restore` / `dotnet build` of the main LocalGPT project on the target host;
2. `pwsh ./Build-LocalDevelopment.ps1` on Windows/macOS/Linux as appropriate;
3. Windows cross-build of `LocalGPTWebviewWrapper` from non-Windows hosts, followed by native Windows execution verification;
4. runtime validation of the confirmation-gated Ollama and LM Studio/llmster setup actions;
5. DevExpress licensed build verification with the developer's own 25.2-compatible key.

No statement in this validation document should be read as a compilation or runtime success claim.
