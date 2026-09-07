# LocalGPT 3.8.9 source validation

This release repairs the actual Council SQL artifact while preserving the established LocalGPT application/runtime architecture.

Validation targets:
- `docs/COUNCIL_KNOWLEDGE_SEED.sql` contains executable `INSERT OR IGNORE INTO "CouncilKnowledgeEntries"` SQL, not a prose/comment manifest;
- the supplied historical seed data is preserved as 60 stable knowledge rows;
- a semantic comparison of `Id`, `Topic`, `Scope`, `Content`, `Source`, `HelpfulSources`, `Tags`, `Confidence`, `IsUserApproved`, `IsPinned`, and `IsArchived` matches the supplied SQL for all 60 rows;
- every insert includes the current required lifecycle fields (`VerificationStatus`, `ReviewStatus`, `StalenessReason`, `StalenessDetectedBy`, `SourceHash`) and a verification timestamp;
- deterministic `SourceHash` values match LocalGPT's `Topic + Scope + Source + HelpfulSources + Content` SHA-256 contract;
- executing the script against the current Council-knowledge table shape inserts all 60 rows without constraint errors;
- executing the script a second time leaves the row count unchanged, proving the intended idempotent repair behavior;
- no destructive `UPDATE`, `DELETE`, `DROP`, `ALTER`, or `REPLACE` statement is present in the seed;
- the clean-source build prerequisite rejects a missing or comment-only Council seed before expensive build/documentation work;
- `/database` remains the existing structured table editor; no claim is made that LocalGPT currently executes arbitrary uploaded SQL;
- the eight established `AddHostedService<T>` registrations remain intact and no rejected post-listen coordinator is reintroduced;
- the 3.8.3 provider and 3.8.4 per-user path contracts remain present.
