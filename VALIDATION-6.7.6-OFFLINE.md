# LocalGPT 6.7.6 — Offline verification

**Source baseline:** LocalGPT 6.7.5 complete source ZIP.

- XML parsing: `Directory.Build.targets`, `LocalGPT.csproj` **passed**.
- JavaScript syntax: `node --check` of `localgpt-reconnect.js` **passed**.
- Browser-state simulation (outside repository, not a shipped test): `components-reconnect-rejected` and `components-reconnect-failed` both navigated to the correct `rejoinCouncilRunId` **passed**.
- Portability audit: reviewed PowerShell AST-based detection of actual `.ArgumentList` member expressions versus quoted source-policy strings. No PowerShell interpreter exists in the packaging environment, so actual `pwsh` execution **not verified**.
- Standard build wiring: cross-platform preflight is a mandatory LocalGPT `BeforeBuild` target, not limited to Release. No skip property was added.
- Version is 6.7.6 (single-digit minor and patch components). All seven RuntimeIdentifiers retained.
- Archive CRC and baseline preservation checked by packaging script.

**Required host acceptance:** Run `dotnet build src/LocalGPT/LocalGPT.csproj -c Debug` (Windows or macOS), then `pwsh build/Assert-PowerShellCompatibility.ps1`, and a normal release build. Separately test an actual long-running Council under a rejected Blazor circuit; verify automatic reload restores the exact run and its transcript. Investigate the unresolved framework stream error independently.
