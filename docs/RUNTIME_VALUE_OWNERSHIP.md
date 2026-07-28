# Runtime value ownership

## Binding rule

Components, controllers, orchestration services, and text services do not own runtime regex text, regex options, match timeouts, or equivalent configurable values. They consume a typed service contract. The implementation at the persistence boundary reads the values from the application database or another explicitly serializable store.

A service lifetime does not make hardcoded runtime values acceptable. Singleton, scoped, and transient services follow the same ownership rule.

## LocalGPT implementation

`CouncilTextService` consumes `ICouncilTextPatternDataService`. `CouncilTextPatternDataService` is the database boundary and loads:

- pattern text and flags from `RegexPatterns`;
- the match timeout from `SystemVariables` through `ISystemVariableDefinitionService`;
- revised rows on later accesses, while reusing a compiled regex only while the database fingerprint is unchanged.

The former-thought patterns, structured-field extraction, model-thinking blocks, knowledge blocks, name cleaners, Minecraft project-name patterns, identifier separators, word extraction, integer extraction, target-framework/package-reference extraction, and whitespace normalization now use that boundary. `CouncilTextService` contains no direct `Regex.*`, `RegexOptions`, regex constructors, or generated cleaner calls.

## Safeguards

`build/Assert-RuntimeValueOwnership.ps1` is a removal-only architecture check. The final19 declaration inventory is the maximum accepted legacy baseline; new service/component/controller-owned runtime fields, properties, constants, or generated regex declarations fail validation. Deleting baseline debt is allowed. Adding to the baseline is not part of normal development.

`build/Assert-SecurityRulePreservation.ps1` independently verifies the reviewed final19 security and 1-Wire files. Runtime-value refactoring must not alter or weaken those rules.

`build/Assert-ProtectedRepositoryFiles.ps1` now also locks the final20 data boundary, DI wiring, architecture safeguards, build entry points, documentation, and contract test to the reviewed normalized SHA-256 manifest.

The protected-file, security-preservation, and runtime-ownership checks run from local/release PowerShell entry points, repository validation, and direct MSBuild guard targets. The existing 1-Wire architecture check runs first.

## Data changes

Seed rows are initial data only. Existing database rows remain authoritative and are not overwritten merely because a seed default changed. Required rows must be present; missing required configuration fails closed rather than silently reintroducing a service-local fallback.
