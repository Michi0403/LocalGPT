# LocalGPT 2.0.1 final22 — compiler fix

## Fixed

- Corrected the database-backed regex timeout lookup to read `SystemVariable.ValueString`, matching the maintained entity and EF Core mapping.
- Explicitly typed both `MatchCollection` iterator variables as `System.Text.RegularExpressions.Match`, so the knowledge and capability-gap parsers can access `Groups` without the iterator variable being inferred as `object`.
- Refreshed only the reviewed protected-file hashes for the two corrected runtime source files.

## Preserved

- No PowerShell safeguard, diagnostic baseline, security rule, 1-Wire rule, service lifetime, runtime-value ownership rule, or database-backed pattern boundary was changed or weakened.
- Regex patterns, flags, and timeout values still come from the database-backed data service and retain fail-closed behavior.
- No NuGet package, package source, project reference, or restore setting was changed.

## Validation

- All LocalGPT Python source-contract files pass, including the final22 compiler regression contract.
- The source-level reproduction of `Assert-MethodDiagnostics.ps1` reports zero new violations.
- The protected-file and final19 security-rule SHA-256 manifests validate.
- Native `dotnet` compilation and Windows PowerShell execution were unavailable in the packaging environment.
