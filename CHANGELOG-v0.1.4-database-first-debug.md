# LocalGPT v0.1.4 — Database-first debug candidate

Status: **debug candidate; owner compile and runtime validation required**.

## Closed in this iteration

- [x] Broke the startup DI cycle by replacing `EfChatMemoryService -> DevExpressChatService` with the stateless `IChatMemoryMessageMapper` contract.
- [x] Added database-backed project revisions and branches for language-neutral project structures.
- [x] Added project requirements, stable requirement links, and named project artifacts for Regex, variables, configuration, prompts, knowledge, business objects, DXFunctions, and CodeDOM targets.
- [x] Added bounded Regex validation with a process timeout before persistence.
- [x] Added safe text-document ingestion with extension, size, binary/control-byte, encoding, normalization, hash, and untrusted-data controls.
- [x] Added guarded DXAI discovery and maintenance functions for project architecture and SQLite data.
- [x] Added model-selection presets persisted through EF Core and exposed in DXChat.
- [x] Added type-aware SQLite row editors, persistent per-column editor choices, editable non-integer keys on inserts, and strict affected-row validation.
- [x] Added explicit human ratings and approvals for council knowledge entries.
- [x] Added a project/revision architecture briefing that requires requirement mapping before function selection.
- [x] Kept the non-blocking Human Collaboration Inbox, deferred one-use approvals, ambient human/council scopes, and human council participation.
- [x] Updated the EF model snapshot for all entities introduced by the database-first migration.
- [x] Added repository checks for the DI-cycle boundary and tracked architecture tasks.

## Open tasks carried forward

- [ ] Run the licensed Windows/DevExpress Debug and Release builds and record the resulting compiler logs.
- [ ] Execute startup service-provider validation and confirm no additional runtime DI cycles.
- [ ] Smoke-test migration on a copy of an existing LocalGPT SQLite database, including downgrade/backup behavior.
- [ ] Add a repository-pull UI that feeds downloaded harmless text files through `ISafeTextDocumentService`; the ingestion service and approved DXAI import function exist, but the uploaded source archive contained no pull UI/script integration point to patch safely.
- [ ] Add richer editable mask/format/null-text fields to the SQLite preference UI. Editor-kind persistence and automatic inference are complete; custom mask strings are persisted and displayed but not yet edited from the page.
- [ ] Add a visual requirement-link browser with validation against live business-object/function catalogs. Stable named links are saved now; catalog-assisted pickers remain future polish.
- [ ] Add integration tests using the real DevExpress packages and SQLite provider after the owner build environment restores licensed dependencies.

A task may be marked closed only when its implementation, migration or compatibility effect, validation coverage, and user-visible behavior have all been checked. Open items must be copied into the next current changelog until closed or explicitly rejected by the owner.
