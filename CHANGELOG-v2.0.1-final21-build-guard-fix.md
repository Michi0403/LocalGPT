# LocalGPT 2.0.1 final21 — build-guard fix

## Fixed

- Added the required exception boundary and structured error logging to `ExtractStructuredField`, `ReadTimeoutMilliseconds`, and `ParseFlags` in `CouncilTextPatternDataService`.
- Changed the affected Council diagnostics to interpolated source messages while preserving structured regex-name placeholders and omitting pattern/source content from logs.
- Updated the reviewed protected-file hashes only for the two changed source files.
- Updated the stale former-thought source contract so it now requires the database-backed pattern boundary instead of requiring a regex literal in `CouncilTextService`.

## Preserved

- No PowerShell policy, diagnostic baseline, security rule, 1-Wire rule, runtime-value ownership rule, or database-backed pattern boundary was weakened.
- Regex patterns, flags, and timeout values remain database-backed and fail closed when required data is unavailable.

## Validation

- The final20 method-diagnostics and security-policy files are byte-for-byte equivalent after normalized line endings.
- The full final19 security hash manifest and final21 protected-file hash manifest validate.
- A source-level reproduction of the method-diagnostics checks reports zero new violations.
- Native `dotnet` compilation was not available in the packaging environment.
