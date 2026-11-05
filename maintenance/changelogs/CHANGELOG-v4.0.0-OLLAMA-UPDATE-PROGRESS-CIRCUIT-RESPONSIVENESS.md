# LocalGPT 4.0.0 — Ollama update, progress, and setup responsiveness

## Why this release exists

Live 3.9.9 setup testing confirmed that hardware-fit discovery and provider-model linking were working, but exposed three follow-up problems in the last mile: some newly discovered Ollama models require a newer Ollama runtime, terminal cleanup removed too much of the useful live pull progression, and a setup/catalog workflow could spend long periods repeating provider discovery while the InteractiveServer frontend was vulnerable to a temporary circuit loss.

## Changes

### Ollama runtime update is now an explicit provider capability

- `AiProviderBootstrapProfile` now supports a knowledge-backed `updateCommand`.
- The provider bootstrap service exposes a user-confirmed `UpdateAsync` action. Existing customized profiles remain compatible: when `updateCommand` is empty, LocalGPT uses the already reviewed install command as the update fallback.
- Maintained Windows, Linux, and macOS Ollama profiles define the provider update command alongside installation.
- The setup assistant exposes **Update Ollama (confirmed)**, and the same action is available through the setup controller and DXFunction surface.
- If Ollama was running, the guided updater stops it before the update, re-detects the executable afterward, and starts it again after success. A runtime that was already stopped remains stopped.
- A failed update attempts to restore the previously running Ollama process instead of leaving it down without a recovery attempt.

### Model pulls recognize an old Ollama runtime

- Failed Ollama model pulls are inspected only for the provider's bounded compatibility response that explicitly says a newer Ollama version is required.
- When that response is detected, the setup assistant raises a focused warning and points to the separate confirmed updater action.
- Model-install confirmation is deliberately **not** treated as permission to update the runtime. The user confirms the runtime update separately, then retries the model installation.
- Failed model pulls no longer trigger an immediate full provider/model refresh, avoiding another expensive discovery round before the user can act on the real error.

### Useful pull percentages survive terminal cleanup

- Redirected stdout/stderr is now consumed as raw UTF-8 character streams rather than line-only events so carriage-return/cursor rewrite progress can be observed before process exit.
- ANSI/OSC cursor controls, spinner frames, and terminal decoration remain filtered from the shared ASCII console.
- Frames containing bounded numeric percentages (`0%` through `100%`) are retained as useful progress snapshots even when the provider rewrites one terminal line repeatedly.
- Percentage snapshots are deduplicated by their stable progress prefix (for example an Ollama layer identifier), so animation/speed changes at the same percentage do not flood the feed while actual percentage movement remains visible. Pending cursor-animation history is compacted so a long pull does not accumulate the raw animation stream.
- Setup-panel console rendering is coalesced to a bounded worker cadence instead of requesting a Blazor render for every terminal rewrite; a pending final update cannot be lost when another frame arrives during the delay.

### Setup catalog/provider work is less circuit-hostile

- One fresh setup snapshot still performs provider discovery, but the same scoped provider-candidate snapshot is reused by model-choice construction and an explicit provider-catalog search instead of immediately repeating the full provider scan.
- The live log that motivated this change showed provider-candidate calls taking about ten seconds each; avoiding duplicate scans keeps the UI operation substantially shorter without making provider state permanently stale.
- Disconnected InteractiveServer circuits are retained for two minutes instead of thirty seconds, matching the existing generous local SignalR timeout posture and giving temporary browser/frontend interruptions more room to reconnect.
- Render-time Ollama classification is now a local pure check instead of invoking the diagnostics-decorated provider service on every progress render. The live test showed this classifier accumulating hundreds of calls during one pull; removing that hot-path service call reduces avoidable circuit and diagnostics pressure.
- Existing browser-owned reconnect/reload controls remain in place; this release does not introduce competing reconnect loops.

## Preserved behavior

- Provider model links, CanIRun.ai attribution/scores, conservative provider-ID matching, and install buttons from 3.9.9 are retained.
- Council/team recovery and self-cleaning behavior is intentionally unchanged.
- Routed render-mode ownership is unchanged: normal routed pages keep `@rendermode InteractiveServer`; the intentionally static error page stays static. The setup child component continues to inherit its routed page circuit rather than creating a nested render boundary.
- Existing code style, local logging/privacy constraints, cancellation behavior, localization catalogs, and database-backed operator policy remain intact.

## Validation boundary

This handoff is source/static validated only. No GitHub access and no `dotnet build`, `dotnet test`, `dotnet publish`, runtime launch, installer build, or documentation regeneration were performed in this environment.
