# LocalGPT 5.2.6 source validation

## Scope

Source-only repair of the macOS/Linux PowerShell release-script path regression reported by the maintainer. No `dotnet`, MSBuild, restore, publish, GitHub access, or online repository access was used.

## Confirmed failure and repair

The reported `Build-Release.ps1` preflight stopped in `build/Assert-PowerShellCompatibility.ps1` because `build/Assert-LocalizationIntegrity.ps1` passed the literal `src\LocalGPT` to `Join-Path`. The maintained portable form is now `src/LocalGPT`.

A broader source audit found additional repository-relative backslash literals in maintenance/release helpers that could become follow-on Unix failures once the first preflight error was removed. These were normalized in:

- `build/Assert-OneWireArchitecture.ps1`;
- `build/Assert-OperationalDiagnostics.ps1`;
- `build/Assert-PublishConfiguration.ps1`;
- `build/Assert-StaticWebAssets.ps1`;
- `build/Invoke-ArchitectureAudit.ps1`; and
- `build/PublishBlazorSolutionAndCreateZips.ps1`.

The publish-profile validator now normalizes separators while comparing profile output metadata, so the validation contract is path-semantic instead of Windows-string-semantic.

## Regression protection

`build/Assert-PowerShellCompatibility.ps1` now checks repository-relative quoted path literals for Windows-only separators in addition to its original same-line `Join-Path` check. This catches indirect helper patterns such as a `Require-Text 'src/LocalGPT\...'` argument whose helper later calls `Join-Path`.

`build/audit_powershell_portable_paths.py` mirrors that rule for source-only validation environments where PowerShell itself is unavailable. `AGENTS.md` records `/` as the required repository-relative PowerShell separator on every host.

## Validation executed

- `python build/audit_powershell_portable_paths.py` — passed across 73 maintained `.ps1`/`.psm1` files.
- A repository scan using the same repository-relative path classifier found zero remaining backslash-delimited repository path literals.
- The direct `Join-Path` literal scan found no path-literal violation; the only remaining line containing both `Join-Path` and a backslash is an unrelated cross-platform regex exclusion in `Assert-IteratorExceptionPolicy.ps1`.
- `python -m py_compile build/audit_powershell_portable_paths.py build/audit_release_5_2_6.py` — passed after the release audit was added.
- Maintained LocalGPT Python architecture/async/transient-state/service audits were rerun after the path repair and passed.
- `Directory.Build.targets` and `src/LocalGPT/LocalGPT.csproj` parse as XML.
- Active LocalGPT identity/cache references are aligned at **5.2.6**, satisfying the repository single-digit minor/patch rule.

PowerShell is not installed in this preparation environment, so the PowerShell parser/runtime preflight itself could not be executed here. The reported macOS failure condition and the broader repository-relative path class were both source-validated by the new mirror audit; the maintainer's macOS `pwsh Build-Release.ps1` remains the authoritative host-runtime verification.
