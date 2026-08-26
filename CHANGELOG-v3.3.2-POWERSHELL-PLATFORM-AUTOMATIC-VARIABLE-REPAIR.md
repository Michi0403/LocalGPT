# LocalGPT 3.3.2 — PowerShell platform automatic-variable repair

## Why this release exists

LocalGPT 3.3.1 introduced cross-platform DevExpress license preflight helpers. On PowerShell 7 (`pwsh`) the helper used a local variable named `$isWindows`. PowerShell variable names are case-insensitive, so this collides with PowerShell 7's built-in read-only `$IsWindows` automatic variable and stops `Build-Release.ps1` before the actual build begins with:

`Cannot overwrite variable IsWindows because it is read-only or constant.`

## Fix

- `build/Initialize-DevExpressLicense.ps1` now uses the repository-specific `$runningOnWindows` variable instead of `$isWindows`.
- `build/Register-DevExpressLicense.ps1` receives the same correction so manual license registration cannot hit the identical PowerShell 7 failure.
- `build/Assert-PowerShellCompatibility.ps1` now rejects assignments to PowerShell 7 read-only platform automatic variables (`IsWindows`, `IsLinux`, `IsMacOS`, `IsCoreCLR`) regardless of casing.
- `Build-Release.ps1` now runs the PowerShell compatibility guard before the DevExpress license preflight, so future script-level portability regressions are reported before a build helper is invoked.

## Preserved 3.3.1 work

The 3.3.1 cross-platform build/install work is retained: Windows cross-targeting for the WinUI wrapper, normal NuGet restore without a mandatory repository-local package source, the guided Ollama/LM Studio setup surface, OS-specific Ollama services selected through DI, portable PowerShell path handling and DevExpress license discovery/registration for Windows, macOS and Linux.

`@rendermode InteractiveServer` ownership remains unchanged from 3.3.0/3.3.1. DevExpress remains 25.2.9.

## Version

LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are **3.3.2**. This follows the repository's single-digit minor/patch convention.
