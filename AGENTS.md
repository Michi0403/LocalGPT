# AGENTS.md

## Purpose

This repository contains LocalGPT, a local-first AI engineering workbench wrapped
as a Windows desktop-hosted Blazor/ASP.NET Core application. It combines:

- Blazor Server interactive UI
- DevExpress Blazor components
- Ollama-hosted AI model configuration
- context-aware chat/client services
- source-backed Windows/.NET/DevExpress/Minecraft council knowledge
- downloadable `.cs`, `.razor`, `.dll`, whole-solution, AI-host, and datapack artifacts
- backend native command execution services
- a WinUI 3/WebView2 desktop wrapper
- MSIX/DesktopBridge packaging for Windows deploy/debug

This file is intended for AI coding agents and contributors so they can work with the repository without fighting the hosting model.

## High-level structure

### `LocalGPTWebviewWrapper/LocalGPT`

Main ASP.NET Core and Blazor application. This project contains:

- Blazor UI and pages
- DevExpress component usage
- AI model configuration and chat services
- Ollama connectivity checks
- configuration save/load services
- Minecraft mod workspace helpers
- native command execution abstraction
- SQLite chat memory, council knowledge, application logs, and live table editing
- Test Lab diagnostics and deterministic artifact-generation routes

### `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper`

WinUI 3 desktop wrapper. It launches the local ASP.NET Core server and hosts it in WebView2.

### `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package)`

Windows package project for deploy/debug. This project must preserve the wrapper output, .NET 10 framework-dependent runtime configuration, and the Blazor static web assets manifest in the AppX layout.

### `LocalGPTWebviewWrapper/build`

Developer repair and build scripts. Keep these scripts sanitized, small, and suitable for GitHub.

## Architectural intent

Treat the system as a local desktop shell around a real ASP.NET Core application.

Security boundary:

- LocalGPT is local-first and privacy-oriented, but it is not risk-free.
- In intended use, bind the ASP.NET Core host to loopback and keep it inside the
  WinUI/WebView2 desktop boundary.
- Do not expose Kestrel to `0.0.0.0`, coworkers, VPNs, or public networks without
  normal web-app hardening: auth, authorization, CSRF protection, TLS, rate
  limits, audit logs, command restrictions, secrets handling, and workspace
  isolation.
- Treat native commands, Python interop, generated scripts, generated projects,
  and imported knowledge as trusted-local capabilities that require user review
  and explicit permission gates.
- Treat SQLite chat memory, council knowledge, diagnostics, and logs as sensitive
  project data.

The WinUI layer should remain thin:

- start/host the server
- display the UI
- handle desktop integration

The Blazor/server layer should own:

- AI setup and model selection
- Ollama connectivity
- context reuse
- Minecraft mod generation workflows
- native command orchestration
- configuration persistence

## DevExpress guidance

The UI intentionally uses DevExpress Blazor.

Important rules:

- do not replace DevExpress with another UI stack unless explicitly requested
- preserve DevExpress package references and static asset loading
- check generated static web asset manifests when DevExpress JavaScript or CSS files 404
- when the user asks for a built-in DevExpress capability, use the documented DevExpress API surface or say clearly that it is blocked/unclear and ask; never add a parallel custom control and describe it as the requested built-in feature
- for `DxAIChat` attachments, use the native paperclip attachment surface: `FileUploadEnabled`, `DxAIChatFileUploadSettings`, `AIChatUploadFileInfo`, and the normal chat-client upload content path. A custom upload panel may exist only as an explicitly labeled fallback, not as the primary feature. Do not add a `MessageSent` handler unless you intentionally replace automatic AI Chat delivery and implement the full manual response path.
- DevExpress 25 module assets are under `/_content/DevExpress.Blazor/modules/`
- use `/__diag/devexpress` or the AI bootstrap inventory before proposing DevExpress APIs; respect the referenced package/version family and mark unknown APIs as `Needs verification`
- implement DevExpress Office document generation, report generation, PDF export, and downloadable generated files in the ASP.NET Core/Blazor server backend, then expose safe download links to the frontend
- use `/__diag/build-debug-files?copy=true` when build symbol files are useful for council diagnostics. These `.pdb`, `.pdg`, and `.appxsym` files stay out of git and should not be mistaken for source-level feature usage.

## Packaging guidance

The package project is not a normal SDK-style project. Use Visual Studio MSBuild, not only `dotnet build`, for full package/debug verification.

Important package behavior:

