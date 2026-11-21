# LocalGPT 7.7.2 — Ollama provider authority and Council context allocation

Base: supplied LocalGPT 7.7.1 source ZIP. PublisherStudio 4.9.1 is unchanged.

## Fixed

- **No synthetic context failure:** `OllamaThinkingChatClient.CreateRequestAsync` keeps conservative reasoning compaction and unscoped tool-broker compaction but never returns `OllamaContextBudgetExceeded` solely because an estimated serialized-byte token count exceeds a threshold. The same correction applies to `SendRequestOnceAsync`, including requests altered by compatibility fallbacks.
- **The configured road reaches Ollama:** `/api/show` `model_info.*.context_length` remains a logged model capability, not an implicit clamp on explicit `num_ctx`. This addresses configurations such as `num_ctx=67072` with an advertised 16384 context; the HTTP provider now accepts or rejects the actual request. An unspecified `num_ctx` is left unspecified rather than forcing an assumed 4096 provider default.
- **No unverified output reduction:** estimated byte/token demand is diagnostic and can no longer silently shrink `num_predict`. Existing persisted maximum output policy still applies.
- **No heuristic context underallocation by default:** the former Council `fitContextToRequest` optimization derived a small `num_ctx` from approximate serialization bytes. A new database-owned `OllamaRequestBudgetPolicyJson.useEstimatedContextFitting` flag defaults to `false` for both seeded and previously saved policy rows. Setting it to `true` deliberately restores context allocation based on the existing growth-step estimate, up to the explicitly selected road ceiling; it still never causes a local rejection.
- **Provider HTTP errors retain provenance:** actual Ollama HTTP context rejections retain their HTTP status and error text; they do not trigger thinking/tool-schema compatibility probes, and Council does not repeat an identical failed member through the general safe-limit retry.
- **Regression guard:** expanded the existing build-wired PowerShell Council road policy guard to protect both pre-send locations, preserved `num_ctx`/output, explicit opt-in, and provider rejection handling.

## Remaining boundaries / runtime verification

- A supplied configured `num_ctx` may exceed a model_info context-length field. Ollama may reject, accept, internally truncate, or run with a higher context allocation depending on actual provider/model/runtime behavior. LocalGPT does not reinterpret acceptance as proof the full prompt was evaluated.
- Council still assembles large prompts from its configured workflow and native tools. This release **does not** claim a complete per-model workload decomposition planner. Provider-reported `prompt_eval_count` and `eval_count` are already logged; compare them with request estimates and requested road budgets under the actual Ollama installation to identify remaining allocation/truncation cases.
- Default-off context fitting changes the memory allocation envelope: a route set to 67072 now sends 67072 (not an estimated smaller value) to Ollama. Hardware scheduling remains governed by the existing hardware road and host concurrency controls. To opt into estimated-memory fitting, change the persisted budget policy, not code constants.
- No .NET build, PowerShell execution, or real Ollama HTTP test was performed in this environment. Source-only inspection and archive-integrity checks are not runtime verification.

## Longitudinal research idea (not implemented here)

Treat each LocalGPT source version as an immutable, hashed, versioned learning snapshot. Derive observations with file/line/version provenance; retain prior hypotheses alongside corrected findings and runtime evidence. Do not replace a verified result solely with a later model-generated explanation. This proposal is documentation only, not a new runtime ingestion feature.
