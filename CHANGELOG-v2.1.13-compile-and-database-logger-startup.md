# LocalGPT 2.1.13 — Compile recovery and database-logger startup isolation

## Fixed

- Changes `RunRemoteKnowledgeAsync` from a direct `Task` return to an `async Task` method that awaits the configured `RunUiActionAsync` awaitable.
- Resolves `CS0029`: `ConfiguredTaskAwaitable` is no longer returned where `Task` is required.
- Restores production of `LocalGPT.dll`; this removes the downstream `WMC1006` and `CS0006` failures in `LocalGPTWebviewWrapper` that occurred only because the referenced LocalGPT project had failed first.
- Adds a one-way database-logger readiness gate. Startup diagnostics remain in the logger's bounded channel until database health checks, migration, and deterministic seed saves have completed.
- Prevents the database logger worker from racing `ApplicationLogs` creation or competing for SQLite write access during startup `SaveChangesAsync` operations.
- Cancels the dormant logger worker cleanly during shutdown when database initialization never reaches the ready state, avoiding a final write attempt against an unavailable schema.

## Preserved

- Startup diagnostics are still available through the configured console, debug, file, or email providers while database persistence is gated.
- Once initialization succeeds, queued database log entries are flushed through the existing bounded batching path.
- Remote source preview/import behavior, approval checks, bounded file limits, role/topic parsing, result serialization, and operational diagnostics remain unchanged.
- The adaptive Ollama benchmark/autotune implementation remains present and unchanged in this patch release.
- The separately maintained `LocalGPT.WireProtocolVersion` package remains at `2.1.0`; installer and desktop-wrapper package versions remain independently versioned.

## Versioning

- LocalGPT application and organic 1-Wire application advertisement: `2.1.13`.
- Source-package sequence: `LocalGpt(35)`.

## Validation status

- The owner reported a successful full Debug rebuild after applying the task-return correction: 4 projects succeeded, 0 failed.
- Static repository validation was run after adding the database-logger readiness gate.
- The final owner-side rebuild and startup run remain authoritative for the logging change because this environment has no .NET 10 SDK, Windows workload, licensed DevExpress targets, EF runtime, or SQLite workload matching the application host.