- `LocalGPTWebviewWrapper` publishes framework-dependent for .NET 10; use the repair script to install the .NET 10 Desktop Runtime instead of letting Windows open an Edge runtime prompt
- the package project overlays that publish output into the AppX layout
- keep `IncludeLocalGptPublishedPayload` defaulted to `false` for Visual Studio Debug/F5; release/publish scripts may opt in with `true` after the web project is published
- do not make published Blazor payload inclusion unconditional, because duplicate `LocalGPT.deps.json`, static web asset endpoint JSON, or `wwwroot` entries can trigger APPX1111 package-map failures
- `LocalGPT.staticwebassets.runtime.json` must exist beside the packaged executable
- release MSIX packages must include the published `wwwroot/_framework`, `wwwroot/_content`, and `wwwroot/LocalGPT.styles.css` assets; `build/Build-LocalGptPackage.ps1` verifies `blazor.web.js`, `dx-blazor.svg`, `office-white.bs5.min.css`, and scoped CSS inside the actual MSIX archive
- AppX image assets must exist in the loose `bin\<platform>\<configuration>\AppX\Images` layout; missing manifest images can surface as `0x80070002`, `0x80073CF9`, or `DEP1000`
- when Windows keeps a stale LocalGPT development registration, use `build\Repair-LocalGptDevEnvironment.ps1 -SkipBuild -Register`; it removes only the LocalGPT package identity and retries once
- missing static web assets cause DevExpress module errors and blank/broken UI

## Configuration expectations

Configuration is user-facing through the setup page and should be durable.

Rules for changes:

- do not silently rename configuration sections
- preserve existing AI profile data where possible
- prefer additive migrations over destructive rewrites
- handle missing optional sections with non-null defaults
- make save/load failures visible through logs or UI messages

## AI and Ollama guidance

The app should be able to save and select multiple Ollama AI profiles and reuse context intelligently.

LocalGPT works best when several local models cooperate instead of one model
carrying every role. Use the council to split planning, implementation, review,
and missing-file checks when hardware allows it. Keep model context database-led:
use pinned/current council knowledge, saved conversations, diagnostics, and
uploaded workspace summaries before sending huge source blobs.

AI coding agents such as Codex can work with the council. The models can ask for
missing LocalGPT functions, source imports, benchmark evidence, or product
repairs; the agent can implement those mechanics, run builds, commit, package,
and feed verified results back into SQLite knowledge.

The preferred local debug model is `gpt-oss:20b`. Use `LocalGPTWebviewWrapper/build/Test-OllamaGptOss.ps1` before blaming `DxAIChat`, because an empty or failing Ollama response will make the chat UI look broken even when Blazor and DevExpress are working.

When changing AI behavior:

- keep model/provider selection explicit
- avoid global hidden state for active models
- persist user choices through the configuration writer
- separate connectivity probing from chat execution
- keep context reuse bounded and explainable
- maintain council knowledge lifecycle: source-backed/current entries can guide
  prompts, model-suggested entries need review, expired/deprecated/superseded
  entries stay visible to humans but must not be used as trusted bootstrap facts
- use capability-gap reports when a model lacks functions, sources, versions, or
  domain knowledge needed to produce a downloadable artifact

## Minecraft mod building direction

The target workflow is complex Java Minecraft mod creation on command.

Agent guidance:

