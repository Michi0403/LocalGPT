# LocalGPT 4.1.8 - Model catalog, CanIRun comparison and provider workbench repair

LocalGPT 4.1.8 follows 4.1.7 without changing the working Council, deployment, persistence, provider-runtime or render-mode architecture. This release focuses on the setup/model-management issues reproduced by the supplied 4.1.7 screenshot, rendered HTML and launcher log.

## Fixed

- Reworked the Ollama runtime form into stable half-width rows so bind address, port, context, keep-alive, maximum loaded models, parallel requests and queue size remain usable at ordinary desktop widths instead of collapsing into narrow DevExpress editors.
- Moved the destructive Ollama acknowledgement out of the model-grid header into its own visible block. Permanent deletion remains intentionally disabled until that acknowledgement is selected; the service-side human-confirmation guard and Ollama `/api/delete` path remain unchanged.
- Expanded the maintained Ollama setup catalog from the smaller curated seed to 92 common/current provider mappings, including Gemma 2/3/3n/4, Llama 2/3/3.1/3.2/3.3/4, Qwen 2.5/2.5 Coder/3/3 Coder/3 VL/3.5/3.6, DeepSeek, Mistral/Mixtral, Phi, CodeGemma, GPT-OSS and related variants.
- Added conservative mapping for CanIRun.ai identities such as Gemma 4 E2B/E4B/A4B, Gemma 3n, Qwen3-VL, Llama 4 Scout/Maverick, Mixtral and vision tags while refusing ambiguous provider identities.
- Expanded the explicit official-provider catalog lookup from 6 families / 60 concrete IDs to 16 families / 256 concrete IDs while keeping the lookup user-triggered, bounded and independent from hardware evidence.
- Expanded an opted-in CanIRun.ai lookup beyond `/api/recommend`'s small best-picks response: LocalGPT now obtains the public model catalog and requests the source's compatibility result for each bounded catalog model, preserving CanIRun grade, score, status, quantization and memory evidence where returned. Recommendation results and full-catalog compatibility rows are de-duplicated by CanIRun model ID.
- Improved small-model ordering so effective-parameter tags such as `gemma4:e2b` and `gemma3n:e4b` participate in the same size ordering as normal `2b` / `4b` tags.

## Preserved

- 4.1.7 database-init log-spam suppression, unchanged hardware-road short-circuit, cancellation/session cleanup, stream cancellation race repair and recoverable zero-provider state.
- Explicit InteractiveServer render-mode contract and static fatal error fallback.
- No implicit model-store migration or provider-private deletion. Ollama permanent removal still uses its documented API; LM Studio deletion remains in My Models.
- CanIRun.ai remains opt-in. Provider availability and CanIRun hardware-fit evidence remain separate concepts.
