# Read-only SQLite JSON export audit — 23 October 2026

This document contains aggregate counts only. The uploaded database content, messages, machine names, file paths, user identifiers and credentials are intentionally not copied into this source release.

## Verified aggregates

| Entity | Exported count | Observations |
|---|---:|---|
| LocalGptProjects | 62 | All are non-archived in this export. |
| LocalGptProjectRevisions | 479 | 61 are flagged as current; all revision ProjectId references resolve to a project. |
| LocalGptProjectTrackedFiles | 238 | Every row is `Pending`; all report `LearningReadComplete = false` and have no learning evidence. |
| Tracked-file project coverage | 1 project / 1 revision | The other 61 project records have no tracked-file rows **in this export**. |
| Broken tracked-file project/revision references | 0 | No orphaned ProjectId or non-null RevisionId values among the 238 tracked files. |
| CouncilKnowledgeEntries | 448 | Entries are retained, with differing review/verification statuses. |
| RegexPatterns | 194 | Curated regex records are retained. The runtime-policy regex definitions are an additional collection, not a substitute. |
| CouncilTeamConfigurations | 36 | Built-in `learning-round` has `IsSystemSeed = true`, `IsUserModified = false`; `learning-round2` is user-modified. |
| ProjectWorkspaceRoots | 1 | A persisted workspace mapping exists; presence does not prove the source path is currently mounted. |
| ProjectOrganicSkillLinks and CouncilMemberOrganicSkillLinks | 0 | These are optional relation tables; empty contents alone do not prove a broken navigation property. |

## Observed control-flow mismatch

The supplied Council log reports a read-only extraction of 1,942, 2,220, and 435 ZIP entries and a single 4,600-file chat workspace. This is not a database-import receipt. Project/file synchronization occurs later in Learning Round (`localgpt.learning.maintain`/workspace sync), after the failed `learning-inventory` evidence gate. The failure therefore explains why these newly uploaded archives need not have corresponding tracked-file rows despite successful extraction.

The stored built-in Learning Round has eight source-read recovery attempts configured but no source excerpts on recovery. The 6.2.5 workflow prevents a first retry when no model read occurred. Version 6.2.6 repairs the first-retry logic and bumps the maintained seed to opt in to bounded excerpts while preserving user-custom teams.

## Limitations

This is a read-only JSON-export audit, **not** an integrity check of the live SQLite file or a test of EF Core Include/ThenInclude navigation behavior. No schema changes, backfill, silent reassignment of projects, or invented completed-file status were performed. The absence of rows in optional link tables and old projects does not itself prove corruption. For datafeed correctness, test live with the exact project/revision IDs and existing service API results before proposing migrations.

No literal external-volume name was found in LocalGPT's 6.2.5 C# source or in the sampled entity stores. Build-storage paths and mounted volumes are contextual filesystem locations, not AI compute hosts.
