# LocalGPT 6.7.8 — Offline validation report

## Source and package

- Baseline: exact complete 6.7.7 source archive `LocalGPT677.zip` (2,352 files, CRC verified).
- Existing source modifications: `build/Assert-CouncilXRoundWiring.ps1`, `src/LocalGPT/LocalGPT.csproj` only.
- New documents: `CHANGELOG-6.7.8-COUNCIL-STRICTMODE-SCHEMA-GUARD-REPAIR.md` and this report.
- Source file deletions: none. Database schema/migrations: unchanged. Windows/Linux/macOS runtime identifiers: unchanged.

## Contract review

- Examined all 14 `ParameterSchemaJson` values in the maintained `LocalAiRuntimeDxAiFunctions.cs` using JSON parsing. Exactly two contain `negativePrompt`.
- For both image functions, `negativePrompt` is absent from the `required` array and its declared default is `""`.
- The 12 other schemas lack the member and are intentionally skipped by the revised strict-mode-safe lookup.
- The PowerShell guard remains wired into `AssertLocalGptNativeArchitectureMaintenance` in `Directory.Build.targets` for both Debug and Release.
- The revised PowerShell code uses member lookup rather than direct access to potentially missing optional properties; it is compatible with Windows PowerShell 5.1 and current pwsh syntax.

## Not executed in this environment

`powershell`, `pwsh`, and the .NET SDK are not available. Therefore the full Windows/macOS/Linux PowerShell guard run, Visual Studio Debug build, Release build and application runtime testing have **not** been performed. The report does not claim a successful compilation.

## Windows acceptance checks

Run from the source repository root:

```powershell
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File build/Assert-CouncilXRoundWiring.ps1

dotnet build src/LocalGPT/LocalGPT.csproj -c Debug
```

Expected: the Council source guard completes without `PropertyNotFoundStrict`; the normal build progresses past the Council guard. If further errors arise, keep them enabled and report the precise message and source line.

## Delivery

The complete source ZIP and each alternative split ZIP are verified as independently readable archives. Splits can be extracted into the same root directory to reconstruct the full source tree. A small repair ZIP includes only the changed guard, project version, and two new reports, for users already running the complete 6.7.7 source tree.
