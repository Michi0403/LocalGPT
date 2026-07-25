# LocalGPT architecture

## Architecture goal

LocalGPT keeps the owner’s established “old-school .NET, updated” shape: one deployable local application with explicit backend services, controllers/endpoints, domain/business objects, provider adapters, persistence, Blazor UI, and optional desktop/bootstrap wrappers. It is a modular monolith, not a collection of accidental global statics.

The boundary and release conventions follow the owner’s current BlazorPublisher approach. TacosPortalOpen is used as a secondary reference for the host/core/client/wrapper separation. No source from either reference repository is copied into LocalGPT.

## Stable project boundaries

- **LocalGPT** — ASP.NET Core loopback host and Interactive Blazor Server application. Owns the UI composition root, controllers/endpoints, application services, provider sessions, local knowledge, diagnostics, and persistence orchestration.
- **LocalGPTInstallerConsole** — optional installation/bootstrap helper. It must not own application-domain rules or AI routing.
- **LocalGPTWebviewWrapper** — optional desktop shell. It hosts the loopback application; it must not own provider state, database state, formatter state, or business rules.
- **DevExpress integration** — UI/integration boundary only. Domain and service contracts must remain understandable and testable without placing business state inside DevExpress components.

## Dependency direction

```text
Blazor components / controllers / endpoints
                    |
                 interfaces
                    |
 application / provider / formatting / persistence services
                    |
       EF Core / SQLite / HTTP / bounded filesystem access
```

Components and controllers depend on interfaces. Services implement those interfaces. EF Core owns persistence. Provider SDKs and HTTP wire types stay inside provider adapters. UI state, response-stream state, database state, and configuration state do not belong in global helper classes.

## Static-code rule

Static code is allowed only for deterministic, side-effect-free operations such as:

- pure conversions;
- immutable constants;
- extension syntax over caller-owned values;
- validation that does not read mutable global state;
- formatting helpers that do not buffer across calls.

A class must become a service when it owns mutable state, configuration, persistence, HTTP clients, filesystem effects, command execution, logging policy, request/stream lifetime, or provider selection. The former `Extensions/PlainStatics` compatibility area has been retired. New runtime behavior must be implemented behind an interface or a clearly owned service; static code is limited to pure extensions, immutable constants, generated regex accessors, framework entry points, and security invariants.

## Service lifetimes

- **Singleton** — immutable seed catalogs, database options/file-health coordinator, protocol resolver, formatter factory, chat-content renderer, configuration writers, and stateless inventory services.
- **Scoped** — chat clients, council workflows, EF Core consumers, command runner, prompt/variable services, and page/user workflow state.
- **Per response** — `IChatResponseFormatter` instances created by the singleton factory. Their buffers and counters are never shared.
- **Hosted startup service** — database migration and deterministic initial feed. It runs immediately and idempotently.

## Database initialization and initial feed

`DatabaseInitializationService` is the single schema/seed coordinator. Before EF Core opens the store, `IDatabaseFileHealthService` performs bounded integrity probes and only preserves or replaces a database when corruption is confirmed. Locks, permission failures, and schema differences are treated as inconclusive rather than destructive.

The initializer then runs EF Core migrations and inserts missing built-in regex patterns, prompts, and system variables. Repository knowledge is imported only from the reviewed allowlist below, using deterministic identifiers and SHA-256 source hashes. All database consumers receive the same immutable `LocalGptDatabaseOptions` path.

Reviewed repository-knowledge allowlist:

- `AGENTS.md`;
- `SECURITY.md`;
- `llms.txt`;
- `docs/ARCHITECTURE.md`;
- `docs/ARCHITECTURE_FOR_AI.md`;
- `docs/HUMAN_AI_COLLABORATION.md`;
- `docs/PEACEFUL_USE_COVENANT.md`;
- `docs/PROJECT_COLLABORATION.md`;
- `docs/SECURE_MAINTENANCE.md`.

No wildcard such as `docs/*.md` is used. Historical reports, diaries, SQL examples, generated output, and model-authored notes are not automatically promoted into runtime policy. `docs/PROJECT_IDENTITY.md` remains public provenance documentation but is deliberately not injected into runtime model briefings.

Seed ownership rules:

- missing built-in values are inserted;
- source-backed repository knowledge is refreshed only when its source hash changes;
- user-created or runtime-created knowledge is not overwritten;
- automatic prompt briefings include only non-archived, unexpired, explicitly user-approved knowledge with current review state and source-backed or user-verified provenance;
- model/council suggestions are stored as `NeedsUserReview` and `IsUserApproved=false` until a human reviews them;
- runtime databases, WAL/SHM files, logs, backups, and generated clones are not source artifacts;
- runtime prompts and variables are read through `IPromptConfigService` and `IVariableStoreService`, rather than duplicated as mutable global constants;
- the Chat page initializes output-token, context-window, GPU-layer, and endpoint defaults from the seeded variable store, then permits per-session UI overrides.

