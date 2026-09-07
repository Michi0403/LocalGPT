# LocalGPT 3.8.9

LocalGPT 3.8.9 keeps the 3.8.3 provider onboarding repair, the 3.8.4 cross-platform per-user path contract, and the 3.8.7 boot dependency-cycle repair while correcting the Council SQL seed artifact restored in 3.8.8.

`docs/COUNCIL_KNOWLEDGE_SEED.sql` is executable, idempotent SQLite again. Its 60 source-backed Council knowledge rows come from the supplied historical seed, retain their stable identifiers/content, and now include the current required knowledge lifecycle columns and deterministic source hashes. The script uses `INSERT OR IGNORE`, so rerunning it does not overwrite existing rows under the same identifiers. LocalGPT packages the script as a repair/backup asset and repository reference; application startup does not auto-execute arbitrary SQL.

The `/database` frontend remains a structured SQLite table/knowledge editor. It does not currently provide a generic raw-SQL file executor, so the Council SQL file remains explicitly packaged and referenced rather than being removed behind a nonexistent frontend workflow.

The established eight `AddHostedService<T>` registrations remain intact. No 3.8.5/3.8.6 post-listen coordinator lifecycle is present.

See `CHANGELOG-v3.8.9-EXECUTABLE-COUNCIL-SQL-SEED-REPAIR.md` and `VALIDATION-v3.8.9-source.md`.
