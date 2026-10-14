# LocalGPT 3.1.1 — durable benchmark audit evidence

## Scope

LocalGPT 3.1.1 is a forward-only follow-up to the supplied 3.1.0 .NET 10 / DevExpress 25.2 source and the benchmark-evidence responsiveness patch. It does not revert benchmark, Council, provider-routing, database, migration, DevExpress, .NET 10, or 1-Wire work.

The release closes the remaining auditability gap discovered during the long all-model calibration: 3.1.0 made benchmark task evidence inspectable while the benchmark report remained in memory, but the benchmark report itself was not a durable history that could be reopened after a later page/process lifecycle.

## What was already fixed in the previous 3.1.0 patched source

The prior patched source already carries forward these behaviors:

- provider benchmark measurements use streaming provider responses;
- each measured task retains its assignment, bounded provider trace/exposed reasoning and bounded scored final answer;
- single-model and Benchmark Council result displays expose an **inspect evidence** control;
- long provider/Council output uses bounded render projections so the browser is not forced to eagerly Markdown-render every historical stream;
- Ollama whitespace-only thinking fragments are preserved so streaming output does not join text such as `from0` or `is5` merely because a standalone space token was discarded.

Those fixes remain intact in 3.1.1.

## Durable full-fidelity benchmark evidence

Every measured benchmark task now writes a separate LocalGPT-owned evidence artifact beneath the current user's local application-data directory:

`LocalGPT/BenchmarkEvidence/<run-id>/task-*.json`

The task artifact preserves the complete captured values available to LocalGPT at measurement time:

- exact benchmark assignment;
- complete provider stream captured by the benchmark, including provider-exposed reasoning/status/function trace;
- complete visible final answer used by deterministic scoring;
- provider attempt count, quality score, throughput, elapsed time, success state and error state;
- exact provider-qualified model identity and measured profile.

The in-memory report still keeps bounded head/tail projections. This is intentional: complete 262k-class streams must not be inserted into every recurrent Blazor render. A developer can explicitly load the full durable artifact from the task evidence card when needed.

A runtime/model that does not expose hidden reasoning still cannot be forced to reveal it. The archive stores what the provider actually exposed to LocalGPT rather than inventing a reasoning trace.

## Durable benchmark report history

Completed and gracefully cancelled/failed benchmark reports are written atomically to:

`LocalGPT/BenchmarkEvidence/<run-id>/report.json`

The stored report contains measurement tables, recommendations, failures, bounded UI evidence projections and references to the full per-task artifacts. Saving a hardware performance preset or applying a benchmark recommendation refreshes the report archive so the persisted run also reflects those later user-approved actions.

No database migration is introduced. Benchmark audit history is deliberately file-backed under LocalGPT user data so the existing SQLite schema and supplied migration tree remain unchanged.

## Benchmark history UI

The Benchmark Council panel now includes **Saved benchmark audit evidence**. It can:

- enumerate recent locally stored benchmark runs without loading every large report;
- reopen a selected report after navigation/restart;
- show deterministic attempted/successful/provider-call counts separately from Council prose;
- select one provider-qualified target at a time to keep the DOM bounded;
- inspect each measured profile/task;
- explicitly load the complete archived raw task evidence only when requested.

Current single-model and Benchmark Council results also show the deterministic audit summary. Reviewer text is labeled as secondary interpretation; it cannot visually replace failed/inconclusive measurement counts.

## Responsiveness and safety boundaries

Full task evidence is written asynchronously and atomically through a temporary file followed by replacement. Evidence-write failure is logged as a warning and never converts an otherwise completed benchmark into a failed benchmark.

Service-side evidence I/O continues to use non-capturing continuations. Full archived streams are not Markdown-rendered automatically; the existing bounded formatted projection remains the normal view and the complete raw archive is a deliberate developer action.

Local evidence artifact identifiers are validated before loading and are resolved only beneath the LocalGPT benchmark evidence root.

## Versioning

The application, installer console and WebView wrapper move from 3.1.0 to **3.1.1**. The 1-Wire protocol remains **2.1.1**.

## Build status

This release archive is source-only and was prepared without a .NET/DevExpress runtime in the packaging environment. Static repository audits are included and should be followed by the normal LocalGPT .NET 10 / DevExpress build on the development machine.
