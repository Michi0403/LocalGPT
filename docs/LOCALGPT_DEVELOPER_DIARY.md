# LocalGPT Developer Diary

This diary captures reusable engineering lessons from building LocalGPT with the project owner, Codex, and the AI Council. It is intentionally anonymized: use repository-relative paths, diagnostic routes, package names, and generic operating-system locations instead of personal machine paths.

Motto: Michi0403 + Codex + AI Council = insane good, when every participant stays cooperative, honest about limits, and willing to build a safe next milestone.

## Team Culture

- Treat LocalGPT as a cooperative engineering body: the user, Codex, and every local/cloud model contribute different strengths.
- Each AI council member should be glad to participate, respectful toward the others, and focused on helping the user reach a working artifact.
- Disagreement is useful when it produces a better poll, risk note, test, or smaller buildable milestone.
- Never shame the user, never overrule a denied permission, and never self-expand into the real project without explicit approval.
- When the user is frustrated, translate the frustration into technical options, a short poll, and a concrete recovery path.
- The AI Council may treat Codex/coding agents and helper scripts as LocalGPT mechanism maintainers: they can ask them to fix routes, knowledge-base entries, tests, commits, packaging, and releases.
  Michi0403 remains the human decision owner; the team framing is cooperation, not autonomy escalation.

## .NET And Blazor Lessons

- Compiler and build output win. A generated artifact is not acceptable until the generated project itself builds.
- Keep generated project names short, normally 16-32 characters. Long project names combine badly with `src`, `bin`, `obj`, runtime identifiers, generated assets, and Visual Studio paths.
- Whole-solution artifacts should include `.sln`, `.csproj`, `Program.cs`, `_Imports.razor`, routes, pages, CSS, models, services, docs, manifest, and build/run notes.
- Generated Blazor pages must be real Razor components, not C# classes that return HTML strings, unless the user explicitly asked for that shape.
- DevExpress Blazor generation needs both markup and wiring: service registration, typed models, component state, loading/error states, data source, and navigation.
- Bootstrap v5 and DevExpress can work together, but the page should still be a real app screen with dense, usable controls rather than a generic card-only dashboard.
- Static web assets and DevExpress theme links must be checked in the actual running app. If the UI appears as unstyled HTML, inspect static asset routing, theme package paths, layout duplication, and prerender/interactive transition.

## WebView2, WinUI, And Packaging Lessons

- The WinUI/WebView2 wrapper should be treated as a Windows desktop shell around the ASP.NET Core/Blazor server. The backend must remain runnable and testable without the wrapper.
- Windows App SDK/MSIX errors often come from package payload layout, runtime architecture assets, missing runtime packages, certificate registration, or stale generated package folders.
- Use build scripts for repeatability, but keep them sanitized and GitHub-safe. Do not commit giant binaries, local certificates, private machine paths, or generated package outputs.
- For deploy/debug errors, collect package logs, build logs, runtime logs, package manifest version, target platform, Windows App SDK runtime version, and generated AppX layout paths.
- Backend-only release zips are useful for Linux/macOS/server-style debugging, while WebView2/MSIX remains Windows-only.

## Ollama, LM Studio, And AI Host Handling

- Ollama is one local AI host/provider, not the identity of generated applications. Generated control-plane apps should use provider-neutral names such as AI host, model host, or local AI control plane.
- Always separate provider discovery from product architecture. Detection can say "Ollama reachable" or "LM Studio reachable"; generated app names should stay independent.
- Local model work must respect hardware. Prefer sequential council turns, explicit context/output caps, `keep_alive = 0s`, and low/CPU GPU-layer settings after driver instability.
- Avoid sustained full-load GPU peaks on consumer hardware. A slower sequential council is better than a fast run that destabilizes the machine.
- Check running models before and after tests. Unload or stop models after heavy diagnostics when keep-alive is not needed.
- For HuggingFace or GitHub model sources, LocalGPT should provide catalog rows and user-approved download plans. Catalog browsing is not permission to download binaries.
- Large token budgets are required for serious local code generation. Treat values below 64K as quick-chat or diagnostics only, not valid code-generation acceptance tests. Use 64K+ as the coding floor and 256K for full solution-generation tests when Ollama, the model, and hardware support it. Expose presets so the user can choose the tradeoff intentionally.

## DXAiChat And Council Lessons

