# EF migration snapshot architecture

`LocalGptMemoryDbContextModelSnapshot` is executable model-building code, not a passive schema note. Its ordering must follow the structure produced by EF Core:

1. Declare every entity and scalar property.
2. Configure relationships only after both source and target entity property blocks exist.
3. Declare collection navigations only after the matching relationships exist.

Calling `modelBuilder.Entity("LocalGPT.BusinessObjects.LocalGptProject", ...)` with a navigation-only block before the project property block can create a shared `Dictionary<string, object>` entity under the CLR type name. Later calls such as `b.Navigation("Artifacts")` then fail during migration validation even though the CLR class contains the property.

## Project navigation contract

`LocalGptProject` retains these database-first collection navigations:

- `Artifacts`
- `Requirements`
- `Revisions`
- `Topics`
- `Versions`

Do not remove a navigation to silence a snapshot failure. Repair the snapshot ordering or regenerate the migration with the exact current DbContext model.

## Required validation

Run `build/Assert-EfSnapshotArchitecture.ps1` after editing:

- `LocalGptMemoryDbContext`
- any EF entity
- a migration
- the model snapshot

The owner build must still start LocalGPT against a disposable or backed-up database. Static ordering checks prevent the known shared-type regression but do not replace a real migration smoke test.
