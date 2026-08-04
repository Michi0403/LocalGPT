# LocalGPT 2.2.9

## Build fixes

- Fixed `Assert-TextServiceOwnership.ps1` findings by moving provider-model signature and reviewer-summary composition from Razor components into `CouncilTextService`.
- Fixed `Assert-IteratorExceptionPolicy.ps1` findings by materializing configured Ollama options into an `IReadOnlyList` with logged exception handling.
- Fixed `Assert-SystemVariableInitialization.ps1` by moving provider identity construction out of the benchmark route initializer and reusing a provider name value.
- Fixed the ambiguous `ConfigurationRoot` compiler error with an explicit alias to `LocalGPT.BusinessObjects.ConfigurationRoot`.
- Removed four unread `MultiModelCouncilService` primary-constructor parameters. Their behavior remains owned and used by `ProviderModelRuntimeService`.

## Council runtime corrections

- A thinking-only model receives one bounded final-only recovery request. If recovery remains non-substantive, the step now carries an error and is counted as a runtime benchmark failure.
- Failed peer verification no longer replaces or decorates a valid consensus with the missing-final-answer notice. The consensus is retained and a warning records the verifier failure.
- Provider-qualified endpoint/model identity, mixed-provider Council execution, benchmark panels and user-approved recommendation presets remain unchanged.

## Versioning

- LocalGPT application version: `2.2.9`.
- `LocalGPT.WireProtocolVersion`: `2.1.1` (unchanged; no wire-contract change).
