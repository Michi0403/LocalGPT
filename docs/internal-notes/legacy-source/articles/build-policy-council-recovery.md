# Build-policy and Council recovery patch

LocalGPT 2.2.9 is a corrective release for the provider-qualified Council and Benchmark Council milestone. It preserves the 2.2.8 model identity, mixed-provider execution and benchmark UI while restoring all maintained build gates.

## Build-policy corrections

- Provider-model selection signatures and reviewer summaries are composed by `CouncilTextService` instead of Razor components.
- Configured Ollama providers are materialized into a bounded list rather than exposed through a new iterator that bypasses the iterator exception policy.
- `ProviderModelRuntimeService` aliases the LocalGPT `ConfigurationRoot` explicitly.
- Adaptive Ollama preset routes reuse a provider identity instance and a provider-name value outside the route initializer.
- Obsolete constructor dependencies were removed from `MultiModelCouncilService`; the provider runtime remains the owner of formatter, protocol, prompt and function-registry dependencies.

## Council result accounting

A model that emits thinking but no substantive final answer receives one bounded final-only recovery request. If that recovery is still empty or non-substantive, the Council step is now marked failed. Runtime benchmark summaries therefore no longer report such steps as successful.

A failed peer verifier is recorded as a warning and the valid consensus is retained. The missing-final-answer notice is no longer appended under a misleading `Peer verification` heading.
