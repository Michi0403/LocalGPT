# LocalGPT 2.5.9 — Additive Ollama provider registry

## Fixed

- `/install` no longer edits `IOptionsMonitor<ConfigurationRoot>.CurrentValue.AICore` by reference. Provider fields now use a detached draft, so typing, selecting, adding or deleting in the UI cannot mutate the live provider registry before an explicit save.
- Saving a new Ollama endpoint is additive. A previously configured Ollama endpoint is preserved even when the primary editor fields are changed to another endpoint.
- Existing Ollama hosts are removed only through an explicit Remove action. Removal is tracked as an endpoint tombstone for the save transaction so the additive merge cannot restore a deliberately deleted host.
- `Make primary` is now the explicit operation that changes the primary/default Ollama endpoint. A normal save preserves the previous primary and stores another endpoint as an additional host.
- Removing the current primary promotes a remaining configured Ollama host instead of silently destroying the whole provider family.
- Re-adding a previously removed endpoint in the same editing session cancels its removal tombstone.
- `Use Ollama gpt-oss:20b` now follows the same endpoint-qualified upsert path as discovery instead of directly overwriting the primary binding.
- `Add Ollama host` creates a blank additional host row instead of cloning the primary endpoint and creating an accidental duplicate.
- The Ollama editor now explains that adding/selecting endpoints is additive and that primary promotion/removal are explicit operations.

## Architecture

- Added `IAiProviderConfigurationRegistryService` / `AiProviderConfigurationRegistryService` to own detached provider drafts and durable Ollama registry merge semantics.
- The provider registry is keyed by normalized endpoint and preserves one preferred model per host while keeping primary/default selection separate from host existence.
- Extended the provider-qualified Council static audit so future changes cannot reintroduce direct `IOptionsMonitor` aliasing or replacement-on-save semantics.
- No maintenance guard or baseline was relaxed to accept this change.

## Versions

- LocalGPT: `2.5.9`
- LocalGPTInstallerConsole: `2.5.9`
- LocalGPTWebviewWrapper: `2.5.9`
- LocalGPT.WireProtocolVersion: unchanged at `2.1.1`

## Delivery constraint

Per the maintained delivery constraint, this source package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Validation is source/static only and is recorded in `VALIDATION-v2.5.9-source.md`.