- treat Minecraft Java Edition as the first-class target
- use Fabric for lightweight client/server mod iteration, NeoForge for modern Forge-style modding, Paper for server-side Java plugins, and datapacks for vanilla command/data behavior without Java
- keep Bedrock support separate as a future behavior/resource pack exporter
- use `LocalGPTWebviewWrapper/build/Setup-MinecraftModToolchain.ps1` when the user needs JDK 21, local Gradle, Eclipse, or setup diagnostics
- prefer LocalGPT diagnostics over raw Ollama when testing AI behavior: `POST /__diag/dxaichat-smoke`, `POST /__diag/council`, and `GET /__diag/minecraft/workspace-smoke`
- for Living Cities datapack work, prefer `GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4`; it validates and zips the datapack, then writes a compact council knowledge entry so models do not need the full prompt pasted again
- inspect `GET /__diag/logs?minimumLevel=Warning&take=30` when setup behavior is strange; recent SQLite application logs are included in AI bootstrap so DXAiChat and the AI Council can notice missing Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, package registration, or model setup
- use `GET /__diag/learn-base/import` or the Test Lab Learn-Base presets to import local source/docs into SQLite knowledge as compact source maps. The importer skips build/cache/binary noise, upserts by stable source IDs to avoid duplicate rows, and has special Microsoft .NET docs, C# compiler/language, Windows docs, and DocFX corpus handling.
- use the WinUI WebView2 smoke mode with `LOCALGPT_WEBVIEW2_SMOKE=1` as the preferred frontend fallback when browser automation is unavailable or misleading. Do not rely on an agent's built-in browser as proof that the packaged desktop shell works; the WebView2 smoke path validates the real wrapper and currently covers `/Chat`, `/model-council`, `/database`, and `/minecraft-mod-builder`.
- after a black screen, driver reset, or high VRAM pressure, run council tests database-first and low-resource: one model, `MaxRounds = 0`, `MaxOutputTokens = 1024`, `MaxContextTokens = 2048`, `OllamaKeepAlive = "0s"`, and `OllamaNumGpu = 0`; check `ollama ps` before and after
- keep filesystem and OS command execution in backend services
- use `INativeCommandRunner` or a similar service boundary for native commands
- keep frontend JavaScript for client-only helpers, not privileged execution
- design generated mod workspaces so they can be inspected and rebuilt
- prefer explicit project templates and build logs over opaque generation
- write missing-feature or blocked-workflow reports to `%LOCALAPPDATA%\LocalGPT\AIReports\`
- have AI Council participants help users set up the system, and ask a technical recovery poll if Java, Gradle, Minecraft, Ollama, or a model is missing
- keep council memory resumable: saved `AI Council - ...` conversations in SQLite can be selected for continuation, and each new run should respect prior user poll decisions unless the user changes them
- always record and display the full council roster for each result step. A user may exclude faulty members in the frontend; models may recommend exclusion only as a user-confirmed poll option
- implementation-request council chats may generate CodeDOM C# starter artifacts under `%LOCALAPPDATA%\LocalGPT\CouncilArtifacts\`; expose them only through safe `/__artifacts/council/{fileName}` links
- prototype requested features in harmless sandbox artifacts or temporary workspaces before embedding them into the real LocalGPT project structure, then add a smoke/diagnostic path for verification
- LocalGPT and the AI Council must never self-expand or integrate generated features into the real project without explicit user permission, and must never overrule a user decision that denies or limits expansion
- missing-feature reports must document helpful sources requested by AI participants, such as official docs, examples, versioned package references, specs, or sample repositories
- use DXAiChat native paperclip attachments as prompt evidence workspaces. Uploaded files are saved under `%LOCALAPPDATA%\LocalGPT\ChatUploadWorkspaces`, zips are extracted safely, and PDB/DLL/EXE/WASM files are summarized with printable strings only. Inspect them through `chat.upload_*` DXAiFunctions; do not execute uploaded or extracted files.
- use council artifact workspaces for generated source, user edits, HtmlEditor-style file review, compile checks, and refreshed downloadable zips. The AI Council can ask Codex/coding agents to maintain these LocalGPT mechanisms, tests, commits, packages, and releases while Michi0403 remains the human decision owner.

Detailed mod-builder instructions for AI agents are in `docs/MINECRAFT_MOD_AI_BUILDER.md`. Current workflow memory and known-good commands are in `docs/LOCALGPT_WORKFLOW_MEMORY.md`.

## How AI should modify code here

When implementing or editing features:

1. Preserve the WebView2-hosted Blazor architecture.
2. Keep the WinUI wrapper thin.
3. Prefer service boundaries over UI-driven command execution.
4. Preserve DevExpress compatibility.
5. Keep configuration backward-compatible.
6. Build the full solution with Visual Studio MSBuild when packaging is touched.
7. Verify the packaged frontend visually when DevExpress UI behavior changes.
8. Never publish a release whose MSIX archive lacks Blazor/DevExpress static assets.

## Good contribution patterns

Good changes include:

- improving setup/config save and load reliability
- documenting packaging and runtime fixes
- tightening null handling around options
- making AI profile selection explicit
- improving command execution safety and logs
- adding focused repair scripts

Risky changes include:

- moving server responsibilities into the WinUI wrapper
- introducing unbounded command execution from UI code
- deleting package targets that copy publish/static asset output
- replacing DevExpress components casually
- broad retargeting across .NET versions without a full package verification

## Recommended first-read areas for an AI agent

Start here:

- `LocalGPT/Program.cs`
- configuration business objects
- configuration writer service
- chat client factory and composite chat client
- setup/install pages
- `INativeCommandRunner`
- package `.wapproj`
- `build/Repair-LocalGptDevEnvironment.ps1`

If a behavior seems unusual, assume it may be related to:

- WebView2 desktop hosting
- DevExpress static assets
- Windows App SDK packaging
- local AI model configuration
- future Minecraft mod build automation
