# LocalGPT 2.8.3 — Strict async policy and Council Teams responsiveness

## Zero-tolerance async continuation policy

- Removes the historical unconfigured-await baseline. Every maintained `await` must explicitly declare continuation intent.
- `ConfigureAwait(false)` is the default for service/background/context-free work.
- `ConfigureAwait(true)` remains narrowly limited to reviewed renderer/circuit-affine component flows and the maintained lifecycle/helper allow-list.
- `await foreach` must explicitly configure its enumerable.
- `await using` must explicitly configure async disposal.
- The prior special exemption for preconfigured awaitable variables is removed; call sites now express the configuration where they await.
- The strict Python audit is mandatory; there is no weaker fallback when the audit runtime is unavailable.
- 176 formerly implicit/special-cased async constructs were migrated to explicit continuation/disposal semantics.

## Council Teams responsiveness

- Council Teams now loads persisted team/runtime configuration first instead of blocking its initial UI on provider/model discovery.
- Provider model refresh continues as supervised background work and marshals only the resulting UI update back through the renderer.
- The large DXFunction catalog is lazy-loaded only when the user expands the DXFunction picker, avoiding construction of the full catalog during route startup.
- Independent Ollama and OpenAI-compatible provider probes are started concurrently. A slow/offline remote host no longer serially delays every following provider probe.
- Existing provider discovery diagnostics and endpoint policy remain intact.

## Compatibility

- LocalGPT, WebView wrapper and installer are 2.8.3.
- 1-Wire protocol remains 2.1.1.
- Council persistence/wire shapes are unchanged.
- No database migration is required for this release.
