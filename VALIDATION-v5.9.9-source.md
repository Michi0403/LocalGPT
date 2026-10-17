# LocalGPT 5.9.9 — static source verification

**Baseline:** exact 5.9.8 full source ZIP from this conversation. No network or GitHub downloads.

## Regression verified

- `Build-Release.ps1` had the sole hardcoded packaging helper version (`1.0.7`).
- `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`, `build/Ensure-ReleasePackagingPackage.ps1`, and `build/Publish-ReleasePackagingPackage.ps1` all declare `1.1.0`.
- The earlier log confirms the failed comparison before any packaging build, and confirms that the preceding macOS preflights passed.

## Repair and retained contracts

- `Build-Release.ps1` resolves the packaging helper version from the helper csproj; aborts if project/version missing; logs resolved version without changing the helper project, package checks, or version.
- Existing `Ensure-ReleasePackagingPackage.ps1` continues to reject mismatched versions, verify current license notice entries, install into a repository-local tool root, and require PDF merge capabilities.
- All uses of `$releasePackagingVersion` and derived package filenames in the release script resolve to the same value, and there is no remaining `1.0.7` packaging request in executable scripts.
- Main app version `5.9.8` → `5.9.9`; packaging helper stays `1.1.0`, 1-Wire protocol stays `2.1.1`.
- All original archive files are retained byte-for-byte except `Build-Release.ps1` and `src/LocalGPT/LocalGPT.csproj`, with this report and a changelog added.

## Limitations

No PowerShell or .NET runtime is installed in this environment. XML parsing, structural assertions, source retention, uniqueness of ZIP names, and ZIP CRC were checked offline, but **PowerShell execution and the full macOS publishing pipeline remain untested**. The definitive test is the user's next `pwsh ./Build-Release.ps1` run.
