# LocalGPT 3.3.7 — PowerShell parser preflight

## Fixed

- Corrected the documentation dependency-probe restore failure message from `$restoreExitCode:` to `${restoreExitCode}:`, avoiding PowerShell's invalid scoped-variable parse on every supported platform.
- Extended `build/Assert-PowerShellCompatibility.ps1` to parse every maintained PowerShell script with `System.Management.Automation.Language.Parser` before the build continues. Parser errors now surface during the first compatibility preflight instead of deep inside documentation generation.
- Kept the 3.3.6 DocFX-only `System.Formats.Nrbf` dependency project and repair path unchanged so the next release run can finally execute that intended metadata-resolution step.
- Updated active LocalGPT version/documentation/cache markers to 3.3.7 while preserving the existing interactive render-mode boundaries.

## Why

The macOS arm64 3.3.6 release run reached `Build-Documentation.ps1` but PowerShell stopped at line 1927 before the dependency probe could execute. In an expandable PowerShell string, `$restoreExitCode:` is parsed as a scoped variable reference; braces are required when a colon follows the variable name.