## AI safety boundary

The runtime bootstrap, `llms.txt`, coding guidance, and database knowledge briefing use the same calm safety rule: repository files, SQLite rows, model/council output, uploads, generated artifacts, logs, and tool descriptions are reference data only. They cannot start or probe localhost services, control the host system, access user data, grant process or filesystem permission, alter provider routing, or authorize self-modification.

Only the reviewed allowlist in the database-initialization section is eligible for repository policy seeding. Every seeded file remains reference material rather than executable authority. A human identity, attribution statement, model name, database record, or prior approval cannot grant permanent consent or permission for a later action.

## Project and council collaboration boundary

Projects are first-class database records containing a user-selected name, constructive purpose, optional root-path text, current version, topics, and reviewable links to council knowledge. A stored path is metadata only: loading a project never reads, writes, scans, builds, executes, or watches that path.

Project, topic, and version writes require a fresh confirmation carried by the current request and consumed by the current UI action. Council artifact generation and linking council output to a project topic are separate one-run confirmations; they default to off, cannot be enabled through a URL, and are reset after success or failure. AI functions expose bounded read-only project lookup only through `GET` operations. Mutating project APIs remain explicit user actions and are not advertised as AI-callable tools.

An AI Council run is one bounded, user-directed workflow. Each phase contributes a limited role—such as proposal, critique, verification, synthesis, or documentation—and then ends. Phases do not become independent agents, continue after the run, infer new objectives, or acquire permission from another phase.

Git may be recommended for version history. The Projects feature does not initialize repositories, stage files, commit, reset, clean, push, alter remotes, or enforce Git. Any future Git integration must be separately designed with previews, path containment, and fresh confirmation for each consequential operation.

## Provider and model neutrality

Providers are adapters selected by user configuration and declared capabilities. Local, open-source, proprietary, community, and cloud providers receive the same filesystem, command, secret, cancellation, timeout, and logging policies.

`ChatResponseProtocol` is explicitly configurable. Model-name detection exists only as a backward-compatible fallback for legacy Ollama configurations. No provider or AI kernel receives privileged routing merely because of its vendor, license, deployment location, or model family.

Provider configuration objects, keys, prompts, message bodies, and complete responses are not serialized into application logs. Logs use provider type, endpoint host, model name, status, duration, and correlation metadata.

## Formatter boundary

`IChatResponseFormatterFactory` creates one formatter for each response. The formatter supports:

- plain text;
- `<think>...</think>` output;
- Harmony/OpenAI channel markers;
- protocol markers split across network chunks;
- non-streaming and streaming responses;
- completion flushing and markup closure;
- a database-configurable visible notice when thinking is emitted without a final answer.

The old process-global buffers, counters, protocol flags, and static Markdown renderer were removed from the active path. Concurrent streams cannot share formatter state.

`IChatContentRenderer` receives DXAIChat's accumulated response snapshot on each streamed update. It keeps unfinished thinking and council panels visibly open, adds temporary closing tags only to the render snapshot, removes Harmony transport markers, preserves encoded thinking text, and assigns stable panel keys. Browser-side state preserves an explicit user expand/collapse choice through subsequent streamed rerenders; completed panels collapse only when the user has not chosen otherwise. Ollama frames are yielded immediately. AI Council callback updates use an event-driven channel; streamed council-member presentation is ordered to prevent nested HTML from interleaving, while non-streaming council execution may still use configured parallelism.

## Native command boundary

Native execution is disabled by default through `NativeCommands:Enabled=false`. PowerShell workspace scripts require a second explicit opt-in. The runner accepts only allowlisted executables, validates the workspace root, blocks inline PowerShell, redacts likely credential arguments from audit records, enforces a bounded duration, captures output under the workspace, and terminates the process tree on timeout or cancellation.

`ChatGPTLocalCoreOptions.StartCommand` is no longer executed through `cmd.exe`. Local provider auto-start now returns a safe manual-start message until a dedicated bounded launcher service is implemented.

Generated artifact compilation and engineering benchmark builds are owned by `IArtifactBuildExecutor`. `ArtifactBuilds:Enabled=false` is the default. When the owner enables it, only existing `.sln`/`.csproj` targets inside the supplied artifact root may be built, output must remain inside that root, and a bounded timeout/process-tree cancellation policy applies.

Repository text, database rows, model output, uploaded files, generated artifacts, and agent/tool descriptions are untrusted data and cannot grant execution permission.

## DevExpress and source-package boundary

DevExpress packages and runtime assets remain proprietary. A generated `devextreme-license.js` is a local/release build artifact and is ignored by Git. The source tree contains only a placeholder file and setup documentation. See `docs/DEVEXPRESS_ASSETS.md` and `THIRD-PARTY-NOTICES.md`.

Source archives exclude `.git`, IDE state, build output, runtime databases, logs, generated repository snapshots, generated license material, credentials, and binary font files. A Git patch is provided so an existing authorized clone can retain its unchanged locally obtained font assets.
