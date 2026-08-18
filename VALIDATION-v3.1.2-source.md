# LocalGPT 3.1.2 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.2;
- 3.1.1 durable benchmark evidence persistence and on-demand evidence inspection remain present;
- benchmark coverage is derived by `ProviderModelBenchmarkCoverageSnapshot` from provider-qualified report targets only;
- deterministic Council calibration uses that same successful-recommendation rule for its successful and unresolved counts;
- coverage summary generation validates attempted/successful/unresolved arithmetic and exact unresolved identities before rendering;
- the deterministic transcript emits a machine-derived coverage invariant before the large evidence matrix;
- the UI exposes the exact unresolved provider-qualified identity list and arithmetic invariant;
- maintained Council reviewer prompts explicitly subordinate reviewer prose to deterministic coverage state;
- no EF migration or database model change was added;
- existing BenchmarkEvidence JSON schema remains unchanged and prior 3.1.1 archives remain compatible;
- .NET 10 / DevExpress 25.2-line package state is carried forward;
- 1-Wire protocol remains 2.1.1.
