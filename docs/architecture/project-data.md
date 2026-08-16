# Projects, workspaces, and persistence

## Database-first project model

LocalGPT stores the project model in SQLite through EF Core. The database describes the user's technical intent independently of a particular checkout path or language.

Core records include projects, versions, revisions, requirements, requirement links, artifacts, topics, knowledge links, tracked files, workspace roots, compiler installations, build verifications, and Council review records.

## Identity and paths

Stable identifiers belong to database records. Paths are mutable location data. A project can have several workspace roots or move to another machine without changing its conceptual identity.

Tracked files use normalized relative paths and hashes. This supports exact-source review and prevents a report about one file version from being mistaken for evidence about another.

## Workspace service boundary

The workspace resolver maps a project/revision to an approved root and validates:

- path containment;
- expected structure;
- read/write requirements;
- file-pattern policy;
- environment variables;
- compiler/tool availability;
- danger findings.

The project service does not execute compilers. The artifact service does not silently replace tracked files. The command runner does not decide project policy.

## Compiler inventory

Compiler installations are explicit records with path, version, supported workload, and validation state. Discovery can propose installations; the user selects what the project may use.

Build verification stores the command plan, revision, timing, exit status, bounded output, and artifact evidence. A successful historical record is not a standing approval for future execution.

## EF Core initialization

Database initialization supports both clean installs and older development databases that may contain application tables before migration history was complete.

The bootstrap sequence must:

1. open the configured database safely;
2. inspect migration history and existing schema;
3. distinguish an empty database from a legacy schema;
4. apply or baseline migrations in the maintained order;
5. preserve user data;
6. initialize seed/catalog data idempotently;
7. expose readiness to dependent services.

Logging tables are handled carefully so diagnostic logging does not recurse into an unready database.

## Migration snapshots

EF migration snapshots and generated migrations are architecture artifacts. They must remain ordered, compilable, and consistent with the context model. Manual fixes should not create duplicate identities or silently drop columns.

## Knowledge and runtime policy

Seed services initialize known catalogs and policy definitions. Mutable values are stored in the database or owning service, not in global static collections. User changes remain distinct from shipped defaults.

User-observable behavior policy is configuration data. BusinessObjects define its serializable contract; Services and Controllers validate, persist, reset, and expose it through dependency injection. Shipped social-team presets, prompts, capability/function policies, retry counts, and recovery behavior are resettable templates only and must never become a hidden second runtime policy in orchestration code. Technical implementation invariants such as protocol compatibility identifiers, serialization names, framework wiring, and bounded in-memory buffer mechanics remain code-owned because editing them would change implementation safety or compatibility rather than user behavior.

## Durable entity contract

The database-first model keeps the important records explicit:

- `LocalGptProject` owns revisions, requirements, artifacts, topics, and versions.
- `LocalGptProjectRevision` stores ancestry, status, structure metadata, and build evidence without touching Git or files by itself.
- `LocalGptProjectRequirementLink` maps a requirement to a stable function, service, controller, business object, table, configuration, variable, regex, prompt, knowledge entry, or generated-code target.
- `ProjectDocumentImport` stores bounded normalized text with source, hash, and safety metadata.

Before a Council step calls a function, it identifies the project/revision, maps the task to approved requirements, states missing evidence, and chooses the smallest relevant function set. **Function availability is not a reason to call it.**

## Migration compatibility sequence

Older owner databases can contain application tables before EF migration history was complete. Compatibility handling therefore performs SQLite health checks, schema inspection, stale-lock handling, and a **SQLite online backup** before adopting history or migrating a compatible logging table. A history row is inserted only when the migration's **complete signature** is present. **Refuse partially applied ambiguous schemas** and report the missing markers plus the backup path instead of guessing.

The database logger uses a separate readiness gate so queued startup diagnostics cannot write through EF while migrations and deterministic seed stages are still running.

## Snapshot ordering contract

`LocalGptMemoryDbContextModelSnapshot` is executable model-building code. Keep **properties first**, configure **relationships** only after both entity property blocks exist, and add collection navigations after the matching relationships. An early navigation-only call can accidentally create a shared `Dictionary<string, object>` entity under the CLR type name.

Run `build/Assert-EfSnapshotArchitecture.ps1` after editing the context, an EF entity, a migration, or the snapshot. Static ordering checks protect against the known regression; they do not replace a real migration smoke test.

## Data safety

Database rows, imported JSON, and localization catalogs are untrusted inputs. Services validate shape and bounds before using them to construct paths, commands, routes, or UI markup.
