<div align="center">

# LocalGPT

### Garage-built for personal use. Open for everyone. Powerful enough for much more.

A local-first .NET and Blazor platform for AI councils, model orchestration, persistent knowledge, project workflows, human participation, and practical experimentation with local AI.

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE.MD)
[![Latest release](https://img.shields.io/github/v/release/Michi0403/LocalGPT?display_name=tag&sort=semver)](https://github.com/Michi0403/LocalGPT/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Local first](https://img.shields.io/badge/AI-local--first-2f855a)](#local-first-by-design)

[Latest release](https://github.com/Michi0403/LocalGPT/releases/latest) · [Architecture](docs/ARCHITECTURE.md) · [Project stance](docs/PROJECT_STANCE.md) · [Security](SECURITY.md) · [Release process](docs/RELEASE_PROCESS.md)

</div>

---

> **Hero image placeholder**<br>
> Add a wide screenshot showing the LocalGPT chat, AI Council output, Council Teams editor, and Theme Fusion panel. Recommended size: `1600 × 900`, stored at `docs/images/readme/localgpt-hero.png`.

## What LocalGPT is

LocalGPT is an independent, garage-built project created and maintained by **Michael Fleischer (`Michi0403`)**. It is developed primarily as a serious personal tool, a shared workshop for friends, and a public engineering project—not as a conventional commercial product.

The project combines a local AI runtime with a database-backed .NET application, a DevExpress Blazor interface, configurable council teams, persistent project context, human collaboration checkpoints, bounded tool access, diagnostics, installers, and release tooling.

The goal is simple: build an AI environment that is useful, expressive, inspectable, and enjoyable to work with. The fact that the same architecture can support larger facilities, internal tools, laboratories, or company-specific platforms is a welcome side effect rather than the product pitch.

## Project stance

LocalGPT is intentionally honest about what it is:

- **One independent maintainer, not a company.** There is no sales department, support contract, roadmap guarantee, or corporate polish layer.
- **Built for real use rather than demonstrations.** The repository favors complete workflows, diagnostics, migrations, orchestration, recovery, and operational boundaries over isolated sample snippets.
- **Public by default.** Development moves quickly, and reviewed local work is commonly merged and committed soon afterward. Fixed version numbers in documentation can become stale quickly; use the latest release and current commit history as the source of truth.
- **Not optimized for every regular user.** Mass-market onboarding, broad compatibility promises, and feature requests from unknown users are not the primary design drivers.
- **Open for serious adaptation.** Individuals, research groups, and companies are welcome to study, fork, integrate, and extend the project under the Apache License 2.0.

Read the full position in [`docs/PROJECT_STANCE.md`](docs/PROJECT_STANCE.md).

## Highlights

### AI Council and role-driven workflows

Create teams of models with explicit roles, execution limits, workflow phases, human participation modes, bounded repetition, and database-backed configuration. Council output is streamed and preserved so the reasoning process, failures, recoveries, and final synthesis remain inspectable.

### Human participation as a first-class role

Roles can be AI-only, optionally human-assisted, require a human checkpoint, or be human-only. Human input is recorded as a peer contribution rather than silently treated as unquestionable truth.

### Local-first by design

LocalGPT is built around local models and local persistence. Provider adapters remain explicit, and no provider gains extra authority because of its vendor, license, model family, or hosting location.

### Database-backed memory and project context

Projects, knowledge, workflows, collaboration requests, role configuration, chat history, diagnostics, and migration state are represented through explicit services and persistence boundaries rather than hidden process-global state.

### Real .NET and DevExpress integration

The repository contains end-to-end C# and Blazor patterns for streaming AI responses, Interactive Server islands, DevExpress components, EF Core migrations, WebView2 packaging, diagnostics, state recovery, build-time architecture guards, and local installer flows.

Official samples usually isolate one feature. LocalGPT shows what happens when those features must coexist inside one working application.

### Safety through explicit boundaries

Native commands, artifact builds, filesystem access, approvals, provider launches, and consequential operations are guarded by dedicated services, opt-ins, bounded contexts, logging, and confirmation rules. Repository text and model output are treated as untrusted data—not as permission.

### A frontend meant to be used

LocalGPT treats the UI as part of the system rather than a thin administrative layer. Wide work surfaces, streamed council panels, persistent state, theme fusion, diagnostics, collaboration controls, and configurable editors are intended to make complex local AI work comfortable.

## Interface gallery

Screenshot naming, sizing, and privacy guidance is maintained in [`docs/README_IMAGE_PLAN.md`](docs/README_IMAGE_PLAN.md).

<table>
  <tr>
    <td width="50%" align="center">
      <strong>Chat and streamed model output</strong><br><br>
      <em>Image placeholder</em><br>
      Suggested file: <code>docs/images/readme/chat-streaming.png</code>
    </td>
    <td width="50%" align="center">
      <strong>Council Teams and workflow editor</strong><br><br>
      <em>Image placeholder</em><br>
      Suggested file: <code>docs/images/readme/council-teams.png</code>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <strong>Human collaboration and approvals</strong><br><br>
      <em>Image placeholder</em><br>
      Suggested file: <code>docs/images/readme/human-collaboration.png</code>
    </td>
    <td width="50%" align="center">
      <strong>Theme Fusion and wide work panels</strong><br><br>
      <em>Image placeholder</em><br>
      Suggested file: <code>docs/images/readme/theme-fusion.png</code>
    </td>
  </tr>
</table>

## Quick start

### Recommended: use the latest release

1. Open the [latest GitHub release](https://github.com/Michi0403/LocalGPT/releases/latest).
2. Download the appropriate installer or packaged application for your platform.
3. Follow the included release notes and installer guidance.
4. Connect a supported model runtime such as Ollama and begin with the main Chat or Council Teams pages.

The installer supports commands such as:

```powershell
localgpt-setup --install-ollama
localgpt-setup --pull-models --range Slim
localgpt-setup --install-localgpt --force
localgpt-setup --import-recommended --force
```

See [`LocalGPTWebviewWrapper/LocalGPTInstallerConsole/README.md`](LocalGPTWebviewWrapper/LocalGPTInstallerConsole/README.md) for installer details.

### Build from source

Local development currently targets the pinned .NET SDK and requires the authorized DevExpress package feed/assets used by the project. The optional Windows desktop wrapper also requires the relevant Windows App SDK and WebView2 prerequisites.

```powershell
.\Build-LocalDevelopment.ps1 -Configuration Debug -Platform x64
```

Before publishing or creating a verified source package, follow the repository validation and release process rather than improvising a manual package:

```powershell
.\build\Invoke-RepositoryValidation.ps1
.\build\New-VerifiedSourcePackage.ps1 -Version "<version>"
```

See [`docs/RELEASE_PROCESS.md`](docs/RELEASE_PROCESS.md) and [`VALIDATION.md`](VALIDATION.md).

## Architecture at a glance

```text
Blazor UI / controllers / endpoints
                │
         explicit interfaces
                │
application, council, provider, formatting,
persistence, collaboration and safety services
                │
EF Core / SQLite / HTTP / bounded filesystem access
```

LocalGPT is a modular monolith with explicit ownership boundaries:

- **LocalGPT** — ASP.NET Core loopback host and Interactive Blazor Server application.
- **LocalGPTInstallerConsole** — installation and bootstrap helper.
- **LocalGPTWebviewWrapper** — optional Windows desktop shell.
- **DevExpress integration** — frontend and integration boundary, not the owner of domain state.

Start with [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/ARCHITECTURE_FOR_AI.md`](docs/ARCHITECTURE_FOR_AI.md).

## For companies and downstream adopters

Commercial and institutional adaptation is welcome under the terms of the Apache License 2.0. You do not need to turn the upstream project into your product, wait for enterprise features, or ask the maintainer to become your support organization.

A downstream adopter can:

- fork the repository and establish its own release discipline;
- replace or extend provider adapters;
- add organization-specific workflows, permissions, persistence, and branding;
- build internal facilities or domain-specific tools on the architecture;
- contribute generally useful fixes upstream without transferring product responsibility to the maintainer.

LocalGPT does not promise production suitability for a specific organization. Adopters are responsible for their own security review, licensing, validation, deployment, compliance, support, and operational guarantees.

For company use, the LocalGPT team strongly recommends a **DMZ-style isolation procedure**: place LocalGPT and its model/runtime services in a segmented network zone, restrict inbound and outbound traffic with operating-system and perimeter firewall rules, allow only required loopback or explicitly approved endpoints, and run the application under a dedicated least-privilege operating-system account that cannot delete or modify unrelated files. Treat imported repositories, webpages, model output, generated code, and tool requests as untrusted until reviewed.

## Contributions and support expectations

Issues, technical discussion, documentation improvements, and focused pull requests may be useful, but acceptance, response time, compatibility work, and implementation are not guaranteed.

The maintainer may prioritize personal use, friends, architectural experiments, current research interests, or major internal changes over general-user requests. Forking and adapting the project is an expected and encouraged path—not a failure of upstream support.

## Donations and public good

LocalGPT is free and open source. Nothing is expected in return, and a donation must never be interpreted as a purchase, subscription, support contract, feature entitlement, or influence over the roadmap.

A donation option may be added later. The preferred direction is to support selected foundations, research, educational, humanitarian, or other public-interest institutions—potentially through direct links—rather than turning LocalGPT into a commercial obligation.

Until such a mechanism is explicitly published, there is no official donation request.

## Documentation

| Area | Document |
|---|---|
| Architecture | [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) |
| Project stance | [`docs/PROJECT_STANCE.md`](docs/PROJECT_STANCE.md) |
| Security | [`SECURITY.md`](SECURITY.md) |
| Human–AI collaboration | [`docs/HUMAN_AI_COLLABORATION.md`](docs/HUMAN_AI_COLLABORATION.md) |
| Council blueprint | [`docs/ORGANIC_AI_COUNCIL_BLUEPRINT_2_1.md`](docs/ORGANIC_AI_COUNCIL_BLUEPRINT_2_1.md) |
| Frontend patterns | [`docs/FRONTEND_DESIGN_PATTERN_LIBRARY.md`](docs/FRONTEND_DESIGN_PATTERN_LIBRARY.md) |
| Diagnostics | [`docs/LOGGING_INTEGRITY.md`](docs/LOGGING_INTEGRITY.md) |
| DevExpress assets | [`docs/DEVEXPRESS_ASSETS.md`](docs/DEVEXPRESS_ASSETS.md) |
| Release process | [`docs/RELEASE_PROCESS.md`](docs/RELEASE_PROCESS.md) |
| Open tasks | [`docs/OPEN_TASKS.md`](docs/OPEN_TASKS.md) |

## License and third-party components

LocalGPT is released under the [Apache License 2.0](LICENSE.MD). Commercial use, modification, distribution, and private adaptation are welcome under that license's terms.

DevExpress packages and runtime assets remain proprietary and are governed by their own licenses. Generated license material must not be committed. See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) and [`docs/DEVEXPRESS_ASSETS.md`](docs/DEVEXPRESS_ASSETS.md).

## Provenance and acknowledgments

LocalGPT was created and is maintained by **Michael Fleischer (`Michi0403`)**.

The project also reflects repeated co-development sessions with OpenAI's ChatGPT, early foundational progress made possible by `gpt-oss-20b`, and many LocalGPT-generated reviews and missing-feature reports that were inspected and used as engineering input. Assistance is acknowledged openly; design decisions, repository ownership, release decisions, and responsibility remain with the maintainer.

See [`docs/PROJECT_IDENTITY.md`](docs/PROJECT_IDENTITY.md).

---

<div align="center">

**Built as a workshop, not operated as a store.**<br>
Use it, inspect it, fork it, improve it, or build something entirely different from it.

</div>

## LocalGPT 2.1.9 Adaptive benchmark wiring compile fix

Version 2.1.9 adds the missing `LocalGPT.Interfaces` import to `AdaptiveOllamaBenchmarkWiring`, allowing its `IDxAiFunctionHandler` contract to resolve during compilation. The empirical Ollama autotuner remains an explicit `NotImplementedException` boundary until it can be validated on real target hardware. The independently versioned `LocalGPT.WireProtocolVersion` package remains unchanged.

## LocalGPT 2.1.8 version alignment

Version 2.1.8 raises the LocalGPT application, runtime context, organic 1-Wire application advertisement, and seeded LocalGPT Core project metadata to the same release number. The independently versioned `LocalGPT.WireProtocolVersion` package remains unchanged.

## LocalGPT 2.0.4 build-policy and diagnostics corrections

Version 2.0.4 keeps the in-chat game and remote-import features from 2.0.3 while correcting the repository's maintained architecture boundaries: renderer-affine Razor awaits are explicit, text splitting remains service-owned, newly introduced runtime/diagnostics data models live under `BusinessObjects`, and method diagnostics no longer take disposal ownership from dependency injection. Database seed reconciliation now keeps concurrent user/database values authoritative while preserving unrelated additive seed records.

## LocalGPT 2.0.3 in-chat runtime games and knowledge imports

The Council Team editor includes database-backed runtime classes and categorized best-use DXFunctions. `/Chat` now keeps one persistent ASCII game surface directly beside the conversation, with the same bounded control contract for humans and AI players:

- **ASCII corridor Council Adventure** — a reactive, original 2.5D-style terminal simulation. A deterministic frame appears immediately, one Council turn advances one meaningful world step, and exactly one AI member may own each complete ASCII frame.
- **Green Dragon Runtime Story** — locations, houses, NPCs, events, player state, and scene frames are separate bounded runtime-class instances coordinated by directors.

The Chat surface supports keyboard, touch, gamepad, shared human/AI control, delayed AI autoplay, and fullscreen layouts suitable for desktop, tablet, and handheld devices. GitHub repositories and public webpages can be inspected from Test Lab, filtered by the database-backed source-file regex policy, reviewed as an exact returned-file list, and then passed through the existing Learn-Base importer after explicit approval. Council members receive matching inspect/import DXFunctions.

These are fan-made configuration and architecture studies. LocalGPT is not affiliated with id Software, ZeniMax, Bethesda, LOTGD, or their contributors, does not ship commercial game data, WADs, trademarks, or the original game engine, and does not claim that these examples are official versions of any game.

See [`docs/ASCII_RUNTIME_GAME_PRESETS.md`](docs/ASCII_RUNTIME_GAME_PRESETS.md) for the frame contract, role ownership, optional learning sources, and runtime-field behavior.

## LocalGPT 2.1.10 scoped diagnostics and adaptive Ollama benchmark

Version 2.1.10 prevents the method-diagnostics decorator from wrapping singleton registrations, so a singleton proxy can no longer attempt to resolve scoped services from the root provider. `IRegexPatternService` is registered as a singleton because its implementation is stateless and creates short-lived database contexts through `IDbContextFactory`.

`localgpt.models.benchmark.autotune` is now an implemented, human-confirmed DXFunction. It benchmarks only models already installed in the configured loopback Ollama runtime, uses bounded deterministic and optional peer-authored tasks, stops profile tuning when improvement falls below the chosen threshold, and can save a new model preset without silently replacing an existing preset.

Version 2.1.10 initially removed explicit `ConfigureAwait(true)` captures and left renderer-owned component awaits implicit. Version 2.1.11 supersedes that continuation policy with explicit configuration on every await expression.

## LocalGPT 2.1.11 explicit async continuation control (superseded)

Version 2.1.11 made ordinary await expressions explicit and initially limited `ConfigureAwait(true)` to `OnAfterRenderAsync`. That rule was too narrow for renderer-owned initialization and parameter-loading chains. Version 2.1.12 supersedes it with exact lifecycle and individually reviewed helper continuations while retaining `ConfigureAwait(false)` as the application-wide default.

`await using` remains the language-level asynchronous-disposal construct; awaited resource initializers are still explicitly configured. Existing `ConfiguredTaskAwaitable` values remain valid because their continuation policy was already selected by the caller.

## LocalGPT 2.1.12 compiler and renderer-continuation corrections

Version 2.1.12 fixes the `ConfigurationRoot` type-name collision, restores project DXFunction parameter binding to `request.Parameters`, and repairs the Windows PowerShell async-policy fallback. The continuation audit now reads Razor `@code` blocks correctly and requires every ordinary await to choose a continuation policy explicitly.

Context-free service, controller, persistence, networking, diagnostics, and background operations use `ConfigureAwait(false)`. Blazor lifecycle entry points may use `ConfigureAwait(true)`. Additional HTTP, SignalR, and object-domain loading helpers retain the renderer only when their exact file and method name is listed in `build/async-continuation-baseline.json` and their continuation directly applies loaded state to component fields. Background probes that marshal their final state through `InvokeAsync` remain context-free.

## LocalGPT 2.1.13 compile recovery and database-logger startup isolation

Version 2.1.13 changes `RunRemoteKnowledgeAsync` into an `async Task` method and awaits the configured `RunUiActionAsync` awaitable. This resolves `CS0029` without weakening the repository's explicit continuation policy. Once LocalGPT builds and emits `LocalGPT.dll`, the desktop wrapper's cascading `WMC1006`/`CS0006` missing-metadata errors disappear as well.

The database-backed logger now holds startup entries in its existing bounded channel until database health checks, migration, and deterministic seeding have completed. This prevents logger `SaveChangesAsync` calls from racing schema creation or seed writes. Console, debug, file, and email providers remain available during initialization, and queued database entries flush after the one-way readiness gate opens.

The adaptive Ollama benchmark/autotune implementation is unchanged in this patch release. The independently versioned `LocalGPT.WireProtocolVersion` package remains at 2.1.0.


## LocalGPT 2.1.15 embedded workbench build correction

Version 2.1.15 corrects the first Windows/.NET build findings from the 2.1.14 embedded-workbench release. `EmbeddedHardwareCatalogService` now returns concrete read-only lists instead of introducing new `yield` iterators that violate the repository's logged try/finally iterator policy. `EmbeddedFirmwarePlanningService` now builds Arduino sketches, PlatformIO configuration and wiring Markdown through explicit `StringBuilder` output instead of malformed interpolated multiline raw strings.

No embedded capability, controller route, DXFunction, workspace model, migration or safety boundary was removed. The release is a compile/policy correction over 2.1.14. See `CHANGELOG-v2.1.15-embedded-build-correction.md`.

## LocalGPT 2.1.14 embedded workbench contracts and workspace environments

Version 2.1.14 adds a Chat-first ESP32/Arduino planning slice. The AI Council can use source-controlled board profiles and transport-neutral protocol descriptors to propose GPIO assignments, validate a wiring graph, generate a small reviewable Arduino/PlatformIO artifact set, preview an edge telemetry packet and explain the optional protected LocalGPT logical 1-Wire bridge. Physical 1-Wire is treated as one possible sensor bus rather than a mandatory architecture.

The same release prepares PublisherStudio integration through a canvas-neutral wiring draft with board/pin nodes, wire connections, OpenSCAD part keys and signal-animation metadata. It also extends Project Maintenance with workspace-local environments, compiler assignment, build arguments, environment variables, expected directories, structure regexes, access-policy regexes and approved/warning/danger rights assessment.

See `CHANGELOG-v2.1.14-embedded-workbench-and-workspace-environments.md` for the full API, DXFunction, organic capability, installer learning-source and safety boundary list.

## LocalGPT 2.1.16 workspace policy build correction

Version 2.1.16 resolves the Windows/.NET 10 compiler ambiguity in workspace access-policy evaluation. The policy matcher now uses an explicit single-argument lambda when applying `Regex.IsMatch` through LINQ, preserving the same bounded matching behavior while selecting the intended `Where(Func<string, bool>)` overload deterministically.

No embedded capability, workspace contract, controller route, DXFunction, migration or security boundary was changed. See `CHANGELOG-v2.1.16-workspace-policy-linq-correction.md`.


## LocalGPT 2.1.17 responsive workbench and customizable LearnBase

Version 2.1.17 completes the workspace access-policy compiler correction with the explicit string overload `EndsWith("/", StringComparison.Ordinal)`. LearnBase imports now expose editable known file endings, additional endings, include/exclude regexes, per-file size limits, and independent manifest/documentation/project-summary modes. The source profile includes modern .NET, Python, C/C++, Arduino/ESP32, device-tree, HDL, Fritzing text parts, KiCad, OpenSCAD, web and build formats; binary containers such as `.fzz` remain excluded from text parsing.

OneWire Security, Human-guided Projects and Project Maintenance now use responsive full-width grids modeled after the Chat surface. The ASCII game console is opt-in, opens larger, places its guide beside the frame when space permits, and supports three fullscreen modes: whole-frame fit, width fit with vertical scrolling, and native monospace size with scrolling. The project-owned corridor remains an original LocalGPT implementation; optional upstream DOOM source import is still separately attributed and does not bundle the original engine, WAD data or commercial assets.

See `CHANGELOG-v2.1.17-responsive-learnbase-and-ascii-layout.md`.

## LocalGPT 2.1.18 authoritative GameDirector, generated documentation and startup correction

Version 2.1.18 makes the GameDirector the final authority for every game-state transition. Human controllers, AI controllers, creature Councils and reactive map objects submit proposals; the session changes only after a turn-safe director decision. The HTTP and DXFunction surfaces include a read-only decision preview, and the runtime catalog now distinguishes director, creature and reactive-object contracts.

Existing core-project seed data is loaded without tracking and only missing child records are attached for insertion. This avoids the stale tracked-row `DbUpdateConcurrencyException` seen during startup while preserving database-owned project values.

Chat configuration and session-tool panels now expand into responsive viewport workspaces so dense DevExpress configuration remains usable at 100% browser zoom and on 4K displays. The former handwritten Help page now launches versioned DocFX HTML/API documentation generated from maintained articles and C# XML comments. Windows builds generate the HTML documentation and attempt `LocalGPT-2.1.18.pdf`; Release builds require the PDF unless explicitly overridden.

See `CHANGELOG-v2.1.18-gamedirector-docfx-startup-and-chat-layout.md`.

## LocalGPT 2.1.19 documentation build correction

Version 2.1.19 corrects the Windows command-line boundary between MSBuild and `Build-Documentation.ps1`. The repository-root argument no longer ends in a backslash inside quotes, so PowerShell receives `AssemblyPath`, `XmlDocumentationPath`, `Version`, and `OutputWebRoot` as separate parameters. The script normalizes all paths before DocFX runs and emits a bounded input summary for diagnostics.

The existing XML-comment catalog, translator adapter, Help launcher, HTML/API documentation and versioned PDF behavior remain unchanged. Successful PDF output is named `LocalGPT-2.1.19.pdf`.

See `CHANGELOG-v2.1.19-docfx-argument-correction.md`.

## LocalGPT 2.1.20 onboarding, development councils and canonical chat rendering

Version 2.1.20 adds a persisted first-run guide that points users to the installer, generated documentation, the editable Council-team catalog and direct Chat quick starts. Source-controlled seed data now includes an adaptive installed-model benchmark Council, a low-latency GameDirector Council, and development teams for modern hosted C#, PowerShell build automation, Java services and Minecraft projects. Their rounds follow the repository's maintained order: preflight and regex discovery, architecture, bounded implementation, policy audit, build/test evidence, independent curation and release synthesis.

Council prompt reconstruction now keeps unique recent user turns plus only the latest cleaned assistant consensus. LocalGPT-owned process/thinking panels and accidentally nested Council wrappers are removed before history is sent to a model. Provider-owned HTML is encoded while Markdown remains available to the common renderer, so Harmony, think-tag and plain-text models use the same heading, list, table and line-break path without being able to forge LocalGPT disclosure panels.

The documentation build unblocks repository-local DocFX inputs before restore, can reuse or install an isolated DocFX tool, and no longer makes a diagnostic Debug build unusable merely because documentation tooling could not be restored. New and changed onboarding, Council, formatting and configuration contracts include XML summaries, primary-constructor parameter descriptions, property documentation, method parameter documentation and return/Task descriptions.

See `CHANGELOG-v2.1.20-onboarding-development-teams-and-chat-formatting.md`.

## LocalGPT 2.1.22 persistent setup and open localization

Version 2.1.22 keeps the benchmark/development onboarding surface permanently available under `/install`, even after it was marked reviewed. The installer also imports validated user localization JSON catalogs while English and German remain built-in fallbacks. Request localization accepts every culture known to the installed .NET runtime, and the global selector is generated from the catalogs actually installed for the current user.

The documentation build now stages referenced assemblies for DocFX metadata extraction and falls back to a compiler-XML Markdown API catalog when metadata extraction is unavailable. Diagnostic builds preserve the last generated documentation instead of failing after the application assembly already compiled.

See `CHANGELOG-v2.1.22-direct-council-starters-docfx-modals-shortcuts.md`.


## LocalGPT 2.2.4 Kawaii documentation gimmick and theme polish patch

Version 2.2.4 carries the documentation website corrections made after 2.2.2 under a new patch number. It activates the complete Kawaii DocFX shell in both light and dark modes, replaces the stock DocFX mark with LocalGPT cat branding, keeps reduced-motion support, validates generated API/PDF links without publishing placeholders, and deploys the exact documentation payload shipped in the release to GitHub Pages. Existing localization, Council, game, persistence, installer and application functionality remains intact. The separately versioned 1-Wire protocol remains at 2.1.0.

See `CHANGELOG-v2.2.4-docfx-kawaii-gimmick-followup.md`.

## LocalGPT 2.2.2 localization, game-layout, PDF and catalog follow-up release

Version 2.2.2 preserves the published 2.2.1 release and carries the subsequent maintenance corrections under a new patch number. It completes full-request culture switching, retains the native AI-provider selector, stabilizes the centered ASCII-game guide, resolves installed PDF discovery, makes DX-function catalog synchronization resilient to legacy unique rows, and keeps the generated GitHub Pages API route valid. The separately versioned 1-Wire protocol remains unchanged at 2.1.0.

See `CHANGELOG-v2.2.2-localization-game-pdf-catalog-followup.md`.

## LocalGPT 2.2.1 localization, documentation and provider-maintenance release

Version 2.2.1 restores language switching after Blazor becomes interactive, keeps installed DocFX content discoverable below the application directory, fixes the AI-provider selector inside the Chat configuration workspace, removes forbidden service statics and corrects the GitHub Pages API route. The separately versioned 1-Wire protocol remains unchanged.

See `CHANGELOG-v2.2.1-localization-docs-provider-maintenance.md`.

## LocalGPT 2.1.23 release-safe documentation, Council handoff, toolchains and durable feature data

Version 2.1.23 makes the documentation step release-safe without disabling it. DocFX metadata and HTML generation are still attempted first; when either path fails, the build script publishes deterministic HTML/API pages from maintained articles and compiler XML comments and creates a versioned dependency-free PDF index. Documentation failures are reported as build warnings rather than invalidating a successfully compiled application, and generated help files are copied into RID publish output after `Publish` completes.

Direct Council starters now switch to the Council session and submit their complete maintained prompt through the native composer/send path instead of treating a highlighted DevExpress suggestion as a completed submission. Regular quick prompts remain available and Council-specific cards are additive. The running-session workspace is content-sized while the already successful Chat configuration workspace keeps its large responsive layout.

The installer can explicitly discover, validate, save, select and remove MSBuild, .NET SDK, Java, Python, PowerShell, C/C++, PlatformIO and Arduino CLI installations from `PATH`, common folders and user-supplied roots. Saved language defaults are reused by workspace and build-verification configuration.

Durable records introduced for recent feature areas are now represented in EF Core with a migration, DbSets, indexes, foreign keys, logged services and approval-gated CRUD controllers: Council prompt starters, localization catalog registrations, documentation build evidence, embedded firmware plan envelopes and authoritative GameDirector session snapshots. Transient requests, renderer DTOs and calculated runtime projections remain deliberately outside EF Core.

See `CHANGELOG-v2.1.23-release-docs-council-toolchains-persistence.md`.