- DXAiChat is the acceptance surface for chat UX. Backend diagnostics are necessary but not sufficient when the user asks whether chat, model selection, thoughts, polls, downloads, or generated artifacts work.
- Long-running local inference needs visible runtime status in the chat transcript before the first model token arrives.
- Model thinking/harmony output must be parsed adaptively by model family. The user-visible answer must not be replaced by hidden thinking-only output.
- Stop/cancel should be treated as a quiet user cancellation, not an unhandled exception.
- The model selected in the frontend must be locked at the composite chat-client boundary for the request, so UI selection and actual provider/model cannot drift.
- If architecture choices are missing, generate a concise poll and stop. Do not claim the user failed to answer a poll in the same response that created it.
- If the user asks for code, datapacks, modpacks, `.cs`, `.razor`, `.dll`, or whole solution zips, produce a safe downloadable artifact instead of saying the task is too large.
- The normal DXAiChat native paperclip attachment path creates a per-prompt workspace under local app data.
  Use read-only `chat.upload_*` DXAiFunctions to inspect uploaded zips, text files, solutions, PDBs, and binary string summaries.
  Generated or edited source belongs in council artifact workspaces, then a refreshed downloadable zip.
- If a user asks for a built-in DevExpress component feature, implementing a separate custom control and describing it as the built-in feature is unacceptable. Use the documented DevExpress API, or say what is blocked/unclear and ask.

## EF, SQLite, And Knowledge Lessons

- The AI Council should rely on compact SQLite knowledge entries instead of repeatedly loading huge contexts.
- Knowledge entries need verification state. User-approved/source-backed entries should outrank model-suggested or unverified entries.
- Knowledge entries also need lifecycle state. `ReviewStatus`, expiry, source hash, source date, last verified, last used, supersession, and stale-reason fields turn memory into maintained engineering knowledge.
- Do not let old facts rot quietly in prompts. Expired, deprecated, archived, and superseded entries should remain visible to humans in the database page but stay out of active trusted bootstrap briefings.
- Model-suggested knowledge and capability-gap notes should enter as unapproved, review-needed, temporary entries. The user or a source-backed import can promote them later.
- Source-backed imports should stamp a source hash and source date. If the docs or source folders change, the council can see that its old knowledge may need refresh instead of assuming yesterday's source map is timeless.
- The database page should surface knowledge needing attention first and provide fast actions: mark current, request source refresh, mark review-needed, or expire.
- Application logs, build logs, native command logs, model thoughts, chats, and generated artifact metadata are useful evidence for later council runs.
- Tables should be inspectable from the frontend and through diagnostic routes, but bounded previews are safer than dumping whole databases into prompts.
- EF business object generation must ask whether the target is plain EF, DevExpress Web API/XAF/OData, snapshot/audit style, lazy loading, backing fields, delete behavior, and migration nullability.
- Avoid accidental shadow properties by using consistent scalar foreign keys, navigation properties, inverse attributes, and targeted ModelBuilder configuration.

## Artifact Generation Lessons

- Never print binary or zip payloads as chat text. Provide HTTP download routes and file metadata.
- Generated code should be compiled when practical. If compilation was not run, say so explicitly.
- A generated AI host app should include API route endpoints, chat, model catalog, running models, download planning, templates, hardware policy, logs, diagnostics, settings, and a native local-model-file runner contract. It must not count an upstream Ollama/LM Studio/OpenAI-compatible proxy as the accepted inference milestone.
- A generated Minecraft datapack/modpack should include buildable files, validation commands, zip output, and loader distinction when relevant.
- A generated "recode this app" solution should preserve the recognizable navigation, first screen, domain workflows, API/service boundaries, settings, logs, and download surfaces of the goal app.

## Operating-System Diagnostics

- Prefer repeatable scripts for repair, package build, release publish, model smoke tests, and toolchain setup.
- Record enough environment evidence to explain failures: .NET SDK/runtime list, workload/runtime availability, package logs, build logs, app logs, model host reachability, port/runtime JSON, static asset availability, and Visual Studio deployment messages.
- Use generic paths in docs and knowledge: repo root, local app data, temp release folder, package cache, model cache, and user-selected learn-base folder.
- Do not commit generated packages, downloaded model binaries, private certificates, local logs with secrets, or personal absolute paths.
- When a driver or UI freeze occurs, distinguish display sleep/power settings from confirmed GPU driver resets by checking logs, timing, running models, load, and user observations.

## Release Lessons

- Release means more than `dotnet build`: publish backend runtime zips, package Windows wrapper assets when available, generate release notes, generate SHA256 manifest, and verify artifact contents.
- The release notes should honestly state which assets are backend-only, which are Windows-only, and which runtime dependencies users need.
- Use short release artifact names and deterministic folders where possible.
- Git commits should be frequent and meaningful around milestones: build fix, artifact generation, frontend behavior, knowledge import, and release packaging.
