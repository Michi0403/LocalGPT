# LocalGPT 6.8.2 — Source-tree coherence and cross-platform release preflight

## Reported failure and verified diagnosis

The third macOS `pwsh Build-Release.ps1` log ran the **6.8.1** `Assert-CrossPlatformBoundaries.ps1` (the new `GetNativeExecutableName("hluk")` checks at line 177) against **older** `HyperlightUnikraftSessionSandboxProvider.cs` and `SessionSandboxExecutionService.cs` source containing `OperatingSystem.IsWindows()`. The 6.8.1 source ZIP contains **neither** of those direct OS branches and **both** platform-service calls. The reported four findings therefore describe an inconsistent build directory, not the 6.8.1 ZIP in isolation. The log alone does not prove how that inconsistency arose.

## Changes

- **Clear per-line OS diagnostics**: `build/Assert-CrossPlatformBoundaries.ps1` reports source file, line, and the actual matched OS/API expression. The original forbidden-platform checks are retained rather than disabled, waived, or added to the approved exception list.
- **Mixed-source detection**: when older direct OS branching is present in either sandbox implementation but the required `platformRuntime.GetNativeExecutableName("hluk")` call is missing, the audit identifies the incompatible file(s), SHA-256 prefix, and the safe remedy: extract the **complete** source ZIP into a **new, empty** directory.
- **Version and source-root visibility**: the audit reads the current application version from `LocalGPT.csproj` and logs the exact repository path being checked. It does not hardcode expected release versions.
- **Windows and Unix class-scoped assertions**: the cross-platform audit and sandbox guard validate `hluk.exe` inside `WindowsPlatformRuntimeService` and `hluk` inside `UnixPlatformRuntimeService`, not merely as two matching strings anywhere in the source file.
- **Compile-only parity**: `build/Invoke-ReleaseCompileValidation.ps1` now runs the cross-platform and sandbox guards, so `pwsh Build-Release.ps1 -CompileOnly` checks those boundaries before compilation, just like a full release.
- **Version**: `src/LocalGPT/LocalGPT.csproj` advances from **6.8.1** to **6.8.2**. PublisherStudio 4.7.1 is unchanged.

The two sandbox C# implementations and `IPlatformRuntimeService` were already correctly updated in the supplied **6.8.1** ZIP; these files are retained unchanged to avoid unnecessary behavior changes.

## macOS: avoid combining revisions

Run this with the new archive at a location you choose, **not** over an older extraction. Do not overwrite/delete the existing working copy or its uncommitted data.

```bash
mkdir -p /Volumes/compilestick/localgpt-6.8.2
# Destination must be empty before extraction.
ditto -x -k /path/to/LocalGPT_6.8.2_source.zip /Volumes/compilestick/localgpt-6.8.2
cd /Volumes/compilestick/localgpt-6.8.2
pwsh -NoProfile -File ./build/Assert-PowerShellCompatibility.ps1
pwsh -NoProfile -File ./build/Assert-CrossPlatformBoundaries.ps1
pwsh -NoProfile -File ./build/Assert-SessionSandboxIsolation.ps1
pwsh -NoProfile -File ./Build-Release.ps1
```

Verify the two actual extracted source files against the hashes in `VALIDATION-6.8.2-OFFLINE.md` when a mismatch is suspected. This does not require .NET, network, or GitHub.

## Validation limits

Static inspection confirmed that 6.8.2 contains no platform-branch source findings and has matching platform contracts. The actual Windows/macOS PowerShell guard execution, .NET compilation, publishing, and full release were **not executed** in this environment; no success is claimed for them.
