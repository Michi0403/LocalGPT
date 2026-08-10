# LocalGPT 2.5.8 — Text-service ownership repair

## Scope

This is a deliberately small follow-up to 2.5.7. The Windows build reached the maintained text-service ownership guard and correctly rejected one direct `string.Join` operation added to `Components/Pages/Chat.razor` by the stale provider-qualified Council selection notice. The application otherwise compiled when that guard was disabled, so this release changes only the ownership boundary and release metadata rather than touching the multi-Ollama behavior again.

## Fixed

- Moved unavailable provider-selection preview formatting out of `Chat.razor` and into the already injected `CouncilTextService`.
- Added `CouncilTextService.ProviderUnavailableSelectionNotice(...)` with the required try/catch and diagnostic logging boundary.
- `Chat.razor` now delegates the complete user-facing stale-route notice construction to `CouncilTextService`; it no longer performs the new direct `string.Join` operation that violated the repository policy.
- The text-service ownership baseline and guard were **not** weakened or expanded to whitelist the violation. The source was changed to comply with the existing architecture rule instead.
- Multi-Ollama endpoint qualification, provider registry behavior, exact Council preflight, no-same-name-fallback behavior and Install/Chat provider behavior from 2.5.7 are otherwise unchanged.

## Preserved

- `ConfigureAwait(false)` remains the default continuation policy.
- Existing renderer-affine continuation sites are unchanged.
- Existing logging, service resilience, architecture, localization and 1-Wire boundaries remain intact.
- `@rendermode InteractiveServer` was not removed from maintained pages.
- LocalGPT 1-Wire protocol version remains unchanged.

## Version

- LocalGPT: `2.5.8`
- LocalGPTInstallerConsole: `2.5.8`
- LocalGPTWebviewWrapper: `2.5.8`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)

## Build boundary

Per the delivery constraint, this source package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Validation is source/static only and is recorded in `VALIDATION-v2.5.8-source.md`.
