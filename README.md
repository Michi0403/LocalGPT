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

## Optional organic 1-Wire integration and protocol package

LocalGPT works without PublisherStudio. The organic 1-Wire connection is an optional, local integration example: a second application can announce a compact identity and TCP endpoint through UDP, request a user-approved connection, exchange its complete capability directory over TCP, and expose bounded “eyes”, “hands” or generation functions to the AI Council. Discovery never grants authority. Every sensitive operation still passes the local permission policy and current human approval.

This is also a practical example of LocalGPT's organic adaptation model. A locally running application can describe a new capability, its input/output contract and its safety requirements. LocalGPT can then help generate an adapter, capability mapping and Council workflow for that user's own installation. Generated integration code remains reviewable source; it is not installed or executed merely because a model proposed it.

Runtime trust is equally optional. Each application creates its own random secret file only at runtime, and the user can create, rotate or delete it from the frontend to reset trust. Public pairing tickets and Authenticator MFA can establish time-limited reciprocal trust; private keys and MFA seeds never cross the wire. Once trusted, sensitive envelope content is encrypted and signed while routing metadata stays compact. The same JSON contract is available through HTTP/JSON endpoints for user-built gateways and constrained clients such as an ESP32. See `docs/ONEWIRE_RUNTIME_SECURITY_HTTP_JSON.md`.

The shared `LocalGPT.WireProtocolVersion` assembly is deliberately RID-neutral and has one source authority: this LocalGPT repository. LocalGPT itself may use the project reference while developing the contract or its generated NuGet package when proving release behavior. Consumer repositories, including PublisherStudio, use only `LocalGPT.WireProtocolVersion.2.1.0.nupkg` and must not keep a second Git-revisioned protocol project.

Place the package at:

```text
<repository>\packages\LocalGPT.WireProtocolVersion.2.1.0.nupkg
```

For normal local development:

```powershell
.\Build-LocalDevelopment.ps1 -Configuration Debug -Platform x64
```

To prove package mode locally:

```powershell
.\build\Publish-WireProtocolPackage.ps1 -Version 2.1.0
.\Build-LocalDevelopment.ps1 -Configuration Debug -Platform x64 -UseWireProtocolPackage
```

For all supported release RIDs:

```powershell
.\Build-Release.ps1 -Runtime all
```

`Build-Release.ps1` packs the protocol once without a runtime identifier, restores each application for its own RID, and publishes only the cross-platform LocalGPT application and installer for Linux/macOS. The Windows-only WinUI wrapper is optional (`-IncludeWindowsWrapper`) and is never allowed to break Linux or macOS publishing. The resulting `.nupkg` should be attached to the official LocalGPT GitHub release so PublisherStudio and other organic integrations can consume the exact same public contract.

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

Running the setup helper without arguments performs the preservation-first default install and update routine. On Windows it verifies or installs Ollama, checks the Slim minimal model set, restores the maintained shortcuts, installs or updates LocalGPT, and starts the application without deleting the existing LocalAppData installation. Destructive deletion requires an explicit uninstall/`--force-delete` command. Downloads and archive extraction fail closed when a platform asset or safe extraction path cannot be verified. Uninstall removes application files, launchers, and shortcuts but preserves the learning base, including forced uninstall.

## Logging integrity guardrail

Logging is not removed as “cleanup”. Structured service/controller diagnostics, exception logging and expected-cancellation handling are protected by `build/Assert-LoggingIntegrity.ps1` and `build/logging-baseline.json`. The baseline is monotonic: a refactor may add diagnostics, but silently reducing existing logger references, log calls or catch/log boundaries fails the dedicated CI workflow. The same guard runs from the provided development/release scripts and from direct Windows MSBuild/Visual Studio application builds. See `docs/LOGGING_INTEGRITY.md`.

## License

LocalGPT source is licensed under Apache License 2.0 unless a file states otherwise. Third-party components retain their own licenses. DevExpress is proprietary and is not redistributed by this repository. See `LICENSE.MD` and `THIRD-PARTY-NOTICES.md`.
