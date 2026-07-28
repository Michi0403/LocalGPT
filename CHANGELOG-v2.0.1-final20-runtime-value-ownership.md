# LocalGPT 2.0.1 final20 — runtime-value ownership repair

## Breaker fixed

- Removed the service-owned `_formerThought*` and `_whitespacePattern` regex fields from `CouncilTextService`.
- Added `ICouncilTextPatternDataService` and the database-backed `CouncilTextPatternDataService`.
- Moved pattern text and options into `RegexPatterns` and moved the regex timeout into `SystemVariables`.
- Replaced the remaining direct regex operations in `CouncilTextService` with typed database-backed pattern access, including structured fields, model-thinking blocks, knowledge blocks, cleaner patterns, Minecraft naming, identifier normalization, word extraction, confidence extraction, target-framework/package-reference extraction, and whitespace normalization.
- Required pattern lookup fails closed when a database row is missing. There is no service-local regex fallback.

## Architecture and security safeguards

- Added a removal-only runtime-value ownership baseline and PowerShell guard.
- Added a final19 security/1-Wire preservation manifest and guard.
- Extended the protected repository manifest to lock the final20 data boundary, DI wiring, safeguards, build entry points, documentation, and contract test.
- Wired protected-file, security-preservation, and runtime-ownership guards into local builds, release builds, repository validation, and direct MSBuild builds after the existing 1-Wire check.
- Kept the reviewed final19 security and 1-Wire implementation files unchanged.
- Kept safeguard manifests visible through `.gitignore` rules without running Git.

## Validation status

- Python final20 architecture/security contract test: passed.
- JSON/XML parsing and source-structure checks: passed.
- Security-rule hash verification against final19: passed.
- Native .NET compilation was not run because the supplied environment has no .NET SDK.
