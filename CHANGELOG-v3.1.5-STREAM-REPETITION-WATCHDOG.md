# LocalGPT 3.1.5 — provider stream repetition watchdog

LocalGPT 3.1.5 is a forward-only runtime-resilience release built on 3.1.4. It preserves the complete XML documentation pass, benchmark evidence archive, machine-derived coverage truth, Council round-member recovery, expected cancellation handling, live-Council rendering improvements, provider-qualified identity rules, SQLite/EF migration state and 1-Wire protocol behavior.

## Problem fixed

A provider can remain technically alive while generating the same short text cycle indefinitely. In that state normal HTTP activity continues, so connection-health checks do not fail and the request can occupy a sequential AI-host road until the much larger per-call timeout. During the all-model benchmark this can block every later model queued on that physical/provider host even though LocalGPT itself remains responsive.

The observed failure shape was a Benchmark Subject repeatedly streaming a phrase equivalent to `loop is the loop ...` for a long period while later subjects on that host remained queued. This is distinct from a dead socket, provider crash, ordinary slow reasoning, or caller cancellation.

## Conservative stream watchdog

`ProviderStreamRepetitionWatchdog` now monitors only provider-generated text while a model is actively producing output.

The detector deliberately does **not** use broad vocabulary-diversity or repeated-word heuristics. It requires all of the following before one request is classified as runaway:

- at least 1,024 generated characters;
- at least four seconds of active generation before sampling starts;
- at least 72 normalized letter/digit tokens in the analyzed tail;
- a short periodic token cycle of at most 32 tokens;
- at least six complete cycles in the sampled tail;
- at least 97% periodic token agreement;
- four consecutive suspicious samples, spaced two seconds apart;
- at least six seconds of sustained suspicious state.

The watchdog uses a bounded 12,288-character rolling window. It starts its `Stopwatch` on the first substantive generated fragment and performs analysis only as new provider fragments arrive. There is no polling thread, no background timer task and no extra provider request. A silent/stalled provider remains governed by the existing request timeout instead of being confused with repetitive generation.

## Request-scoped cancellation only

When repetition is confirmed, LocalGPT cancels/disposes only the current provider request. It does not stop the LocalGPT process, kill Ollama, kill `llama-server`, cancel the Council, cancel unrelated host roads, or mutate provider configuration.

The partial repetitive stream remains visible/auditable as provider evidence. Exception text deliberately omits the repeated model content so normal diagnostics continue to avoid duplicating provider payloads into logs.

## Benchmark recovery

Provider benchmark measurements now have `RepetitionRecoveryAttempts`.

For the configured `SystemBenchmarkCalibration` social-team round, this count is mechanically derived from the existing round `MemberFailureRecoveryAttempts` setting. If `MemberFailureRecoveryMode` is disabled, watchdog retries are also disabled. Otherwise the benchmark reuses the configured bounded attempt count, clamped to zero through eight.

A benchmark must preserve exact provider-qualified subject identity, so a repetition retry always reruns the **same endpoint/model/profile/task**. Substituting another model would falsify the measurement. Every aborted attempt remains in the provider trace with an explicit watchdog marker and increments the real provider-call `AttemptCount`.

If the repetition recovery counter is exhausted, the task is retained as an explicit failed measurement and the benchmark continues to later tasks/profiles/subjects rather than holding the host queue until the full timeout.

Standalone provider benchmarks default to one repetition recovery attempt unless their caller explicitly configures another bounded value.

## Council recovery integration

The same conservative watchdog is applied to normal Council participant streams, corrective role-retry streams and final-answer recovery streams.

A watchdog failure enters the existing Council failure path. Therefore the already-maintained recovery behavior remains authoritative:

1. the failed partial stream is retained;
2. the existing safe same-member retry may run;
3. if required configured member work is still unresolved, the 3.1.3 round-member recovery logic uses the Social Team recovery mode, attempt count and eligible role-member pool;
4. the round is not silently dropped or fabricated.

No parallel replacement scheduler was added.

## Compatibility

- LocalGPT / installer console / webview wrapper version: 3.1.5.
- .NET SDK policy remains 10.0.400 / `net10.0`.
- DevExpress remains on the existing 25.2 package lane.
- 1-Wire protocol remains 2.1.1.
- BenchmarkEvidence JSON schema remains version 1.
- No EF Core migration source or database migration compatibility source changed.
- No existing 3.1.4 XML documentation coverage was removed.

## Validation boundary

This archive is source-only. The preparation environment does not contain the .NET/DevExpress build toolchain, so no successful compilation or runtime execution is claimed. Applicable repository static audits and package re-validation are recorded in `VALIDATION-v3.1.5-source.md` and the external validation log.
