# LocalGPT 2.9.4 source validation

This release is validated source-only in the packaging environment. No `dotnet`, MSBuild or Visual Studio build is executed here; the Windows build remains authoritative.

Targeted validation covers:

- LocalGPT application/project versions at 2.9.4 while `LocalGptWireProtocolVersion` remains 2.1.1.
- Current workflow role task authority over the background user request and prior transcript evidence.
- Initial Hardware Calibration Benchmark ordering: inventory → task design → per-member task execution → deterministic measurement → quality curation → coverage review → performance review → profile synthesis.
- Removal of the readiness-only benchmark round.
- Four maintained benchmark task categories and `MaxTasks = 4` across four evenly spaced profile points.
- Seed version 22 and supplied-default preservation/custom-copy behavior.
- Existing Council role coordination, provider-qualified addressing, X-Rounds, reasoning/function trace, rejoin, async, resilience, architecture, documentation and InteractiveServer invariants.
