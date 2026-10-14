# LocalGPT 3.0.0 source changelog

## Startup regression repair

- Fixed the release-blocking EF Core startup failure inherited from 2.9.8/2.9.9.
- `CouncilTeamConfiguration` had six persisted scalar properties in the runtime model that were absent from `LocalGptMemoryDbContextModelSnapshot` and from the migration chain. EF Core therefore raised `PendingModelChangesWarning` before deterministic seed stages could start.
- Added migration `20260816233500_AddCouncilTeamUserPolicyFields`.
- The migration preserves existing `CouncilTeamConfigurations` rows and adds:
  - `AllowedAutomaticFunctionsJson` with default `[]`.
  - `IsDeleted` with default `false`.
  - `AllMembersReadinessPreflightMode` with legacy/default enum value `0`.
  - `IncludeAllMembersReadinessPreflightInWorkflowContext` with default `false`.
  - `AllMembersReadinessPreflightMaxOutputTokens` with default `192`.
  - `AllMembersReadinessPreflightPromptTemplate` with default empty text.
- Updated `LocalGptMemoryDbContextModelSnapshot` to match the runtime model.
- The runtime-policy `no definition` exception seen after the migration failure is left fail-fast; it was a downstream consequence of database initialization aborting, not a separate schema fix.

## Release guard

- Added `build/Assert-EfModelSnapshotConsistency.ps1` and wired it into the Windows `BeforeBuild` validation chain.
- The guard derives `DbSet` entity types and scalar/enum persisted properties from source and rejects properties missing from the EF model snapshot.
- Added `build/audit_release_3_0_0.py` for source-only release regression coverage.
- The same source-equivalent check finds the six missing 2.9.9 properties and finds zero missing persisted scalar properties in 3.0.0.

## Preserved 2.9.9 behavior

- The 2.9.9 Council rich live-lane repair is unchanged.
- All 19 explicit `@rendermode` directives are unchanged from 2.9.9.
- All 137 maintained browser JavaScript files are byte-identical to 2.9.9.
- LocalGPT Wire Protocol remains 2.1.1 and its source tree is byte-identical to 2.9.9.
- Council seed version remains 25.
- The async policy remains unchanged, including 29 renderer-affine `ConfigureAwait(true)` sites.

## Versioning

- LocalGPT: 3.0.0
- LocalGPTWebviewWrapper: 3.0.0
- LocalGPTInstallerConsole: 3.0.0
- LocalGPT Wire Protocol: 2.1.1
- The rollover from 2.9.9 to 3.0.0 follows the maintained single-digit version-segment rule; 2.9.10 is not used.

## Build status

This package is source-only and intentionally not compiled in the repair environment. No GitHub access and no .NET/MSBuild invocation were used.
