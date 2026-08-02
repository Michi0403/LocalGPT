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

## LocalGPT 2.1.11 explicit async continuation control

Every await expression is now explicit and reviewable. Services, controllers, persistence, diagnostics, network operations, background workflows, and component methods that do not require renderer affinity use `ConfigureAwait(false)`. The only explicit `ConfigureAwait(true)` sites are inside `OnAfterRenderAsync`, where the continuation must return to the Blazor renderer before lifecycle-owned UI state is changed.

The async architecture audit no longer uses broad per-file allowances. It rejects unconfigured await expressions, rejects `ConfigureAwait(true)` outside `OnAfterRenderAsync`, and rejects `ConfigureAwait(false)` inside that renderer-affine lifecycle method. `await using` remains the language-level asynchronous-disposal construct; an awaited initializer inside it is still explicitly configured. Existing `ConfiguredTaskAwaitable` values remain valid because their continuation policy was already selected by the caller.
