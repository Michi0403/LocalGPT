# LocalGPT 3.1.2

LocalGPT 3.1.2 is the benchmark coverage truth-guard successor to 3.1.1. It keeps durable benchmark evidence, live evidence inspection and the existing Council architecture while making provider-qualified coverage counts and unresolved identities mechanically authoritative.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Coverage truth state

- attempted, successful and unresolved provider-qualified identities are derived from the benchmark report by one shared coverage helper;
- deterministic summaries include the exact unresolved identity list and the arithmetic invariant `attempted - successful = unresolved`;
- the benchmark audit UI exposes that same exact list;
- later Council reviewer prose is explicitly secondary and cannot override the deterministic identity set;
- quality-review prompts may no longer summarize all subjects as successful unless machine-derived coverage says so.

## Compatibility

No database migration or evidence archive schema migration is introduced. Existing 3.1.1 `BenchmarkEvidence` archives remain readable and receive machine-derived coverage truth when opened by this source version.

See `CHANGELOG-v3.1.2-BENCHMARK-COVERAGE-TRUTH-GUARD.md` and `VALIDATION-v3.1.2-source.md`.
