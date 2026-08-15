# LocalGPT 2.9.3 — Benchmark Calibration and Seed Preservation

## Changes

- Reworked the maintained `adaptive-model-benchmark` seed into an Initial Hardware Calibration Benchmark intended for first-run use.
- Added an all-selected-member readiness round before benchmark measurement.
- Added `SystemBenchmarkCalibration`, a deterministic configured-workflow execution mode. LocalGPT itself now owns target coverage; a model can no longer replace an all-member request with representative sampling.
- The calibration engine attempts every distinct benchmark-capable provider-qualified selected Council member at four evenly spaced bounded profile points with early-stop disabled.
- Added measured Low, Middle, High and Expert hardware-spooler profile synthesis. Routes are created only from successful measured points; failed or unsupported members remain explicit coverage gaps.
- Added post-measurement Coverage Auditor, Performance Analyst and Profile Synthesizer rounds with the optional same-role peer usefulness/vote and role-result synthesis features enabled.
- Added a deterministic benchmark completion notice to the visible final handoff.
- Supplied Council team seeds are now preserved. Saving a supplied seed creates a user-owned literal copy instead of overwriting the maintained seed.
- Existing databases that contain a previously user-modified system-seed row are migrated losslessly: the edited content is preserved as a unique custom team and the maintained default is restored.
- First-run onboarding and the benchmark starter now recommend the calibration workflow after installation.
- Embedded provider benchmarks can stream progress through a parent Council without replacing the parent's live session.
- Wire protocol remains 2.1.1.

## Compatibility

Existing user-owned Council teams are not rewritten. Existing workflow execution modes remain unchanged. The new deterministic execution mode is additive and is used by the maintained benchmark seed.
