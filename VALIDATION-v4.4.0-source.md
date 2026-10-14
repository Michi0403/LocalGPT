# LocalGPT 4.4.0 source validation

## Environment boundary

This release was revised and inspected from the supplied source repository only. No `dotnet`, NuGet restore, build, publish, EF tooling, GitHub access, or other online repository access was used. PowerShell itself is not available in this environment, so PowerShell-only build guards were not invoked directly; their relevant source contracts were reproduced read-only where practical and the maintained Python audits were executed directly.

## Source checks performed

The following checks completed successfully before packaging:

- `build/audit_application_architecture.py --root . --product localgpt --mode all`;
- `build/audit_service_resilience.py --root . --product localgpt` — 2469 guarded service methods;
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 268 files / 3322 await tokens;
- `build/audit_configurable_behavior_policy.py`;
- `build/audit_release_4_4_0.py`;
- exact text-service ownership baseline reproduction for Components/Controller source;
- JavaScript diagnostics manifest/hash reproduction for all 24 maintained browser files plus `node --check` syntax validation;
- localization catalog UTF-8/key-parity reproduction for 2263 LocalGPT UI strings;
- static-web-asset existence reproduction for 357 maintained assets;
- documentation pointer-overlay source-contract reproduction;
- system-variable initialization baseline reproduction;
- EF model/snapshot consistency reproduction for 47 DbSet entity types plus explicit Game-profile one-to-one relationship parity;
- exact MainLayout diagnostics-boundary and routed-page `InteractiveServer` checks;
- changed/new C# delimiter sanity plus release JSON/XML parse checks;
- XML documentation delta comparison against the supplied 4.3.9 working tree.

After archive creation the source ZIP was re-extracted and `build/audit_release_4_4_0.py` was run again against the extracted copy, together with a file-count/content-integrity comparison.

## Game-project persistence and migration

`LocalGptGameProjectProfile` is a dedicated one-to-one Project-owned row. Migration `20260916204500_AddGameProjectAuthoringProfiles` creates `LocalGptGameProjectProfiles`, its unique `ProjectId` index, update-time index, and restrictive Project foreign key. `LocalGptMemoryDbContextModelSnapshot` and `Assert-EfSnapshotArchitecture.ps1` carry the same relationship contract.

The authoring profile remains separate from `LocalGptProjectRequirement`. Builds copy only user-approved requirement ids into the `ProjectGameDefinition` traceability baseline; they do not flatten requirements into the scenario field.

## Build boundary

`SaveGameProjectProfileRequest` owns editable Project design input. `BuildProjectGameRequest` is approval-only. `GameProjectService.BuildAsync` requires an already-persisted profile and does not call `SaveProfileAsync`. The resulting `GameBuild / Runtime Definition` artifact records project/version identity, source profile, current revision, approved requirement ids, runtime profile and lower runtime defaults before GameDirector consumes it.

## Maintained Council presets

Four source-controlled presets are registered through the existing seed-evolution path: requirements discovery, Game-project development, reusable engine extension development, and playtest/QA. The discovery preset uses human collaboration for missing intent and persists only confirmed requirements. The engine-extension preset explicitly requires migration/snapshot and repository-policy review when reusable lower-layer changes are needed.

## XML documentation note

The broad source XML quality audit reports 359 historical findings in the supplied 4.3.9 working tree. LocalGPT 4.4.0 reports the same normalized 359 findings: **zero new XML documentation findings** were introduced by this release. No XML guard, exemption, or baseline was weakened to obtain that result.

## Expected owner-side verification

Run the repository's normal maintenance-enabled build. In particular, do not disable the operational diagnostics, InteractiveServer, async continuation, application-static, text-service ownership, EF snapshot/model, JavaScript diagnostics, localization, or static-web-asset guards. The new migration should then be applied by the normal database initialization path on an existing 4.3.9 database.
