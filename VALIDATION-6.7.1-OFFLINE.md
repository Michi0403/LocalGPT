# LocalGPT 6.7.1 — Offline validation

- **Root cause:** EF pending-model-changes warning on 6.7.0 startup. Existing migration `20261029123000_AddCouncilRoleAssignments.cs` is present; its manually maintained snapshot contains an extra foreign-key index on the two-column `(TeamKey, RoleName)` prefix of the three-column primary key. The runtime model does not declare this index, and EF's key-covering index convention does not produce it.
- **Correction:** Removed only the redundant `b.HasIndex("TeamKey", "RoleName")` from the CouncilRoleModelBinding snapshot entity; kept primary key, parent FK cascade, role-name/model-key indexes and existing migration identifier and operations intact.
- **Regression prevention:** Extended build-wired `build/Assert-EfModelSnapshotConsistency.ps1` with Council role FK/index and migration presence validation. No warnings suppressed, no destructive database actions, no new Python/Node tests in source.
- **Baseline:** 2338 original 6.7.0 source files preserved; 3 modified files: `build/Assert-EfModelSnapshotConsistency.ps1`, `src/LocalGPT/LocalGPT.csproj`, `src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs`; 2 new documents; no deleted files.
- **JavaScript:** 28 maintained browser script hashes match repository manifest.
- **Runtime targets:** win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64 preserved.
- **Schema shape smoke test:** An isolated SQLite in-memory equivalent of the `CreateTable`/`CreateIndex` operations passes insert, foreign-key and cascade checks. This is *not* EF migration execution.
- **Limitations:** `dotnet` and PowerShell are unavailable in this container. EF Core `HasPendingModelChanges`, real Windows build, migration application on an existing database, and seeding have **not** been executed. The app should be tested against a safely backed-up existing database; do not reset or hand-edit its migrations history.
