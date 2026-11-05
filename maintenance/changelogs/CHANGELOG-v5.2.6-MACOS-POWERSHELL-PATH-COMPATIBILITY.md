# LocalGPT 5.2.6 — macOS PowerShell path compatibility

## Release-build repair

- Fixed the build-stopping macOS/Linux compatibility finding in `build/Assert-LocalizationIntegrity.ps1`: its application source root now uses the portable repository-relative path `src/LocalGPT` instead of `src\LocalGPT` when passed to `Join-Path`.
- Audited the other PowerShell maintenance/release scripts for the same separator class rather than stopping at the reported line. Repository-relative source paths used by operational diagnostics, 1-Wire diagnostics, publish-profile validation, architecture fallback validation, static-web-asset validation, and the legacy ZIP helper now use forward-slash repository paths or normalize path metadata before filesystem access.
- `Assert-PublishConfiguration.ps1` now compares publish-directory metadata after separator normalization. Checked-in `.pubxml` values may retain their existing MSBuild representation while the maintenance script itself stays host-neutral.
- `Assert-StaticWebAssets.ps1` now canonicalizes project item identities to `/` before both comparisons and `Join-Path`, avoiding a second macOS failure after the localization guard.

## Permanent maintenance protection

- Strengthened `build/Assert-PowerShellCompatibility.ps1` so it rejects repository-relative path literals that contain Windows-only backslash separators even when they are passed indirectly through a helper that later calls `Join-Path`. The earlier check only caught a backslash literal written on the same line as `Join-Path`.
- Added source-only `build/audit_powershell_portable_paths.py`. It mirrors the portable-path rule without requiring PowerShell, allowing source-package preparation environments to catch the same regression before a macOS/Linux build reaches `pwsh`.
- Updated the repository PowerShell maintenance contract in `AGENTS.md`: repository-relative PowerShell paths use `/` on every host; platform-specific provider syntax and regex escaping remain separate concerns.

No application runtime behavior was changed. LocalGPT 5.2.5 transient vendor/editor-state ownership remains intact.
