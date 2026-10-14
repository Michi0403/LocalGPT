# LocalGPT 2.9.9 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. A Windows build remains the authoritative compile/runtime validation.

## Passed source audits

- `build/audit_release_2_8_5.py` through `build/audit_release_2_9_9.py`
- `build/audit_configurable_behavior_policy.py`
- `build/audit_provider_qualified_council.py --root .` — 280 checks
- `build/audit_xround_wiring.py`
- `build/audit_benchmark_rejoin_repair.py`
- `build/audit_codegen_dxfunction_wiring.py`
- `build/audit_chat_ascii_console.py --root .` — 17 checks
- `build/audit_human_entity_formatting.py`
- `build/audit_documentation_onewire_contracts.py`
- `build/audit_async_responsiveness_2_8_3.py`
- `build/audit_application_architecture.py --root . --product localgpt --mode all`
- `build/audit_service_resilience.py --root . --product localgpt` — 1,899 service methods with maintained error boundaries/diagnostics
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 160 source files / 2,426 await tokens / 2,211 `ConfigureAwait(false)` / 29 renderer-affine `ConfigureAwait(true)`
- `build/Assert-XmlDocumentationCoverage.py src/LocalGPT` — 7,283 direct declarations / 415 maintained source files
- Exact Python-equivalent execution of `Assert-TextServiceOwnership.ps1` detection/baseline logic — no new component/controller direct string/regex operations

PowerShell is not installed in the source-inspection environment, so the `.ps1` guard itself was not executed. Its regex, baseline IDs and exclusion rule were reproduced directly against the 2.9.9 tree; the three 2.9.8 failures are absent.

## Release invariants

- LocalGPT, InstallerConsole and WebviewWrapper versions are 2.9.9.
- Council seed version stays 25.
- Wire Protocol stays 2.1.1; all 3 files in its source tree are byte-identical to 2.9.8.
- All 19 explicit `@rendermode` directives are identical to 2.9.8.
- All 137 browser JavaScript source files are byte-identical to 2.9.8.
- `Directory.Build.props` is byte-identical to 2.9.8.
- All six maintained project/property/target XML files parse successfully.
- `CouncilTeams.razor` no longer contains the three direct text-manipulation expressions reported by the 2.9.8 Windows build.
- The configured participant path is release-audited for producer-side rich lanes, completed-result capture, cancellation/error closure and bounded ordered-transcript coalescing.
- Native Ollama automatic-tool construction is release-audited for pre-send transport-name collision validation.
- `localgpt.time_state.now` remains parameterless and its empty-object validation path is release-audited.
- Final pre-package artifact scan is clean; the source tree contains 2,438 files and no `bin/`, `obj/`, `__pycache__`, DLL, EXE, PDB, PYC or PYO artifacts.
