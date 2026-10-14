# LocalGPT 2.9.8 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. A Windows build remains the authoritative compile/runtime validation.

## Passed source audits

- `build/audit_release_2_8_5.py` through `build/audit_release_2_9_8.py`
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
- `build/audit_service_resilience.py --root . --product localgpt` — 1,896 service methods with maintained error boundaries/diagnostics
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 160 source files / 2,426 await tokens
- `build/Assert-XmlDocumentationCoverage.py src/LocalGPT` — 7,280 direct declarations / 415 maintained source files

## Release invariants

- LocalGPT, InstallerConsole and WebviewWrapper versions are 2.9.8.
- Seed version is 25.
- Wire Protocol stays 2.1.1 and its source tree is byte-identical to 2.9.7.
- All 19 `@rendermode` directives are exactly identical to 2.9.7.
- `Directory.Build.props` is byte-identical to 2.9.7.
- All 137 browser JavaScript source files are byte-identical to 2.9.7.
- All six maintained project/property/target XML files parse successfully.
- The source tree contains no `bin/`, `obj/`, `__pycache__`, DLL, EXE, PDB, PYC or PYO artifacts before packaging.
