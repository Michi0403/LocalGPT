# LocalGPT v0.1.0-rewire


## Repository recovery

- selected outer commit `35b5fcf53681129d39e461e41ef3d2a691679a25` as the newest coherent base;
- compared the outer and nested source trees after line-ending normalization and preserved the owner-authored knowledge/security documents;
- removed the duplicated nested repository, temporary clones, IDE caches, logs, generated snapshots, and tracked runtime SQLite databases from the deliverable;
- added `.gitattributes` and stronger ignore rules for build/runtime/tool state;
- retained the canonical Git history and produced a separate patch against the selected base.

## Architecture

- added `docs/ARCHITECTURE.md`, `RELEASE.md`, `VALIDATION.md`, `REPAIR_REPORT.md`, and consolidated third-party notices;
- formalized the modular-monolith boundaries used by the owner’s BlazorPublisher architecture and the host/core/client/wrapper wiring visible in TacosPortalOpen;
- classified `Extensions/PlainStatics` as a legacy compatibility area;
- moved mutable state, configuration ownership, provider protocol selection, database initialization, and command policy behind services;
- moved Ollama HTTP wire DTOs and command-policy DTOs out of the global-static container.

## Formatter repair

- replaced shared static response buffers, counters, and Harmony flags with one formatter instance per response;
- added explicit `Auto`, `PlainText`, `ThinkTags`, and `Harmony` protocol selection;
- repaired tags and protocol markers split across streaming chunks;
- added completion flushing, safe thinking markup closure, plain-text fallback, and thinking-without-final handling;
- made the missing-final notice database-configurable;
- routed both streaming and non-streaming Ollama responses through the same formatter boundary;
- added `IChatContentRenderer` so incomplete thinking is expanded and rendered on every DXAIChat stream update, then collapses when final text begins;
- replaced the AI Council's two-second queue polling with an event-driven channel and a bounded heartbeat;
- serialized streamed council-member presentation to prevent parallel HTML fragments from crossing, while non-streaming runs still honor configured model parallelism;
- added per-panel completion markers so finished council panels automatically lose their live/open state.

## Persistence and initial database feed

- added one hosted, idempotent migration/initial-feed service;
- removed the delayed reflection migration service and remaining per-operation `EnsureCreated` path;
- seeded regex patterns, runtime prompts, system variables, architecture/security documentation, and repository knowledge deterministically;
- made runtime decision policy, Harmony instructions, missing-final notice, and output/context/GPU/endpoint defaults database-backed;
- wired the Chat page to consume the seeded defaults through `IVariableStoreService`, retaining compiled values only as failure fallbacks;
- fixed prompt, regex, and variable lookups that used non-key values with `FindAsync` or compared a key to itself;
- made legacy duplicate prompt rows readable deterministically while leaving owner/user rows intact;
- moved database path resolution, integrity probes, conservative corruption recovery, migration, and re-seeding behind `LocalGptDatabaseOptions`, `IDatabaseFileHealthService`, and `IDatabaseInitializationService`;
- removed the old static SQLite recovery implementation and routed chat memory, knowledge, logs, and the table editor through the same configured database path.

## Provider neutrality and logging

- made response protocol selection explicit and provider-neutral;
- retained model-name heuristics only as a legacy compatibility fallback;
- allowed local OpenAI-compatible endpoints that do not require an API key;
- removed full provider-configuration, API-key, prompt, message, and response-body values from repaired logging paths;
- disabled SDK request/response content logging by default;
- reduced council failure logging to structured metadata.

## AI authority and knowledge safety

- rewrote `llms.txt` and Copilot repository instructions so repository/model/database text is reference data rather than executable authority;
- replaced the legacy bootstrap prompt with an owner-authority boundary that prevents LocalGPT or council members from impersonating the human user or granting permissions to one another;
- classified only current policy/architecture documents as owner-approved pinned seed knowledge; historical/experiment documents remain source-backed references;
- made seed trust metadata refresh even when document text is unchanged, so older databases lose stale approval flags deterministically.

## Native execution safety

- disabled native commands by default;
- added a second opt-in for PowerShell workspace scripts;
- blocked inline PowerShell and kept execution inside the configured Minecraft workspace root;
- added a configurable command timeout, process-tree termination, output capture, policy audit entries, and likely-secret argument redaction;
- removed unrestricted `cmd.exe /c StartCommand` provider launch behavior; local providers must be started manually until a bounded launcher exists;
- moved generated DLL and engineering benchmark compilation out of static/process-launch helpers into `IArtifactBuildExecutor`;
- disabled artifact builds by default and added root, target-extension, output-location, timeout, cancellation, and process-tree constraints;
- changed generated AI-host templates so native model-runner execution is opt-in rather than enabled by default.

## Licensing and source assets

- retained Apache-2.0 for project-owned code;
- documented proprietary DevExpress requirements and open-source dependencies;
- removed the tracked generated DevExtreme runtime-license script containing customer-linked metadata;
- added a safe placeholder and developer setup guidance;
- source ZIP packaging excludes generated license material and binary font files.
