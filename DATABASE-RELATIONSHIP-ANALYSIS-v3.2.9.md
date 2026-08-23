# LocalGPT 3.2.9 — supplied database relationship analysis

This report is based on the SQLite SQL export supplied for the 3.2.9 review. The release does not rewrite user data automatically; observations that could require destructive normalization are documented for later, explicit migration work.

## Integrity baseline

- SQLite `PRAGMA integrity_check` returned `ok`.
- `PRAGMA foreign_key_check` returned zero violations.
- 48 application tables were present.

This is important: the database is not broadly corrupt or full of orphaned declared foreign keys. The "disconnected" feeling is primarily an application-model and discoverability problem.

## Knowledge structure

- 381 Council knowledge entries.
- 118 reusable RegEx patterns.
- 17 LocalGPT projects and 5 persisted project topics.
- 0 `LocalGptProjectTopicKnowledgeLinks` rows.
- 49 normalized topic/scope duplicate groups covering 225 knowledge rows.

The duplicate-heavy groups are primarily source-backed LearnBase architecture fingerprints. They should not be merged merely because topic/scope text repeats: different imports can have different content hashes, sources, verification dates, review state or provenance. The UI now distinguishes them with semantic labels and a short stable identity.

The empty project-topic knowledge link table is a stronger architectural signal: the relationship capability existed but was effectively stranded. 3.2.9 exposes it directly so knowledge can become project-scoped deliberately rather than only through indirect workflow behavior.

## RegEx structure

One exact pattern/flags duplicate pair was found between:

- `builtin.mod-id-cleaner`
- `builtin.package-part-cleaner`

The release does not delete or merge either row. RegEx identity is semantic as well as syntactic; two named patterns can intentionally share an expression while serving different purposes. The new knowledge/RegEx relationship stores that *purpose and meaning on the relationship*, which is the safer place for contextual semantics.

## Declared FK relationships whose reverse CLR navigation was missing

3.2.9 restores navigation for already-declared relationships instead of altering the constraints:

- knowledge rating → knowledge;
- project-topic knowledge link → knowledge;
- revision requirement/artifact relationships;
- requirement artifact relationship;
- project/revision document imports;
- project organic-skill assignments;
- compiler-installation build verification;
- project-scoped embedded firmware plan;
- conversation-scoped Council game session.

The new knowledge↔RegEx link is the only new relationship table in this release.

## Soft identifiers intentionally left soft

The database also contains identifier-looking columns without declared foreign keys. They are not all bugs. Some are cross-workflow correlation IDs or optional references whose target lifetime differs from the source row.

Examples observed in the supplied data/model include chat project/version IDs, knowledge supersession IDs, deferred invocation approval/Council/operation IDs, human collaboration decision/Council/approval-session IDs, compiler knowledge IDs and preferred compiler-installation IDs.

A few of these may deserve hard FKs later, especially when the related feature has enough real persisted data to establish deletion semantics. 3.2.9 deliberately avoids a broad "every Id becomes a foreign key" migration because that can break historical/correlation records and would require choosing cascade/restrict/null semantics without sufficient evidence.

## Recommended next data-maturity pass

A future dedicated migration can safely focus on one soft relationship family at a time, with preflight queries that report invalid references before adding constraints. Good candidates are knowledge supersession and project-workspace preferred compiler selection once those fields contain representative persisted data. Correlation IDs used for audit/workflow history should generally remain soft unless a strong lifetime invariant is proven.
