# LocalGPT 3.1.2 — benchmark coverage truth guard

LocalGPT 3.1.2 is a forward-only follow-up to 3.1.1. It keeps the durable benchmark evidence archive, streaming evidence inspection, Blazor responsiveness work, .NET 10 / DevExpress 25.2 lane, database boundary and 1-Wire protocol unchanged while closing a coverage-accounting trust gap found in a real 94-model run.

## Why this release exists

A completed benchmark reported 94 provider-qualified targets attempted and 84 with a successful measured recommendation, while later Coverage Auditor prose listed only four unresolved identities. The arithmetic and identity set were therefore inconsistent: 94 - 84 requires 10 unresolved attempted identities. The deterministic measurement matrix was still the primary evidence, but the later AI restatement could mislead a human reviewer.

## Machine-derived coverage truth

- Adds `ProviderModelBenchmarkCoverageSnapshot`, which derives attempted, successful and unresolved provider-qualified identity sets only from the benchmark report.
- Uses the same successful-measured-recommendation rule as deterministic Council calibration instead of letting UI components invent a different success definition.
- Council calibration now fails fast if its benchmark-capable attempted count disagrees with the mechanically derived report count or if coverage arithmetic is inconsistent.
- `CouncilBenchmarkCalibrationResult` retains the exact unresolved provider-qualified selection keys so summary generation cannot silently shrink the set.

## Deterministic summary truth guard

The benchmark transcript now places a `Machine-derived coverage invariant` near the top of the deterministic summary, before large evidence tables. It contains:

- attempted target count;
- successful measured recommendation count;
- unresolved attempted count;
- the explicit arithmetic check `attempted - successful = unresolved`;
- every authoritative unresolved provider-qualified identity;
- a truth-guard notice that conflicting later AI prose is incorrect.

This placement also makes the invariant less likely to disappear from later model context when a very large measurement matrix is truncated.

## UI auditability

The deterministic benchmark audit panel now shows the same arithmetic invariant and provides an expandable list of the exact unresolved provider-qualified identities. Council reviewer prose remains explicitly secondary interpretation.

## Council reviewer guardrails

The maintained benchmark workflow now tells later roles to:

- copy the machine-derived coverage counts and identities exactly;
- verify the arithmetic before reporting coverage;
- never substitute examples, one-host subsets or remembered identities for the deterministic set;
- never say all benchmark subjects succeeded/high-quality unless deterministic coverage actually says all succeeded;
- correct reviewer prose when it conflicts with measurement evidence.

## Compatibility

- No benchmark feature is removed.
- No database schema or EF migration is changed.
- Existing 3.1.1 durable `BenchmarkEvidence` archives remain readable.
- No evidence schema migration is required because coverage truth is derived from the existing report fields.
- .NET SDK policy remains 10.0.400 / net10.0.
- DevExpress remains on the existing 25.2.* package lane.
- 1-Wire protocol remains 2.1.1.

## Validation boundary

This source package is not compiled in the preparation environment because no .NET SDK, MSBuild or DevExpress build toolchain is available there. Static release, architecture, async-continuation and service-resilience audits are used where available and are recorded separately.
