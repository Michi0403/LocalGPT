# LocalGPT 5.5.4

Full-source maintenance hotfix for the confirmed Visual Studio `Assert-TextServiceOwnership.ps1` failure in 5.5.3. The direct SQLite type string comparison in `DataManagement.razor` now belongs to the already injected `SqliteGridPresentationService`. The same table/primary-key read-only rules remain active; classification failures are handled conservatively by leaving the editor read-only. The original Windows PowerShell architecture guard and its baseline are **unchanged**. A new source regression audit reproduces that guard's inspected folders and matching rules.

Preserves the complete 5.5.3 bounded DevExpress Data Management workbench, CRUD/paging and field preferences, the InteractiveServer circuit, `/install` VRAM and CanIRun.ai repairs, graphical thinking, Human Collaboration, per-file learning checklist and documentation PDF recovery. See `CHANGELOG-v5.5.4-TEXT-SERVICE-OWNERSHIP-REPAIR.md` and `VALIDATION-v5.5.4-source.md`.

No .NET restore/build/publish or GitHub operations were attempted. Static source audits passed; the final Windows PowerShell/DevExpress build remains a user-side check. PublisherStudio is unchanged and is not repackaged.
