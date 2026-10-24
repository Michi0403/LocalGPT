# Read-only SQLite JSON audit — LocalGPT 6.2.7

This report inspects the supplied `localgptdbtojson.zip` snapshot; no user records were modified, added, replayed, approved or removed. The source ZIP does **not** contain the user's database export, application logs or conversation data. These counts reflect the exported snapshot and not necessarily the database after the user upgrades LocalGPT.

## Verified configuration inventory

| Table / relationship | Records | Finding |
|---|---:|---|
| CouncilTeamConfigurations | 36 | 25 system-seeded and 11 user-modified; all enabled; unique team keys |
| CouncilModelPresets | 24 | One default, all marked user-approved; approved presets do not establish live model reachability |
| CouncilPromptStarterConfigurations | 9 | No references to missing team keys |
| CouncilRuntimeClassConfigurations | 17 | Persisted user-visible runtime class definitions |
| CouncilKnowledgeEntries | 448 | 330 source-backed, 84 model-suggested, 33 user-verified, 1 runtime-detected; 224 still need user review |
| RegexPatterns | 194 | No empty patterns or duplicate IDs in the export |
| LocalGptProjects | 62 | Only two project records have a populated `RootPath` in the export |
| LocalGptProjectRevisions | 479 | No missing ProjectId references |
| LocalGptProjectTrackedFiles | 238 | All remain `Pending`, zero complete read/review evidence, only one project covered |
| ProjectWorkspaceRoots | 1 | No missing ProjectId reference; recorded access status is not a live filesystem validation |
| ProjectOrganicSkillLinks | 0 | No persisted explicit project-to-skill relations |
| CouncilMemberOrganicSkillLinks | 0 | No persisted explicit member-to-skill relations |
| CouncilKnowledgeRegexPatternLinks | 0 | No explicit knowledge-to-regex relations |
| LocalGptProjectTopicKnowledgeLinks | 0 | No explicit topic-to-knowledge relations |
| LocalGptProjectRequirementLinks | 0 | No explicit project-to-requirement relations |

No orphaned tracked-file ProjectId or RevisionId references, cross-project tracked-file/revision mismatches, duplicate team keys, broken saved X-child-Council references or broken saved X-target-step references were detected. Generic role labels in older team templates are not foreign keys and must not be treated as broken relations.

## Team and workflow coverage

- The exported `learning-round` is enabled, system-seeded, **not user-modified** and still at seed version **47**. Its `learning-inventory` step requires a verified workspace read and sets eight recovery attempts but `includeWorkspaceSourceExcerptsOnRecovery=false`. The 6.2.6/6.2.7 source supplies seed version 48 and enables bounded excerpts for a resettable built-in seed. Only a runtime migration/seed-refresh log on the upgraded instance can prove the export has advanced.
- The exported `learning-round2` **is user-modified** (seed version 40). Its four older steps do not carry newer per-step source-read recovery fields. It must not be silently overwritten with new built-in defaults. The user can compare/duplicate/reset intentionally.
- `spreadsheet-team` and `openscad-team` exist, enabled, with six and four `publisher.*` preferred capability keys respectively, and six workflow steps each. They use role pools rather than a specified single model; remote action permissions remain independent.
- Four newer supplied teams — `publisher-image`, `publisher-video`, `publisher-audio` and `publisher-panel` — are **absent in the exported database**, even though the 6.2.6+ source defines them as resettable seeds. The export alone cannot prove the upgraded runtime seed service has run.
- Explicit relational mapping tables for Council members, skills and project knowledge are empty. This means **there is no stored link-based affinity** to traverse through those relationships in this snapshot, not that the runtime 1-Wire peer catalog has zero functions. Runtime discovery is separate.
- 24 saved model presets and old assigned-role preferences are not evidence the endpoints are online at execution time. Refresh reachable provider identity and let the user explicitly save edits.

## Where `compilestick` came from

A streaming raw-text scan of **every file in the exported ZIP**, including the 602-MiB uncompressed `ApplicationLogs.json`, found **31 occurrences** of `compilestick`, all inside **`ApplicationLogs.json`**. The contexts include a macOS exception stack path rooted at `/Volumes/compilestick/localgpt/.../OllamaThinkingChatClient.Transport.cs`. The term was not present in the scanned exported teams, model presets, projects, persisted hardware profile or system variables, nor in maintained LocalGPT/PublisherStudio C# or build scripts.

This is **historical diagnostic text in persisted application logs**. A Windows LocalGPT process can display it when reading a shared/imported database or a previous debug record; it does not imply this Windows machine has, mounted, or attempted to use the Apple-formatted drive. A log excerpt or model answer that quotes that path must never be promoted into a canonical machine, provider, build root or project configuration. Windows and macOS records need runtime-host provenance when presented to an AI agent. This release does not erase historical logs or rewrite user-provided database data.

## Frontend and AI data-flow requirements

1. A Council-team picker must use persisted `CouncilTeamConfigurations` and resettable blueprint descriptions; do not substitute a private C# list of preferred PublisherStudio function keys. Version 6.2.7 projects keys from stored team preferences and currently advertised 1-Wire peer capabilities on a supervised refresh, without bypassing function permissions.
2. A UI must distinguish persisted team preference from **currently connected/online**, **permitted**, **approved for execution**, and **successfully invoked**. A capability name in JSON is not proof of a runnable tool.
3. Workflow steps and roles must be read from their `RolesJson` / `WorkflowStepsJson` serialized definitions and must preserve `IsUserModified`. A seed refresh may upgrade only resettable system-seed records; it must not replace `learning-round2` or user-defined structures.
4. Source extraction, project workspace sync, full text/semantic page reads, regex detection, reviewer output, verification and durable `LearningReadComplete` checkpoints are **different stages**. A ZIP-entry inventory cannot stand in for a verified source-read receipt, and no pending file is automatically declared reviewed.
5. `RegexPatterns` and `CouncilKnowledgeEntries` contain real data; empty link tables mean the UI cannot assume every regex belongs to a knowledge entry or project topic. Generic search/inspection is still possible via each maintained service; creating links requires explicit review and entity identity.
6. An EF `Include`/`ThenInclude` failure, migration defect, or missing automatic backfill cannot be confirmed from a JSON dump. For any proposed repair, use a live test querying the service API and matching SQLite records; never fabricate navigation rows simply to make a graph look complete.

## Scope of the release

6.2.7 repairs the static-policy-breaking source list, moves live peer discovery to the supervised lifecycle, repairs a compiler-local variable collision and adds source-level regression coverage. **It does not migrate the supplied database**, backfill missing relationship rows, change any approvals, auto-select unverified models or certify full 1-Wire/EF persistence behavior without running the application.
