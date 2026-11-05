# LocalGPT 4.1.7 - Runtime churn, provider storage and setup repair

LocalGPT 4.1.7 completes the work started from 4.1.6 and addresses the post-cancellation churn visible in the supplied runtime log without changing the established provider/Council architecture.

## Runtime and cancellation

- Makes repeated `IDatabaseInitializationService.InitializeAsync` calls idempotent at the logging boundary: the migration/feed completion event is emitted only when that call actually performed initialization work; already-initialized calls remain trace-only.
- Stops unchanged Council hardware-road synchronization from repeatedly normalizing and logging the same routes.
- Prevents a completed or cancelled live Council session from scheduling another attachment/session refresh cycle.
- Keeps expected method-level cancellation diagnostics stack-free while preserving cancellation propagation.
- Reads Ollama streaming frames directly until EOF instead of probing `StreamReader.EndOfStream`; if the HTTP stream ends while cancellation is already requested, it is normalized to the operation cancellation path rather than logged as a provider failure.
- Lets the chat selector survive a transient zero-provider interval and recover when provider discovery becomes available again instead of throwing during component construction.

## Provider catalog and model discovery

- Separates provider-catalog knowledge from optional CanIRun.ai hardware-fit evidence in setup state and UI.
- Orders lightweight provider-model variants before larger alternatives within the same installed/non-installed tier.
- Extends maintained Ollama aliases with small Gemma, Qwen/Qwen3/Qwen3.5, DeepSeek, Llama, CodeGemma and Phi variants plus `llama2-uncensored:7b` and the maintained larger variants.
- Keeps user-triggered provider-catalog results installable even when no CanIRun.ai record exists.

## Ollama storage

- Extends the platform abstraction with bounded mounted-volume discovery, including macOS `/Volumes` and standard Linux mount roots.
- Detects Ollama-shaped stores only when both `blobs` and `manifests` exist; mounted-volume inspection is shallow and bounded rather than recursive.
- Shows provider-documented default, inherited `OLLAMA_MODELS`, LocalGPT override, detected stores, mounted roots, effective path and effective-source evidence separately.
- Does not silently move or delete model files when storage settings change.

## Interface and localization

- Makes the provider runtime controls and storage evidence responsive on narrow layouts.
- Gives the Matrix/operator and initial-setup consoles a stable initial height with internal scrolling instead of line-by-line growth.
- Adds English/German localization parity for the new provider/storage evidence strings; both maintained catalogs contain 2,256 matching keys.
- Preserves the existing render-mode architecture: routed interactive pages/islands remain `InteractiveServer`; inherited nested components and the fatal error fallback remain intentionally static/inherited.
