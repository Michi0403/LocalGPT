# LocalGPT 6.8.3 — Offline verification

## Source and cause

Input archive: `LocalGPT-6.8.2-Source-Download.zip`, supplied in this conversation. User-provided macOS logs confirm the 6.8.2 PowerShell compatibility, platform boundaries (893 C# sources, 35 contracts), sandbox check, browser integrity, and static web assets passed before failure at the application version resolver. The `LocalGPT.csproj` XML contains two PropertyGroup elements; only one declares Version, so strict access to `$_.Version` on all groups is invalid.

## Offline static checks

- `build/Get-ProjectVersion.ps1` resolves actual version XML nodes with XPath `/Project/PropertyGroup/Version`; no unchecked `PropertyGroup | ForEach-Object { $_.Version }` remains in either build entrypoint.
- The same XPath was evaluated offline against the actual application and release-packaging project XML and eight synthetic XML cases. Existing fields, later version groups, missing/blank values, multiple versions, and an unrelated ItemGroup Version were covered. This evaluates the XML/data contract, **not** PowerShell runtime execution.
- `build/Assert-ProjectVersionResolution.ps1` is called from the full-release preflight, local-development entrypoint and release compile-only preflight, so the fixture validation executes before any .NET build in all three paths.
- Both build entrypoints call the same helper for all project version resolution performed by their previous `Resolve-ProjectVersion` functions.
- Application version is **6.8.3**; the release-packaging tool remains independently at **1.1.2**, and the wire protocol is unchanged.
- Archive CRC and contents were checked after packaging; unrelated existing source files are retained from the supplied archive.

## Not executed

No PowerShell executable is available here. Windows PowerShell 5.1, macOS `pwsh`, all repository `.ps1` guards, actual .NET compilation, DocFX, frontend execution, installer generation, native signing, and full release packaging remain unverified. The first execution recommended on the macOS source checkout is `pwsh -NoProfile -File ./build/Assert-ProjectVersionResolution.ps1`, followed by the normal `pwsh -NoProfile -File ./Build-Release.ps1`.
