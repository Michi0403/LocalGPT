# LocalGPT 4.2.3 — macOS UI/runtime recovery

## Browser and WindowServer pressure reduction

- Isolated the shared setup command feed into its own Razor component and coalesced terminal progress rendering to at most four UI snapshots per second.
- Coalesced the Chat ASCII console's shared-process output refreshes instead of scheduling a renderer update for every terminal event.
- Preserved full keyboard/gamepad control while making the ASCII gamepad `requestAnimationFrame` loop demand-driven: it runs only while game input is enabled, the browser window is focused/visible, and a controller is connected; it resumes from focus, visibility and gamepad connection transitions and tears down with the component. This keeps controller/Steam Deck scenarios supported without an idle compositor loop.
- Removed the dynamic `cursor: wait` request from disabled ASCII controls so frequent component updates do not gratuitously force native cursor-surface changes.

The supplied macOS crash report terminated WindowServer on `com.apple.coreanimation.cursor.primary` inside SkyLight/QuartzCore cursor-surface handling and attributed the proximate/originating client work to Microsoft Edge. These changes reduce LocalGPT's browser/compositor churn; they do not claim to repair Apple's WindowServer implementation.

## Runtime identity and Council scheduling

- Added a protocol-independent provider runtime authority key based on normalized host plus effective port.
- Council execution/concurrency lanes now use that runtime key. Native Ollama and an OpenAI-compatible facade on the same authority no longer imply independent hardware, while separate runtimes on different ports no longer collapse merely because they share a host.
- Preserved the 4.2.2 model-level Ollama `/v1` canonicalization and legacy binding reconciliation.

## Ollama crash recovery

- Ollama status/start/restart now distinguish a visible process from a responsive local provider API by probing `/api/tags` with a short bounded timeout.
- An explicit Start can recycle an Ollama-named process that exists but does not answer the configured local API, avoiding the former false "already running" state after a provider/system crash.
- Startup/restart success is reported only when the process is visible and the local API becomes responsive; stale/unresponsive state remains visible in the UI.

## Optional HTTPS catalog resilience

- Kept platform/default certificate validation unchanged. No permissive certificate callback was introduced.
- A provider-catalog `HttpRequestException` (including the observed AppleCrypto TLS failure) is converted into one bounded warning and a maintained offline alias fallback rather than four propagated full-stack diagnostics.
- Per-family online expansion failures remain partial failures: already-discovered provider results stay usable.

## Preserved behavior

- Single-model Ollama and OpenAI-compatible chat paths remain independent protocol choices.
- Generic OpenAI-compatible providers remain supported.
- Existing 4.2.2 provider identity reconciliation, 4.2.0 live catalog behavior, 4.1.9 packaging repair and prior release protections remain in place.

- Gamepad polling remains available for controller/Steam Deck workflows, but only while the game surface is active, focused, visible, enabled, and a controller is connected; unchanged controller frames no longer rewrite pressed-state DOM classes.
