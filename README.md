<div align="center">

# LocalGPT

### User-owned AI and software infrastructure — from one machine to independent AI centers.

An open-source, local-first .NET 10 and Blazor platform for AI councils, model orchestration, persistent knowledge, project workflows, human participation, automation, connected systems, and independently operated AI.

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

LocalGPT is an independent, garage-built project created and maintained by **Michael Fleischer (`Michi0403`)**. It began as a serious personal tool and shared workshop, but the architecture is deliberately aimed beyond a single desktop application.

The project combines a local AI runtime with a database-backed .NET application, a DevExpress Blazor interface, configurable council teams, persistent project context, human collaboration checkpoints, bounded tool access, diagnostics, installers, release tooling, and explicit local/remote system boundaries.

### Future2 mission

LocalGPT is part of the **Future2** direction: practical, user-owned AI and software infrastructure that can operate without making a centralized corporate or government service a technical requirement or authority. External cloud providers may be connected when they are useful, but they remain optional providers rather than owners of the system, its data, or its decisions.

The intended scale runs from one person's computer and local models, through workstations, embedded devices and explicitly authorized robots, to independently operated AI centers and specialized software environments. The engineering priorities behind that goal are local ownership, human authority, provider neutrality, inspectability, durable persistence, explicit permissions, and replaceable integrations.

PublisherStudio is a companion demonstration of the same broader direction outside the AI-workbench category: it is independently usable creative/productivity software that can optionally cooperate with LocalGPT and 1-Wire rather than being technically dependent on a remote platform.

For .NET developers and companies, the project is also a substantial open-source .NET 10/Blazor reference that can be studied, forked, adapted, or integrated under Apache-2.0. The maintained UI uses separately licensed DevExpress components; the repository documents that boundary rather than pretending every dependency is open source.

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

Local development targets the pinned .NET SDK and DevExpress 25.2.x. DevExpress 25.1+ packages are restored from NuGet.org; the personal **DevExpress .NET license key is separate from package restore credentials** and must remain outside the repository.

The PowerShell build entry points are intended to run from Windows PowerShell 5.1 or PowerShell 7 (`pwsh`) on Windows, macOS, and Linux. The optional WinUI/WebView2 wrapper remains a Windows application, but its project enables Windows cross-targeting so a non-Windows developer machine can restore/build the Windows target without pretending that the wrapper is runnable on macOS/Linux.

```powershell
# Windows
.\Build-LocalDevelopment.ps1 -Configuration Debug -Platform x64

# macOS / Linux
pwsh ./Build-LocalDevelopment.ps1 -Configuration Debug -Platform arm64
```

`NuGet.Config` no longer requires a repository-local `./packages` folder for ordinary source builds. The release scripts inject that source explicitly only when they intentionally consume the locally packed wire-protocol package.

#### DevExpress license registration on macOS/Linux

DevExpress 25.2 performs build-time license validation. The exact file/environment-variable casing matters on Unix-like systems. LocalGPT build scripts run a preflight and never print the key value.

Default license locations:

- Windows: `%AppData%\DevExpress\DevExpress_License.txt`
- macOS: `$HOME/Library/Application Support/DevExpress/DevExpress_License.txt`
- Linux: `$HOME/.config/DevExpress/DevExpress_License.txt`

You can register a downloaded key file into the correct per-user location without adding it to the repository:

```powershell
pwsh ./build/Register-DevExpressLicense.ps1 -LicenseFile "$HOME/Downloads/DevExpress_License.txt"
```

Alternatively use the case-sensitive `DevExpress_LicensePath` (folder) or `DevExpress_License` (key value) environment variable. If DevExpress reports DX1002 after the key is found, update the key so it supports the 25.2 major version, then restart the IDE/terminal and rebuild. Never commit `DevExpress_License.txt` or a license value.

#### Cross-platform documentation prerequisites

The release and local-development PowerShell entry points now prepare the documentation runtime before the long build starts. LocalGPT accepts an existing Node.js 20-22 installation, but if no compatible runtime is available it downloads a **portable per-user Node.js 22.23.2 runtime** for the current Windows, macOS, or Linux architecture. The download is verified against the official Node.js `SHASUMS256.txt` manifest and is stored outside the repository in the LocalGPT documentation-tool cache; administrator/root installation is not required.

You can run the same preflight explicitly:

```powershell
pwsh ./build/Initialize-BuildPrerequisites.ps1
```

On Apple Silicon this selects the `darwin-arm64` distribution; Intel Macs use `darwin-x64`; Linux uses the matching `linux-x64` or `linux-arm64` archive; Windows selects the matching ZIP. The resolved executable is exported as `PLAYWRIGHT_NODEJS_PATH` for DocFX/Playwright and its directory is prepended to the current build process `PATH` only.

The documentation stage also looks for Chromium-family browsers both on `PATH` and in normal macOS application-bundle locations. `LOCALGPT_DOCUMENTATION_BROWSER` can still override the browser executable explicitly. If direct Chromium-family printing cannot produce the very large complete manual, the build falls back to the DocFX PDF plug-in using the provisioned Node.js runtime instead of asking the developer to install Node manually.

Before publishing or creating a verified source package, follow the repository validation and release process rather than improvising a manual package:

```powershell
pwsh ./build/Invoke-RepositoryValidation.ps1
pwsh ./build/New-VerifiedSourcePackage.ps1 -Version "<version>"
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

</div>
