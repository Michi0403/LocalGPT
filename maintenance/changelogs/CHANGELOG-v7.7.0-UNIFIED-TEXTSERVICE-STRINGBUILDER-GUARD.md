# LocalGPT 7.7.0 — unified, non-bypassable TextServiceOwnership guard

## Reason

The 7.6.9 maintenance policy was attached as a separate `Assert-StringBuilderOwnership.ps1` script and a separate `AssertLocalGptStringBuilderOwnership` MSBuild target, duplicating an existing architectural contract. The `AssertLocalGptTextServiceOwnership` target still had a `SkipTextServiceOwnershipGuard` condition. This release returns enforcement to the existing TextServiceOwnership contract and removes that bypass.

## Changes

- **`build/Assert-TextServiceOwnership.ps1`**: preserve existing UI/controller text/regex checks and integrate the full lexical `StringBuilder` ownership scan. This scans first-party `src/**/*.cs`, `*.csx`, and `*.razor`, excluding generated `bin/obj`. It reports every forbidden file/line, and fails on any finding. Matching declared Text/Regex service classes remain the only permitted locations. Comments and string literals containing `StringBuilder` also count. No baseline, suppressions or warnings.
- **`Directory.Build.targets`**: remove the duplicate StringBuilder target and use the **existing** `AssertLocalGptTextServiceOwnership` target as the mandatory ordinary Debug/Release gate. Remove the old `SkipTextServiceOwnershipGuard` condition. Design-time build behavior is unchanged.
- **`build/Invoke-RepositoryValidation.ps1`**: run just the existing text ownership guard instead of invoking a second duplicate audit.
- **`build/Assert-RepositoryPowerShellBoundary.ps1`**: protect the unified target, check it remains unskippable and is executed, and reject the return of a duplicate target. Check the maintained guard still includes the identifier scan and its fatal failure condition.
- **Delete `build/Assert-StringBuilderOwnership.ps1`**: the logic has moved into the existing guard; no second source ownership system remains.
- Bump LocalGPT version 7.6.9 → 7.7.0. Preserve all prior cumulative tasks and source-closure work.

## Repository audit

Independent source scan (not PowerShell execution): **1,292 files**, **150 forbidden occurrences in 84 files**, **28 permitted occurrences in 11 actual Text/Regex service files**. These values exactly match the 7.6.9 baseline. No C# application implementation or UI/Join/Rejoin logic has been modified. The build is intentionally expected to fail until every forbidden occurrence is refactored into the appropriate existing DI-managed service and all contracts pass.

See `maintenance/audits/STRINGBUILDER-OWNERSHIP-7.6.9.md` for the historical baseline. A complete 7.7.0 read-only independent scan is available separately as `LocalGPT-7.7.0-TextService-Audit.md` / CSV.

## Verification boundary

- Passed: XML structure of `Directory.Build.targets` / `.csproj`, mandatory unified target wiring, source-file inventory and matching baseline counts, absence of the duplicate script/target and skip condition.
- **Not executed:** Windows PowerShell 5.1 / pwsh runtime, .NET build and Council/Join/Rejoin runtime. Owner validation must run `build/Assert-TextServiceOwnership.ps1` and the standard Debug and Release builds. The expected failure is intentional until the 150 forbidden occurrences are removed; no success is claimed.
- PublisherStudio untouched.
