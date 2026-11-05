# LocalGPT 6.7.1 — Council role EF model/snapshot repair

## Incident

6.7.0 starts but database initialization aborts with `Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning`. Deterministic seed stages are consequently skipped. The 6.6.7 role-assignment schema migration **is present** (`20261029123000_AddCouncilRoleAssignments.cs`), but its manually maintained model snapshot erroneously defines a redundant index `CouncilRoleModelBindings(TeamKey, RoleName)`.

## Repair

- Remove only the redundant composite index from `LocalGptMemoryDbContextModelSnapshot.cs`. `TeamKey, RoleName` is already the leading prefix of the binding primary key `(TeamKey, RoleName, ModelKey)`; EF Core's foreign-key index convention does not create that separate index. The runtime model never declared one. Retain the actual `ModelKey` lookup index and the `RoleName` index on the parent role table.
- Preserve the already-distributed 20261029123000 migration ID, Up/Down schema operations, and existing migration history. No database reset, migration-history rewrite, warning suppression, or seed rewrites.
- Extend the existing build-wired `Assert-EfModelSnapshotConsistency.ps1` to check the new role mapping, snapshot indexes, and SQL migration contracts, not just entity scalar properties.
- Advance release version from 6.7.0 to **6.7.1**.

## Verification

Inspect `VALIDATION-6.7.1-OFFLINE.md`. Archive and static schema validation were performed offline. **EF's runtime model comparison and migration execution remain unverified without a .NET SDK**. On a supported Windows build host, build, then start LocalGPT against the existing database and confirm database initialization and deterministic seeding finish; if an EF model drift warning persists, inspect with EF tooling rather than suppress it.
