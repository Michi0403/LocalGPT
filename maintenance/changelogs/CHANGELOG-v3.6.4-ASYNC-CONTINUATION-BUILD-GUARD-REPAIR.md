# LocalGPT 3.6.4 — async-continuation build-guard repair

- Fixes the 3.6.3 release build regression where the repository's mandatory async-continuation audit rejected the three new `await Task.Yield()` startup hand-off statements before the RID-neutral documentation build could complete.
- Keeps the intended non-blocking `BackgroundService` startup behavior from 3.6.3, but replaces each bare `Task.Yield()` with a one-millisecond cancellable asynchronous hand-off using `Task.Delay(1, stoppingToken).ConfigureAwait(false)` so Kestrel startup is still released before the long initialization work begins.
- Applies the repair to database initialization, runtime-capability synchronization, and DX AI-function catalog synchronization.
- Leaves the 3.6.3 dynamic macOS port selection, `/health` readiness probe, stale-process cleanup, visible Terminal diagnostics, native-architecture guard, user-data permission handling, and Ollama/LM Studio helper unchanged.
- Strengthens the version-specific source audit so it requires the continuation-policy-compliant startup hand-off and executes the same syntax-aware async-continuation audit that blocked the user's 3.6.3 build.
- Version advanced from 3.6.3 to 3.6.4.
