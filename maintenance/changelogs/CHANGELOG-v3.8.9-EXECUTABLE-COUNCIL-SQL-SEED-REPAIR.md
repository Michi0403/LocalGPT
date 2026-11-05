# LocalGPT 3.8.9 — executable Council SQL seed repair

- Restores the user-supplied `docs/COUNCIL_KNOWLEDGE_SEED.sql` as real SQLite DML instead of the non-executable manifest mistakenly shipped in 3.8.8.
- Preserves all 60 historical Council knowledge rows, stable identifiers, topics, content, sources, tags, confidence values, approval flags, pinning, and archive state from the supplied SQL.
- Verified semantic equality of those 60 historical rows against the supplied SQL before packaging; only current-schema lifecycle/hash fields and executable formatting are added. The supplied source file SHA-256 was `440678467a506f06827dd225e3a0fccd1705376aa84a56a11de90d3d48902db2`.
- Updates each insert for the current `CouncilKnowledgeEntries` schema by supplying `VerificationStatus`, `ReviewStatus`, `LastVerifiedAtUtc`, `StalenessReason`, `StalenessDetectedBy`, and a deterministic SHA-256 `SourceHash` derived with the same field ordering used by `CouncilKnowledgeContentService.ComputeSourceHash`.
- Keeps the script idempotent with `INSERT OR IGNORE`; existing rows and user edits under the same identifiers are not overwritten by a repair rerun.
- Confirms that LocalGPT's `/database` frontend currently supports structured row editing but does not expose a generic raw-SQL file execution workflow. Therefore the Council SQL repair asset remains packaged/referenced instead of deleting its contract.
- Adds an executable-SQL source audit and an early build prerequisite check so a comment-only or schema-incompatible Council seed cannot pass source validation again.
- Retains the 3.8.3 provider onboarding, 3.8.4 cross-platform pathing, and 3.8.7 normal hosted-service/boot-cycle repairs without changing application startup ownership.
