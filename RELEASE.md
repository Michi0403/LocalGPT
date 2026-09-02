# LocalGPT 3.6.4

LocalGPT 3.6.4 is a focused build-repair release for the 3.6.3 macOS startup work.

The 3.6.3 source converted three expensive startup workers to `BackgroundService` and inserted an initial asynchronous hand-off so Kestrel could begin listening before database migration, runtime-capability synchronization, and DX AI-function catalog synchronization completed. The repository's own zero-tolerance async-continuation policy correctly rejected the bare `await Task.Yield()` statements, so the release stopped during the RID-neutral LocalGPT build before packaging.

3.6.4 preserves the intended startup behavior while using a cancellable, policy-compliant `Task.Delay(1, stoppingToken).ConfigureAwait(false)` hand-off in all three workers. The same async-continuation audit that failed 3.6.3 is included in the 3.6.4 source validation.

All other 3.6.3 fixes remain in place: dynamic macOS loopback-port selection, runtime endpoint discovery, `/health` readiness probing, visible Terminal diagnostics, stale-process cleanup, user-data permission repair, Ollama/LM Studio helper, and macOS native-architecture validation.

See `CHANGELOG-v3.6.4-ASYNC-CONTINUATION-BUILD-GUARD-REPAIR.md` and `VALIDATION-v3.6.4-source.md`.
