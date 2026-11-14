# LocalGPT 7.1.4 — Preferred Council models and model-selection audit

## Scope

- Data Management → Council & models now contains a provider-qualified preferred-model editor. It discovers available models, retains stale saved preferences visibly, lets the user select/unselect models, reorder their priority, refresh discovery, and save the durable selection in `AICore.PreferredCouncilModelKeys` within the existing per-user configuration.
- The preference pool does **not** change provider-host settings, passwords, Ollama installation, saved Council-team assignments, or explicit per-run selections. Preferences influence **automatic fallback** only.
- Automatic model selection in Council and Goal Execution orders currently eligible models by saved preference priority, then preserves the existing discovery/load fallback order. Team/explicit bindings remain authoritative, including their missing-model diagnostics.
- The initial Chat and Model Council UI defaults consult the preferred pool **only when no selection has already been made**. Chat falls back to its previous default if no preferred installed model is available; Model Council retains its previous two-candidate fallback.
- Goal semantic planning permits user-preferred installed models that are not separately marked as host-configured. A chosen semantic planner route and its selection reason are logged.
- Every running Council writes `## Model Selection Audit` to its durable CouncilLogs Markdown, reporting provider-qualified model identities, selection provenance (caller/goal, saved team assignment, user preference, or discovery fallback), preference rank when present, and current configuration/availability metadata. The structured application logger writes the run ID and the chosen model route/reason without logging prompt contents.
- Saving Install / provider settings preserves the preference pool owned by Data Management; an older Install UI draft does not overwrite it.

## Changes explicitly not made

- No changes to installer/bootstrap, package generation, TLS/HTTPS, MCP, 1-Wire transport, MFA, Ollama inference, database schema, user data, Council role permissions, or PublisherStudio.
- LocalGPT version: **7.1.4**. Installer version: **5.0.3**, unchanged.

## Important boundaries

- The preferred pool is **soft priority**, not a new authorization policy or an explicit team-role replacement. It cannot make a disconnected model usable. Already explicitly selected model routes and saved team bindings are never silently rewritten.
- The Council log identifies the recorded **selection source**; it does not claim a benchmark-derived optimality score or a model quality recommendation.
- Real runtime tests on Windows, macOS and Linux have not been performed in the source packaging environment.
