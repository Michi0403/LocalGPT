# LocalGPT 6.8.1 — offline validation

## Inputs

- Base: `LocalGPT_6.8.0_source.zip` supplied in this conversation.
- Reported host: macOS (`darwin-arm64`), `pwsh Build-Release.ps1`.
- Both issues in the log were analyzed. The first (`ArgumentList` detection on literal patterns) is already fixed in the input ZIP; the second (two C# OS branches outside platform boundaries) was reproduced from the input sources and corrected.

## Checks performed without PowerShell or .NET

1. Scanned **893** non-generated C# source files under `src/LocalGPT` using the same platform branch and Windows-specific detail patterns as `Assert-CrossPlatformBoundaries.ps1`; **0 violations** after the patch.
2. Verified **34** regex-based source contracts in `Assert-SessionSandboxIsolation.ps1` against the patched files; all matched, including the new platform selection assertions.
3. Verified the C# platform contract is implemented by exactly the existing Windows/Unix runtime service classes, and that both sandbox consumers depend on it. Source-based confirmation only; no compiled runtime behavior was evaluated.
4. Verified new source version `6.8.1` and early guard order before `Initialize-BuildPrerequisites.ps1` in both build entrypoints.
5. Verified the ZIP can be read without integrity errors after packaging.

## Not executed

- `pwsh` or Windows PowerShell 5.1 parsers/guards (PowerShell executable unavailable in this environment).
- `dotnet build`, `dotnet publish` or release packaging scripts.
- The Hyperlight guest smoke tests, notarization/signing, DevExpress license checks, documentation build, and cross-OS runtime behavior.

## Suggested host verification

Run from the repository root before repeating the full release:

```powershell
pwsh -NoProfile -File ./build/Assert-PowerShellCompatibility.ps1
pwsh -NoProfile -File ./build/Assert-CrossPlatformBoundaries.ps1
pwsh -NoProfile -File ./build/Assert-SessionSandboxIsolation.ps1
```

Then run `pwsh ./Build-Release.ps1`. These static guards cannot guarantee that all downstream compilation, packaging, SDK, dependency, or OS runtime issues are absent.
