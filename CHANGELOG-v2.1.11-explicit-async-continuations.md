# LocalGPT 2.1.11 — Explicit asynchronous continuation control

## Corrected behavior

- Restores explicit continuation configuration instead of deleting it from Blazor components.
- Applies `ConfigureAwait(false)` to every context-free await expression across services, controllers, persistence, diagnostics, network operations, background workflows, and non-lifecycle component methods.
- Applies `ConfigureAwait(true)` only inside `OnAfterRenderAsync`, where lifecycle code continues by changing renderer-owned state or invoking browser UI integration.
- Keeps both existing `ConfiguredTaskAwaitable` usages because those awaitables were explicitly configured by their caller and cannot be configured a second time.
- Keeps `await using` as the C# asynchronous-disposal syntax. Any awaited initializer used by an `await using` declaration is explicitly configured independently.

## Architecture enforcement

- Replaces the broad per-file async baseline with one exact policy.
- Rejects any ordinary await expression without explicit `ConfigureAwait(true/false)`.
- Rejects `ConfigureAwait(true)` outside `OnAfterRenderAsync`.
- Rejects `ConfigureAwait(false)` inside `OnAfterRenderAsync`.
- Requires `await foreach` sources to use `ConfigureAwait(false)`.

## Versioning

- LocalGPT application and organic 1-Wire advertisement: `2.1.11`.
- The independently maintained `LocalGPT.WireProtocolVersion` package remains unchanged.
