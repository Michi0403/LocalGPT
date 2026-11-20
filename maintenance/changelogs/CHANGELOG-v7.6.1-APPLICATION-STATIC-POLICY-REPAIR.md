# LocalGPT 7.6.1 — Application-static policy repair

**Baseline:** 7.6.0 source candidate. **Status:** source-only candidate, owner build pending.

## Reported build failure

On 20 November 2026 the owner Debug build completed six prerequisite projects and reached the LocalGPT application-static policy gate. `Assert-ApplicationStaticPolicy.ps1` invoked `Invoke-ArchitectureAudit.ps1 -Mode static`, which rejected `Services/MultiModelCouncilService.FileWorkAuthority.cs|private static async Task<string> PreserveFileWorkTechnicalEvidenceAsync(...)`. MSBuild reported MSB3073 at `Directory.Build.targets:217`. This is a genuine repository architecture violation, not a compiler or toolchain compatibility problem. LocalGPT compilation and later gates were not established by that build.

## Source repair

- In `src/LocalGPT/Services/MultiModelCouncilService.FileWorkAuthority.cs`, convert `PreserveFileWorkTechnicalEvidenceAsync` and `ReadFileWorkReleaseField` to private instance methods of the existing DI-owned Council service. The second declaration was also prohibited by the same gate, even though its terminating error output did not reach that finding.
- Keep existing callers, methods' bodies and XML documentation unchanged. Preserve async file operations, cancellation, atomic content-addressed retention and release metadata parsing; no new DI registration, configuration or database migration.
- Leave `Assert-ApplicationStaticPolicy.ps1`, `Invoke-ArchitectureAudit.ps1`, all other guards and build wiring unchanged. The existing mandatory static policy is the regression guard for both declarations.
- Increment the authoritative application version from 7.6.0 to 7.6.1. Append the cumulative task ledger; retain the 7.6.0 evidence and all open acceptance work.

## Validation

Source inspection confirms both offending modifiers are removed and no other static member declaration is present in the affected partial. Archive integrity and changed-entry scope are checked during packaging. PowerShell is unavailable in the editing environment; the guard and .NET compilation were not executed. No online GitHub access or database modification. PublisherStudio 4.9.1 is unchanged.

Owner acceptance, from repository root:

```powershell
.\build\Assert-ApplicationStaticPolicy.ps1
```

Then run the normal build with all gates enabled. Resume the existing 7.6.0 technical-evidence and project-delivery runtime acceptance only after the build succeeds. This repair does not certify other gates, compilation or runtime functionality.
