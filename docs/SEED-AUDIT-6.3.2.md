# LocalGPT 6.3.2 — offline default-seed audit (source only)

Source comparison: user-provided `dbasjson.zip` read locally, without extracting or shipping the export; LocalGPT 6.3.1 authoritative source baseline. The supplied database itself is **not** modified or bundled.

| Data | Existing export | Action |
|---|---:|---|
| CouncilKnowledgeEntries | 448 | 18 maintained repository docs and 42 curated general-purpose core reference entries bundled for first installation, plus runtime path reference; intentionally no wholesale import |
| RegexPatterns | 194 | Every exported name is present in maintained regex source; required names verified after durable insertion |
| Prompts | 16 | Maintained default prompts already present; required keys and languages verified after durable insertion |
| SystemVariables | 2,345 | Do not copy runtime values, paths, caches, or hardware settings; verify code-owned required defaults exist without overwriting user values |
| CouncilTeamConfigurations | 40 | Existing system-team seeding and user-created-team precedence retained; no replay of users' workflows |
| CouncilRuntimeClassConfigurations | 17 | Existing runtime-class game seeds retained; no user-specific game state cloned |
| CouncilModelPresets | 24 | Existing generic model preset seeding retained; no personal performance or hardware settings cloned |

## Critical differences repaired

1. The maintained `KnowledgeFiles` catalog lists 18 authoritative reference paths. All 18 now have compile-time embedded fallbacks with unique logical names, rather than relying on a publish layout to contain source-only files. A missing or empty mandatory document fails initialization with an explicit error.
2. `COUNCIL_KNOWLEDGE_SEED.sql` is a reference and repair text file, not startup-executed SQL. A curated selection of 33 generic reference entries from that source SQL is now shipped as an explicit, validated JSON manifest, not executed as arbitrary SQL.
3. Nine generic `LocalGPT built-in seed` reference entries were promoted from the export into that manifest (Ollama context, Blazor/DevExpress UI, source verification, product archetypes, SQLite diagnostics, native uploads, provider-neutral model hosting, Harmony display, whole-solution acceptance). They contain no personal paths or per-device data. The original exported record identities are preserved for additive seeding.
4. Core seed stages are checked after SaveChanges. Missing required regex/prompt/variable/knowledge keys and unchanged reference hash mismatches are detected; a stage error no longer marks initialization complete.
5. Knowledge reconciliation preserves records whose source was changed or whose persisted content no longer matches its seed hash. The existing seeded document changes only when it remains unmodified.
6. Generic Ollama alias tokens seen in Council configuration but missing in offline fallback are included: deepscaler:1.5b, deepseek-ocr:3b, dolphin3:8b, openthinker:7b and openthinker:32b. Ollama/provider live inventory remains authoritative; tags are not asserted downloadable until the endpoint confirms availability. Custom/local models remain user-owned.

## Explicit exclusions

* ApplicationLogs, chat messages, generated projects, errors, diagnostics, approvals and incomplete requests.
* Hardware-road measurements, locally configured hosts, tokens, source paths, custom Council presets, local models and personal knowledge or learn-base snapshots.
* Domain-specific historical project notes, curriculum, example-site experiments, historical Council outcomes, and the remaining 27 SQL reference records that aren't general installation essentials.

## Verification boundary

Source-level checks and XML/resource manifest verification can be run without .NET. No .NET build, EF migration, fresh Windows installation, execution of embedded resources or real Ollama pull has been performed here. The seed-stage runtime verifier is supplied as source and requires an actual run to confirm database migration/seeding.
