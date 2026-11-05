# LocalGPT 2.5.7 — Multi-Ollama host registry and Council route preflight

## Scope

This release fixes the provider-state regression exposed by running one AI Council across the local Ollama host and a second Ollama host on the LAN. In 2.5.5/2.5.6 discovery and Council identities were already endpoint-qualified, but selecting a discovered Ollama model in `/install` could still replace the single primary `OllamaCore` binding. That could leave a previously selected local Council route in saved Council state while the provider registry only retained the newly selected remote host.

## Fixed

### Adding a second Ollama no longer overwrites a different primary host

- `/install` now treats an Ollama provider as `provider kind + normalized endpoint` rather than treating the provider kind as a singleton slot.
- Selecting a model on the current primary endpoint updates the primary binding as before.
- Selecting a model on a different Ollama endpoint creates or updates an entry in `AICore.OllamaCores` instead of replacing `AICore.OllamaCore`.
- The same endpoint-qualified behavior is applied to additional local OpenAI-compatible / LM Studio hosts through `ChatGPTLocalCores`.
- `appsettings.json` now exposes `OllamaCores: []` explicitly as the additional Ollama host registry.
- Additional local provider cards can be deliberately promoted with `Make primary`; the previous primary is preserved as an additional binding instead of being discarded.
- Configured badges and host removal compare canonical provider endpoints (`localhost` and `127.0.0.1`, plus OpenAI-compatible `/v1` normalization) instead of raw strings.

### Local Ollama remains discoverable after a remote host becomes primary

- Provider runtime discovery probes every configured Ollama endpoint once.
- The historical local Ollama endpoint `http://127.0.0.1:11434` remains a discovery candidate even when the configured primary Ollama is a LAN endpoint.
- Discovered remote Ollama candidates now derive `IsLocal` from the endpoint instead of being marked local unconditionally.
- OpenAI-compatible discovery suppression is based only on configured native-Ollama authorities, so fallback discovery does not silently erase a deliberately configured OpenAI-compatible route on the same authority.
- The adaptive benchmark loopback path no longer inherits a remote primary Ollama endpoint; it uses an actually configured loopback Ollama when one exists and otherwise the maintained loopback default.

### Council runs preflight exact provider-qualified routes

- Council participant selection refreshes the current provider catalog once before a run and resolves selected identities by exact selection key.
- A reachable provider host that no longer exposes the selected model is treated as a stale route and is rejected before the Council starts.
- A deliberately configured host that is temporarily offline may retain its exact selected route so the real provider call can report reachability; it is never remapped to a same-name model on another endpoint.
- Stale provider-qualified selections produce a clear error telling the user to refresh/reconfigure the exact host.
- No same-name provider fallback is performed for provider-qualified selections.
- Freshly preflighted references are reinserted into the runtime reference cache so a 20+ member Council does not repeat full provider discovery for each participant.

### Chat reconciles stale selections without silently replacing them

- Provider-model refresh reconciles exact provider-qualified Council selections against the refreshed host/model catalog.
- Missing qualified routes are deselected with a visible explanation.
- If stale qualified selections were removed and no selections remain, Chat does **not** silently auto-select an unrelated default model during that refresh.
- Legacy bare model names are only upgraded automatically when exactly one current provider route owns that name; ambiguity remains an explicit error instead of an endpoint guess.
- Provider status now reports distinct provider hosts (`provider kind + endpoint`) rather than collapsing multiple Ollamas into one provider count.

## Preserved

- Existing `AICore.OllamaCore` and `ChatGPTLocalCore` remain the explicit primary/default bindings for compatibility and normal single-model selection.
- `OllamaCores` / `ChatGPTLocalCores` remain additive and configuration-file compatible; no destructive configuration migration is performed.
- Existing provider-qualified selection keys, hardware roads, Council teams, benchmark wiring, session persistence and 1-Wire contracts remain intact.
- Existing logging and service resilience boundaries are preserved.
- `ConfigureAwait(false)` remains the default continuation policy; renderer-affine `ConfigureAwait(true)` sites were not expanded.
- `@rendermode InteractiveServer` was not removed from any maintained page.
- LocalGPT 1-Wire protocol version remains unchanged.

## Version

- LocalGPT: `2.5.7`
- LocalGPTInstallerConsole: `2.5.7`
- LocalGPTWebviewWrapper: `2.5.7`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)

## Build boundary

Per the delivery constraint, this source package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Validation is source/static only and is recorded in `VALIDATION-v2.5.7-source.md`.
