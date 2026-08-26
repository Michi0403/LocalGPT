# LocalGPT 3.3.9 source validation

Static validation scope only; no `dotnet` or `pwsh` build was executed in the packaging environment.

## Verified

- All three product projects declare version 3.3.9.
- The DocFX theme post-processor repairs missing/non-populated `<html lang>` attributes with `lang="en"`.
- `.github/scripts/prepare-pages-artifact.py` exposes an HTML-only validation mode that reuses the production accessibility and local-link checks without requiring the final PDF/status files.
- `Build-Documentation.ps1` executes that HTML-only gate before PDF rendering and temporarily supplies the versioned PDF link stub only for link validation.
- The temporary PDF stub is removed before the early HTML publication pass and before real PDF generation.
- `documentation-status.json` records `htmlPreflightValidated`; Release and LocalDevelopment guards require it.
- Shared build prerequisites check Python 3 before the long build begins.
- The pinned `System.Formats.Nrbf` DocFX-only dependency probe remains isolated from `LocalGPT.csproj`.
- Source ZIP uses the Finder-compatible repository-root layout and is checked for duplicate, case-insensitive and Unicode-normalized path collisions.
