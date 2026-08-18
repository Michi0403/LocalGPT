# LocalGPT 3.1.1

LocalGPT 3.1.1 is the durable benchmark-audit successor to 3.1.0. It keeps the supplied .NET 10 / DevExpress 25.2 source and the existing benchmark/Council architecture, while making benchmark evidence both inspectable during a run and reopenable after later navigation or process restarts.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Benchmark audit state

- streaming provider evidence remains visible through per-task **inspect evidence** cards;
- bounded render projections protect long-running Blazor pages;
- full captured task evidence is stored under the user's LocalGPT `BenchmarkEvidence` directory and loaded only on demand;
- completed/gracefully stopped benchmark reports are stored as versioned JSON archives and can be reopened from **Saved benchmark audit evidence**;
- deterministic success/failure/provider-call counts are shown separately from Council reviewer interpretation.

## Database boundary

No migration or schema change is introduced by this release. Durable benchmark audit evidence is file-backed under LocalGPT user data.

See `CHANGELOG-v3.1.1-BENCHMARK-AUDIT-EVIDENCE.md` and `VALIDATION-v3.1.1-source.md`.
