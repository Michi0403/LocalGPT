# LocalGPT

LocalGPT is a local, human-guided AI council and Blazor application created by **Michael Fleischer (Michi0403)**. It combines local and configured cloud model providers, streaming chat, structured knowledge, diagnostics, and reviewable artifact generation.

## Maintainer and development assistance

LocalGPT is created and maintained by **Michael Fleischer (Michi0403)**. Architecture decisions, releases, testing, and project responsibility remain with the maintainer.

ChatGPT, `gpt-oss-20b`, and other AI systems have been used as development assistants for research, architecture review, debugging, test design, and documentation. This acknowledgement does not imply ownership, vendor endorsement, autonomous authority, or responsibility for a release. Every accepted change is reviewed and built by the human maintainer.


## Current maturity

LocalGPT is an active independent project. The application, installer workflow, AI Council, project memory, DXAIFunction catalog, and PublisherStudio 1-Wire bridge are implemented, but release candidates still require maintainer-side .NET builds and runtime validation on each target platform. Claims should be evaluated through the source, included architecture documents, and reproducible demonstrations rather than benchmark-free superlatives.

## Human-guided by design

LocalGPT is a bridge for human–AI coworking, not an unattended coding agent.

- The current human request defines the task.
- Thinking and answer text stream dynamically to the frontend.
- Suggestions, music, hobbies, learning, and other harmless creative work are welcome when requested.
- When no task is active, LocalGPT remains idle.
- Commands, builds, downloads, installation, deletion, publication, credentials, networking, localhost control, and other consequential actions require fresh, specific human confirmation.
- Only explicitly human-approved knowledge can enter automatic prompt briefings.

See `docs/HUMAN_AI_COLLABORATION.md` and `SECURITY.md`.


## Peaceful and constructive use

LocalGPT is intended for constructive cooperation: business software, public and private infrastructure, hospitals, schools, accessibility, children’s learning, music, art, lawful research, electronics, ESP/PCB work, assistive devices, and other positive projects. It must not be used for war, killing, destruction, coercion, sabotage, abuse, or autonomous harmful action. Safety-critical medical, biological, electrical, and physical work remains under qualified human supervision and applicable safeguards.

## Project cooperation

The database-backed Projects area stores a user-selected purpose, optional path text, versions, topics, and links to reviewed AI Council knowledge. Selecting a project gives the council bounded context; it does not authorize file access or execution. Git may be recommended for revision history, but LocalGPT does not initialize, commit, reset, clean, push, or enforce Git through this feature.

Each AI Council phase is a bounded contribution—such as proposal, critique, verification, synthesis, or documentation—inside one user-directed run. It is not an autonomous agent or continuing mission.

See `docs/PEACEFUL_USE_COVENANT.md` and `docs/PROJECT_COLLABORATION.md`.

## Architecture

LocalGPT uses an updated service-oriented .NET structure:

- Blazor/DevExpress UI and controllers depend on interfaces.
- Application behavior lives in scoped or singleton services according to state ownership.
- EF Core/SQLite services own migration, recovery, deterministic seed data, and knowledge lifecycle.
- Provider integrations remain behind provider-neutral contracts.
- Stateful response formatters are per stream and preserve incremental thinking/final rendering.
- Native commands and artifact compilation are bounded, disabled by default, and human-confirmed.

Detailed architecture is in `docs/ARCHITECTURE.md`.

## Reviewed generation and DXAIFunctions

LocalGPT discovers intentionally exposed `IDxAiFunctionHandler` implementations through dependency injection. Local models may automatically call only bounded read-only functions. Mutating functions remain discoverable but require a fresh user decision.

When the Council proposes source, scripts, addons, DLLs, executables, or solutions, it creates a database-backed change review first. The UI displays the exact paths, hashes, CodeDOM types, output targets, safety summary, and current-project context. Generation is one-use and bound to that review hash; an optional bounded build requires a second current confirmation. Generated programs and addons are never executed or loaded automatically.

See `docs/DXAI_FUNCTIONS_AND_CHANGE_REVIEWS.md`.

## Security and CVEs

Security work is cooperative: confirm advisories, contain exposure, patch or replace affected dependencies, document the decision, and validate the result. Never exploit a CVE, scan unrelated systems, bypass permissions, publish sensitive payloads, or suppress an audit merely to make the build green.

NuGet audit covers direct and transitive dependencies. High and critical advisories block owner-side builds. See `docs/SECURE_MAINTENANCE.md`.

## Build requirements

- .NET SDK specified by `global.json`
- Windows workloads/Windows App SDK for the desktop wrapper
- a valid DevExpress license and package feed for DevExpress components
- optional Ollama or another configured model provider for local inference

This source package intentionally excludes build output, IDE state, runtime databases, logs, secrets, certificates, private feed credentials, generated DevExpress licensing material, and proprietary dependency binaries.

## Installer safety

Running the setup helper without arguments shows help and performs no installation. Destructive replacement or uninstall requires explicit `--force-delete`. Review target paths before confirming. Downloads and archive extraction fail closed when a platform asset or safe extraction path cannot be verified. Uninstall removes application files, launchers, and shortcuts but preserves the learning base, including forced uninstall.

## License

LocalGPT source is licensed under Apache License 2.0 unless a file states otherwise. Third-party components retain their own licenses. DevExpress is proprietary and is not redistributed by this repository. See `LICENSE.MD` and `THIRD-PARTY-NOTICES.md`.
