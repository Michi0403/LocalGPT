# LocalGPT 3.0.0 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. A Windows build and startup remain the authoritative compiled/runtime validation.

## Reproduced failure

The supplied runtime log shows the newest startup attempt aborting in `DatabaseInitializationService.RunMigrationAsync` because EF Core detected pending `LocalGptMemoryDbContext` model changes. Runtime-policy loading then returned no definition because deterministic database initialization had already aborted.

A source comparison of all 41 `DbSet` entity types found exactly six persisted scalar properties missing from the 2.9.9 snapshot, all on `CouncilTeamConfiguration`.

## Migration validation

- Added `20260816233500_AddCouncilTeamUserPolicyFields`.
- Updated `LocalGptMemoryDbContextModelSnapshot`.
- Source audit covers 41 `DbSet` entity types and 571 persisted scalar/enum properties with zero missing snapshot properties.
- A SQLite migration simulation started from the preceding `CouncilTeamConfigurations` schema with an existing user row, added all six columns, preserved the row, and produced defaults `[]`, `0`, `0`, `0`, `192`, and empty text as intended.
- No database reset is required by this repair.

## Passed source audits

- `build/audit_release_2_8_5.py` through `build/audit_release_3_0_0.py`
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
- `build/audit_service_resilience.py --root . --product localgpt` — 1,899 service methods
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 160 source files / 2,426 await tokens / 2,211 `ConfigureAwait(false)` / 29 renderer-affine `ConfigureAwait(true)`
- `build/Assert-XmlDocumentationCoverage.py src/LocalGPT` — 7,286 direct declarations / 416 maintained source files
- Python-equivalent execution of the new EF model/snapshot build guard — zero missing scalar/enum properties

PowerShell is not installed in the source-inspection environment, so the new `.ps1` guard itself was not executed here. Its source-derived algorithm was exercised directly against both 2.9.9 and 3.0.0 data: 2.9.9 produces the six expected findings, while 3.0.0 produces none.

## Release invariants

- LocalGPT, InstallerConsole and WebviewWrapper versions are 3.0.0.
- Council seed version remains 25.
- Wire Protocol remains 2.1.1 and its 3 source files are byte-identical to 2.9.9.
- All 19 explicit `@rendermode` directives are identical to 2.9.9.
- All 137 browser JavaScript source files are byte-identical to 2.9.9.
- The Council live-lane/ordered-transcript repair from 2.9.9 is unchanged.
- Final pre-package scan contains 2,443 files and no `bin/`, `obj/`, `__pycache__/`, DLL, EXE, PDB, PYC or PYO artifacts.
