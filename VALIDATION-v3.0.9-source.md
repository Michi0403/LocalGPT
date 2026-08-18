# LocalGPT 3.0.9 source validation

## Version and protocol

- `src/LocalGPT/LocalGPT.csproj`: 3.0.9.
- `src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj`: 3.0.9.
- `src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj`: 3.0.9.
- CanIRun.ai attribution user agent updated to LocalGPT/3.0.9.
- `LocalGPT.WireProtocolVersion` remains 2.1.1.

## Reported build-guard failure

- `App.razor` contains the reviewed reconnect contract `ssr: { disableDomPreservation: false }`.
- `Assert-OperationalDiagnostics.ps1` now requires `disableDomPreservation:\s*false` and no longer requires `true`.
- `Assert-InteractiveServerRenderModes.ps1` now requires `ssr: { disableDomPreservation: false }` and no longer requires `true`.
- Explicit `@rendermode` file comparison against the supplied 3.0.8 source: 20 files in each version, same paths.

## Benchmark contract

- Maintained initial calibration profile count: 5.
- Maintained profile names: Low, Normal, High, Expert, Max.
- Exact provider-qualified selected membership remains frozen for deterministic measurement.
- Initial setup benchmark-team creation still has no `.Take(128)` selected-model truncation and no `.Take(4)` preferred-curator truncation.
- Initial calibration requests catalog-backed minimum context/output values and the catalog-backed maximum context value.
- Failure-based early stop is disabled for the maintained initial calibration; generic callers retain a configurable stop count.
- Provider benchmark maximum context/output clamps are catalog-backed.
- One consolidated four-section benchmark task is executed in one provider turn per profile point.
- Actual provider-call attempt count is retained on each task result and included in lane/summary provenance.
- Per-target unexpected benchmark-engine failures are retained as explicit failed target evidence and do not terminate the remaining host queue.
- Coverage validation still rejects a report that omits a frozen benchmark-capable SelectionKey.
- Exact-tier persistence does not interpolate a lower successful point into a missing higher named tier.

## Live UI and human wait

- CouncilBenchmarkCalibrationService uses the existing `BeginParticipantActivity`, `SetParticipantActivityStatus`, `AppendParticipantActivity`, `SetParticipantActivityResult` and `CompleteParticipantActivity` APIs for per-subject live lanes.
- Per-subject streamed progress is routed to the matching live lane rather than replacing the ordered transcript with a large benchmark dump.
- Blocking human collaboration uses an indefinite wait loop; the 30-second delay is only a recheck interval.
- The live session is touched while waiting and reports that there is no inactivity timeout.

## Static checks run

- `python build/audit_release_3_0_9.py`: passed 74 release-specific checks.
- `python build/audit_application_architecture.py --root . --product localgpt --mode all`: passed.
- `python build/audit_service_resilience.py --root . --product localgpt`: passed; 2088 reviewed service methods own try/catch + diagnostics under the maintained policy.
- `python build/audit_async_continuations.py --source-root src/LocalGPT`: passed.
- `python build/audit_provider_qualified_council.py --root .`: passed 282 checks.
- `python build/audit_chat_ascii_console.py --root .`: passed 17 checks.
- `python build/audit_documentation_onewire_contracts.py`: passed.
- `python build/audit_configurable_behavior_policy.py`: passed.
- `python build/Assert-XmlDocumentationCoverage.py .`: passed for 8748 direct C# declarations across 575 maintained source files.
- Maintained JavaScript SHA-256 manifest was independently recomputed with CRLF/CR normalization and matched all 24 maintained files.
- `node --check` passed for all 24 maintained JavaScript files in the diagnostics manifest.
- Delimiter-balance source check passed for all C# files changed in 3.0.9.
- No repository-local `bin` or `obj` output is included in the source package.

## Not executed

- No `dotnet restore`, `dotnet build`, `dotnet publish`, `dotnet pack`, MSBuild, or runtime launch was executed.
- No GitHub or online repository access was used.
- The user's build remains the authoritative compiler/runtime verification for this source-only handoff.
