# LocalGPT 4.2.2 - Ollama provider identity canonicalization

## Fixed

- Prevents a single Ollama runtime from being presented to Council selection twice merely because the same listener exposes both native Ollama APIs and the OpenAI-compatible `/v1` facade.
- Canonicalizes an OpenAI-compatible model to native `Ollama` only after native Ollama discovery succeeds on the same scheme/normalized host/port **and** the same concrete model identifier is present through both transports.
- Preserves independent OpenAI-compatible providers and unmatched `/v1` models. LM Studio, vLLM, user-configured remote endpoints, and other compatible servers are not globally reclassified as Ollama.
- Carries an explicit OpenAI-compatible configured-model flag onto the canonical Ollama candidate when that configured route is proven to be the same model/runtime.
- Reconciles persisted pre-4.2.2 Council bindings such as `Local OpenAI-compatible — model @ http://127.0.0.1:11434/v1` to the unique matching native Ollama candidate, avoiding stale team bindings after duplicate suppression.

## Why this matters

Provider protocol compatibility is not physical-provider independence. Treating one Ollama listener as two provider identities can cause the model picker, Council composition, and provider-aware scheduling/presentation logic to overstate runtime diversity. LocalGPT now keeps transport capability separate from runtime identity while retaining provider-qualified routing for genuinely separate providers.

## Preserved

- OpenAI-compatible provider support remains available and independently configurable.
- Native Ollama execution remains canonical where native Ollama is confirmed, preserving Ollama-specific keep-alive, GPU, load/unload, and runtime-management behavior.
- LocalGPT 4.2.1 metadata-backed console identity and installer repository metadata behavior are unchanged.
- LocalGPT 4.2.0 live Ollama catalog, CanIRun comparison, runtime workbench, and prior cancellation/storage/macOS packaging fixes remain unchanged.
