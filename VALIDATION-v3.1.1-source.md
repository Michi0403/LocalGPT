# LocalGPT 3.1.1 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.1;
- the prior 3.1.0 benchmark streaming/evidence/responsiveness changes remain present;
- benchmark task results retain bounded assignment/provider-trace/final-answer projections plus a durable task evidence artifact identifier;
- complete captured task assignment, provider stream and scored final answer are written to LocalGPT user-data JSON artifacts and loaded only on explicit request;
- benchmark report archives are written atomically and can be enumerated/reloaded after the in-memory component report is gone;
- the Benchmark Council history UI renders one stored target at a time and does not eagerly materialize every archived full stream;
- deterministic measurement counts are presented separately from Council reviewer prose;
- persistence failure is non-fatal to benchmark completion;
- no EF migration or database model change was added;
- .NET 10 / DevExpress 25.2-line package state is carried forward;
- 1-Wire protocol remains 2.1.1;
- async continuation, service resilience, application architecture and release-source audits are run separately in the packaging validation log.
