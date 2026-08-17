# LocalGPT 3.0.8 source validation

- Application versions: LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper set to 3.0.8.
- 1-Wire package version remains 2.1.1.
- `localgpt-reconnect.js` parses with `node --check` and its reviewed JavaScript SHA-256 manifest entry was refreshed.
- Interactive startup uses `ssr.disableDomPreservation = false`; no render-mode boundary was removed.
- Render-mode regression: explicit `@rendermode` file count remains 20, matching 3.0.7.
- Initial setup selected benchmark membership has no `.Take(128)` truncation and preferred curator membership has no `.Take(4)` truncation.
- Benchmark workflow text forbids model packs/quartets/representative sampling and describes the four sections/profile points as one measurement phase.
- Workflow execution freezes the exact provider-qualified target set before SystemBenchmarkCalibration and validates the returned requested-target count.
- CouncilBenchmarkCalibrationService validates that all benchmark-capable frozen SelectionKeys appear in the deterministic measurement report.
- The previously reported CS1572 XML documentation warning sources were removed without changing their constructors or service behavior.
- No dotnet restore/build/publish/pack was executed.
