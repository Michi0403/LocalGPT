# LocalGPT 2.2.8

## Provider-qualified Council and Benchmark Council

LocalGPT 2.2.8 makes the provider endpoint part of a model's runtime identity. Council and Chat selections now retain provider kind, provider name, endpoint and provider-native model name, so identically named models can run concurrently from Ollama, LM Studio or another OpenAI-compatible host, OpenAI, and Azure OpenAI without being addressed through a bare model name.

### Model controls

- Adds one reusable provider-model Razor panel that inherits the containing page's Interactive Server circuit.
- Places provider properties and bounded benchmark controls beside individual models in Chat, Model Council and tested local-provider discovery on Install.
- Adds keyboard-visible disclosure state, associated controls, status announcements, cancellation and result details.
- Adds a Benchmark Council panel that runs all selected provider-qualified models and lets the selected peers review each target's recommendation.

### Benchmark recommendations

- Benchmarks bounded latency, balanced, quality and maximum profiles, plus an Ollama CPU-safe control when applicable.
- Keeps calls bound to the selected provider endpoint and resolves credentials only from that provider's configured settings.
- Binds local OpenAI-compatible credentials to the exact configured endpoint during both discovery and execution; LM Studio fallback probes never inherit another provider's key.
- Avoids passing Ollama GPU controls to OpenAI-compatible or cloud providers.
- Requires a fresh user action before recommendations are persisted.
- Applies successful recommendations as user-approved model presets whose routes retain provider kind, endpoint and provider-native model name.
- Supports applying one recommendation or all successful Benchmark Council recommendations directly from the model panels.

### Compatibility and addressing safety

- Legacy bare model names continue to migrate when exactly one provider exposes that name.
- Ambiguous bare names are rejected and require selection of the provider-qualified entry.
- Ambiguous bare Council-leader names fall back to the normal deterministic leader selection instead of choosing an arbitrary provider.
- Stale provider-qualified addresses are rejected instead of silently falling back to Ollama.
- Historical unqualified preset routes are treated as legacy Ollama routes, preserving their prior GPU settings until they are safely qualified.
- Multi-provider recovery recreates the correct provider client; Ollama CPU fallback remains Ollama-only.
- Provider-qualified OneWire routes hydrate authoritative runtime selections so external Council requests retain provider identity across discovery changes.

### Contract and versioning

The LocalGPT application version is 2.2.8. The separately versioned `LocalGPT.WireProtocolVersion` contract is 2.1.1 because Council route DTOs now include provider kind, provider name, provider endpoint and provider-native model name.

### Validation boundary

The source passes the repository's static architecture and explicit async-continuation audits plus JSON, XML/MSBuild, YAML, JavaScript, Python, version and ZIP-integrity validation in the packaging environment. A .NET SDK, Windows desktop build and live GitHub Actions runner were unavailable there, so the delivered archive is marked `UNVERIFIED` until built in the intended Windows/.NET environment.
