# LocalGPT 4.1.1 — Provider runtime, model storage, and cleanup management

## Why

Large local models can exhaust a laptop's system disk quickly, especially when several quantizations or models are downloaded during experimentation. The existing setup flow could install/discover Ollama and LM Studio models, but it did not provide a complete provider-native cleanup/storage/runtime workbench.

## Ollama

- Added a persisted LocalGPT-owned runtime section for model directory, bind/port, context length, keep-alive, loaded-model limit, parallel requests, queue size, and cloud-disable policy.
- LocalGPT-started Ollama CLI processes receive reviewed `OLLAMA_MODELS`, `OLLAMA_HOST`, `OLLAMA_CONTEXT_LENGTH`, `OLLAMA_KEEP_ALIVE`, `OLLAMA_MAX_LOADED_MODELS`, `OLLAMA_NUM_PARALLEL`, `OLLAMA_MAX_QUEUE`, and `OLLAMA_NO_CLOUD` values.
- Model directory selection uses the existing host path explorer and supports external volumes; leading `~/` is expanded through the current user's home directory without invoking a shell.
- Changing the target model directory never moves or deletes the old Ollama store implicitly.
- Added downloaded/running model inventory from `/api/tags` and `/api/ps`, unload through `keep_alive = 0`, and permanent delete through `DELETE /api/delete`.
- Permanent deletion requires explicit human acknowledgement.
- Added provider-reported disk usage and filesystem free-space visibility plus a low-space warning.

## LM Studio

- Added downloaded/loaded inventory through `lms ls --json --detailed` and `lms ps --json` in direct-process mode.
- Added model size, parameter count, architecture, loaded state, and maximum-context display when reported by LM Studio.
- Added memory estimation (`lms load --estimate-only`), load, unload, context-length, GPU-offload, TTL, server bind/port, and CORS controls.
- LM Studio permanent downloaded-model removal and model-directory relocation intentionally remain in LM Studio → My Models because the documented CLI/API does not expose equivalent supported operations. LocalGPT does not delete provider-owned model files by guessing their storage layout.
- Existing OpenAI-compatible API-key configuration remains the credential surface; secrets are not duplicated into the runtime panel.

## DevExpress and safety

- Added a DevExpress provider-management workbench to the selected AI-guided setup profile using form-layout, spin-edit, combo-box, checkbox, button, grid, and host-path-explorer controls.
- Managed bind choices are intentionally constrained to `127.0.0.1` or `0.0.0.0`; arbitrary remote providers remain separately configured endpoints.
- Changing the managed local port synchronizes LocalGPT's matching provider endpoint.
- LocalGPT-wide default output/context token budgets remain separately editable from provider runtime context settings.
- Provider HTTP lifecycle actions are loopback-scoped; model/path values are omitted from diagnostics where appropriate.

## Preserved behavior

- 4.1.0 Matrix operator console, installer cancellation, shell/job/signal controls, and process ownership remain intact.
- Renderer-affine InteractiveServer continuations were not bulk changed.
- Council/provider qualification, Ollama multi-host routing, localization, rendezvous, installer, documentation, macOS signing/notarization, and native packaging contracts remain intact.
