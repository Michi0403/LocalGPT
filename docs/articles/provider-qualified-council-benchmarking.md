# Provider-qualified models and Benchmark Council

LocalGPT 2.2.8 addresses every Council model by provider, endpoint and provider-native model name. The visible Council address has this shape:

```text
Provider — model @ endpoint
```

For example, an Ollama model and an LM Studio model may both be named `qwen3:8b`; their endpoints keep them separate. A bare model name is accepted only when exactly one discovered or configured provider exposes it.

## Reusable model panel

The provider-model panel is used in three places:

- Chat, for active single-model sessions and Council candidates.
- Model Council, for each selectable participant.
- Install, after a local Ollama or LM Studio/OpenAI-compatible host was tested and its models were discovered.

The panel exposes selection or activation, provider properties, endpoint identity, bounded benchmark settings, cancellation, result details and an explicit apply action. It inherits the Interactive Server render circuit from its containing page rather than creating nested render boundaries.

## Single-model benchmark

A single model can run bounded deterministic tasks with configurable profile count, task count, timeout, maximum context and maximum output. Optional Council review asks up to three other selected provider-qualified models to review the measured recommendation. When no independent reviewer is available, the target performs a bounded self-review.

Ollama profiles may include a CPU-safe control. OpenAI-compatible and cloud routes never receive Ollama GPU settings.

## Benchmark Council

Benchmark Council runs all currently selected provider-qualified models as targets. The other selected models review each target where possible. A result is not applied automatically. The user can apply all successful recommendations as one user-approved preset.

The saved preset contains one route per successful model and retains:

- provider kind;
- provider name;
- endpoint;
- provider-native model name;
- recommended context and output bounds;
- Ollama GPU setting only for Ollama routes.

Applying a preset configures future Council routing. It does not modify provider-global server settings or expose provider credentials.
Configured local-provider credentials are used only for the exact configured endpoint. Automatic LM Studio fallback discovery and execution use no credential from another endpoint.

## Mixed-provider Council behavior

Chat and Model Council can combine Ollama, LM Studio or another configured OpenAI-compatible endpoint, OpenAI and Azure OpenAI in one run. The runtime creates the correct client for each participant immediately before its call. Same-named models are never merged merely because their model strings match.

Legacy presets that store bare model names migrate only when the name resolves to one provider. If multiple providers expose the name, LocalGPT asks the user to choose the provider-qualified entry rather than guessing. A stale qualified selection also fails clearly instead of falling back to another provider.

Provider-qualified OneWire model routes are converted into authoritative Council selections before execution. A parallel legacy list of bare model names is treated as compatibility metadata and cannot redirect those routes to a different provider. Ambiguous bare leader names likewise never select an arbitrary endpoint.
