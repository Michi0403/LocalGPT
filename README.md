<div align="center">

# LocalGPT

### Garage-built for personal use. Open for everyone. Powerful enough for much more.

A local-first .NET and Blazor platform for AI councils, model orchestration, persistent knowledge, project workflows, human participation, and practical experimentation with local AI.

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE.MD)
[![Latest release](https://img.shields.io/github/v/release/Michi0403/LocalGPT?display_name=tag&sort=semver)](https://github.com/Michi0403/LocalGPT/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Local first](https://img.shields.io/badge/AI-local--first-2f855a)](#local-first-by-design)

[Latest release](https://github.com/Michi0403/LocalGPT/releases/latest) · [Architecture](docs/architecture/system-overview.md) · [Project stance](docs/reference/design-evolution.md) · [Security](SECURITY.md) · [Release process](docs/engineering/release-and-docs.md)

</div>

---

<div align="center">

🌸 **[Open the live Kawaii documentation](https://michi0403.github.io/LocalGPT/)** · [User guide](docs/guide/index.md) · [Complete API reference](docs/api/index.md)

**Live project pages (visible URLs for mobile GitHub):**  
LocalGPT: <https://michi0403.github.io/LocalGPT/>  
PublisherStudio: <https://michi0403.github.io/BlazorPublisher/>

</div>

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

Read the full position in [`docs/reference/design-evolution.md`](docs/reference/design-evolution.md).

## Highlights

### AI Council and role-driven workflows

Create teams of models with explicit roles, execution limits, workflow phases, human participation modes, bounded repetition, revisable **X-Rounds**, and database-backed configuration. Council output is streamed and preserved so the reasoning process, failures, recoveries, live per-host participant activity, X-Round causal revisions, and final synthesis remain inspectable. X-Rounds can revisit an earlier step without deleting history, start a bounded single-model or child-Council subtask, return text to the parent workflow, and optionally require a local human gate.

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

## Explore the interface

The maintained guide describes the user-facing surfaces without depending on stale screenshots:

- [Chat and AI Council](docs/guide/chat-and-council.md)
- [Projects and workspaces](docs/guide/projects-and-workspaces.md)
- [Embedded planning and games](docs/guide/embedded-and-games.md)
- [Documentation inside LocalGPT](docs/guide/documentation.md)

The original screenshot naming, sizing, and privacy plan remains preserved for maintainers under `docs/internal-notes/legacy-source/top-level/README_IMAGE_PLAN.md`.

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

See [`src/LocalGPTInstallerConsole/README.md`](src/LocalGPTInstallerConsole/README.md) for installer details.

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

See [`docs/engineering/release-and-docs.md`](docs/engineering/release-and-docs.md) and [`VALIDATION.md`](VALIDATION.md).

## Architecture at a glance

```text
Blazor UI / controllers / endpoints
                │         explicit interfaces
                │ application, council, provider, formatting,
persistence, collaboration and safety services
                │ EF Core / SQLite / HTTP / bounded filesystem access
```

LocalGPT is a modular monolith with explicit ownership boundaries:

- **LocalGPT** — ASP.NET Core loopback host and Interactive Blazor Server application.
- **LocalGPTInstallerConsole** — installation and bootstrap helper.
- **LocalGPTWebviewWrapper** — optional Windows desktop shell.
- **DevExpress integration** — frontend and integration boundary, not the owner of domain state.

Start with [`docs/architecture/system-overview.md`](docs/architecture/system-overview.md) and continue with the [`AI Host control plane`](docs/architecture/ai-host.md).

## For companies and downstream adopters

Commercial and institutional adaptation is welcome under the terms of the Apache License 2.0. You do not need to turn the upstream project into your product, wait for enterprise features, or ask the maintainer to become your support organization.

A downstream adopter can:

- fork the repository and establish its own release discipline;
- replace or extend provider adapters;
- add organization-specific workflows, permissions, persistence, and branding;
- build internal facilities or domain-specific tools on the architecture;
- contribute generally useful fixes upstream without transferring product responsibility to the maintainer.

LocalGPT does not promise production suitability for a specific organization. Adopters are responsible for their own security review, licensing, validation, deployment, compliance, support, and operational guarantees.

For company use, the LocalGPT project strongly recommends a **DMZ-style isolation procedure**: place LocalGPT and its model/runtime services in a segmented network zone, restrict inbound and outbound traffic with operating-system and perimeter firewall rules, allow only required loopback or explicitly approved endpoints, and run the application under a dedicated least-privilege operating-system account that cannot delete or modify unrelated files. Treat imported repositories, webpages, model output, generated code, and tool requests as untrusted until reviewed.

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
| Architecture | [`docs/architecture/system-overview.md`](docs/architecture/system-overview.md) |
| Project stance | [`docs/reference/design-evolution.md`](docs/reference/design-evolution.md) |
| Security | [`SECURITY.md`](SECURITY.md) |
| Human–AI collaboration | [`docs/architecture/council-runtime.md`](docs/architecture/council-runtime.md) |
| Council blueprint | [`docs/architecture/onewire-security.md`](docs/architecture/onewire-security.md) |
| Frontend patterns | [`docs/architecture/frontend-and-themes.md`](docs/architecture/frontend-and-themes.md) |
| Diagnostics | [`docs/engineering/build-validation.md`](docs/engineering/build-validation.md) |
| DevExpress assets | [`docs/architecture/frontend-and-themes.md`](docs/architecture/frontend-and-themes.md) |
| Release process | [`docs/engineering/release-and-docs.md`](docs/engineering/release-and-docs.md) |
| Open tasks | [`docs/engineering/index.md`](docs/engineering/index.md) |

## License and third-party components

LocalGPT is released under the [Apache License 2.0](LICENSE.MD). Commercial use, modification, distribution, and private adaptation are welcome under that license's terms.

DevExpress packages and runtime assets remain proprietary and are governed by their own licenses. Generated license material must not be committed. See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) and [`docs/architecture/frontend-and-themes.md`](docs/architecture/frontend-and-themes.md).

## Provenance and acknowledgments

LocalGPT was created and is maintained by **Michael Fleischer (`Michi0403`)**.

The project also reflects repeated co-development sessions with OpenAI's ChatGPT, early foundational progress made possible by `gpt-oss-20b`, and many LocalGPT-generated reviews and missing-feature reports that were inspected and used as engineering input. Assistance is acknowledged openly; design decisions, repository ownership, release decisions, and responsibility remain with the maintainer.

See [`docs/reference/design-evolution.md`](docs/reference/design-evolution.md).

---

<div align="center">

**Built as a workshop, not operated as a store.**<br>
Use it, inspect it, fork it, improve it, or build something entirely different from it.
How it works:
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/Screenshot.2026-08-26.005223.png
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/Screenshot.2026-08-26.005245.png
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/Screenshot.2026-08-26.005253.png
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/Screenshot.2026-08-26.005314.png
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/Screenshot.2026-08-26.005514.png
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/teamssettings.html
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/LocalGPTChat.html
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/LocalGPTAiSetupPage.html
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/council-20260816-134119-8e6e1a77d24c40b99c7c096dbb06b197.md
https://github.com/Michi0403/LocalGPT/releases/download/v3.3.0/council-20260821-000301-fd21d7bdaeb84f2b93ad688541ca140d.md
</div>
