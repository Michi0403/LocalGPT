# LocalGPT 3.0.4 source changelog

## User-configurable Remote Control integration fabric

- Added a database-backed Remote Control layer for user-owned REST, OData-style pull, and tokenized webhook connectors without enabling any online endpoint by default.
- Connector definitions persist transport method, URL/body/header templates, response format/selector, timeout, payload bound, poll interval, allowed hosts, enablement, and network enablement. Newly created definitions are disabled until the user explicitly enables them.
- Outbound requests require the connector and network flags, HTTPS unless the user explicitly allows insecure HTTP, and an explicit host allowlist. The same checks are repeated across bounded redirects.
- Webhook secrets remain server-owned: the token is excluded from JSON projection and logs, while the UI may show a newly generated token once for user configuration.
- Added a Remote Control page with a connector editor, action-pipeline builder, execution history, and catalog-backed action selection. The page is the twentieth explicit `InteractiveServer` island; all nineteen pre-existing render-mode directives remain unchanged.
- Added controller and DXFunction surfaces for connector/pipeline management and execution while keeping transport/runtime work in DI services.

## Configurable action pipelines / Remote Control Factory

- Added persistent pipelines that transform connector/manual payloads and execute ordered application actions through the existing `IDxAiFunctionRegistry` rather than reflection or direct service shortcuts.
- Pipeline steps may target existing DXFunctions or published service methods. Published service-method targets route through the maintained `localgpt.public_service.invoke` bridge, preserving the existing catalog, approval, automatic-invocation, schema, and diagnostics policy.
- Templates support payload, connector, variable, previous-step, and step-result-path interpolation such as `{{step:normalize.value}}`.
- Cancellation propagates as cancellation rather than being converted into an ordinary failed step.
- User-DXFunction wrappers are intentionally rejected as nested pipeline steps to prevent recursive wrapper graphs; users compose the underlying actions directly in one pipeline.
- AI-safe connector/pipeline projections omit secret-bearing URL/header/body/payload-template details where those values could contain credentials.

## User-owned editable DXFunctions

- Added first-class database persistence and Create/Edit/Delete UI/API support for user-owned `user.*` DXFunctions.
- A user DXFunction wraps one enabled Remote Control pipeline but is registered into the normal DXFunction registry as a dynamic descriptor; it therefore passes through the same direct/automatic invocation checks, parameter-schema validation, human-confirmation policy, logging, and catalog synchronization as source-controlled functions.
- Source-controlled/system DXFunctions remain source-owned and cannot be deleted from the runtime editor. Their existing exposure/policy grid remains editable.
- Deleting a user DXFunction removes its user-owned catalog entry during synchronization without replacing user policy on unrelated system entries.
- Added a user-DXFunction editor to `/dx-functions` with pipeline selection, parameter schema, exposure/automatic/read-only/human-confirmation policy, save, edit, and delete actions.

## Knowledge-backed cross-platform compiler/runtime toolchains

- Reworked the existing Project Maintenance compiler discovery into a cross-platform toolchain discovery service instead of introducing a second toolchain database.
- Discovery is PATH-first on Windows, Linux, and macOS. PATH is split with the platform-native separator and is used only as a discovery source; the full PATH value is not copied into persisted toolchain environment rows.
- Knowledge profiles can add executable names, common roots, OS-specific roots, environment-root variables, project markers, validation arguments, context tags, search depth, and database-backed regex names.
- List-valued environment-root variables are split with the platform-native path separator before discovery. Environment variables that directly name an executable, such as `MSBUILD_EXE_PATH`, are supported as executable candidates.
- Added offline seeded Knowledge profiles for .NET SDK/MSBuild, Java/JDK/JRE, Gradle, Maven, Python, Node.js, PowerShell, GCC/G++, Clang, CMake, Rust/Cargo, Go, PlatformIO, and Arduino CLI. These are discovery metadata only; they contain no online endpoint and perform no automatic download.
- PowerShell discovery intentionally does not treat `PSModulePath` as an installation root; PATH and knowledge-defined executable roots are used instead.
- Persisted toolchain installations now retain `ToolchainKind`, `DetectedPlatform`, `KnowledgeProfileKey`, profile Knowledge linkage, and exact-version Knowledge linkage while continuing to use the existing `ProjectCompilerInstallation` record as the authoritative installed-toolchain object.
- Toolchain environment variables are exposed as structured Name/Value/Source/Enabled rows in the UI/API and serialized only by the owning service for backward-compatible process execution.
- Added toolchain controller/DXFunction capabilities for Knowledge profile listing, local discovery, installation listing/save/validate/delete, and missing-version Knowledge requests.
- If an exact discovered version lacks approved/pinned Knowledge, LocalGPT asks through the existing Human Collaboration system for a Markdown file, a Knowledge Database article, a pasted text blob, or a skip decision. It does not automatically search the Internet.

## Knowledge / regex / project-system wiring

- Added `docs/reference/toolchain-discovery.md` to the maintained offline Knowledge seed and runtime Knowledge file set.
- Added database-backed regex definitions for toolchain Knowledge blocks, version extraction, and environment-token expansion and routed toolchain parsing through the existing regex service.
- Project Maintenance get/save/discover/validate flows now expose and preserve toolchain kind, platform, discovery source, Knowledge profile/version linkage, and structured environment variables.
- Toolchain capabilities are available to Council/AI through ordinary DXFunctions; pipelines reference logical stored toolchain/application capabilities rather than machine-specific hardcoded Windows paths.

## Database migration

- Added the single additive migration `20260817135000_AddRemoteControlIntegrationFabric`.
- Added four new empty-by-default tables: `RemoteControlConnectorDefinitions`, `RemoteControlPipelineDefinitions`, `RemoteControlExecutionRecords`, and `UserDxAiFunctionDefinitions`.
- Added five backward-compatible columns to `ProjectCompilerInstallations`: `KnowledgeProfileKey`, `KnowledgeEntryId`, `VersionKnowledgeEntryId`, `ToolchainKind`, and `DetectedPlatform`, plus a Knowledge-profile/version index.
- Existing 3.0.3 migrations are unchanged. No database reset is required and no connector/user DXFunction row is seeded by the migration.

## Version

- LocalGPT: 3.0.4
- LocalGPTWebviewWrapper: 3.0.4
- LocalGPTInstallerConsole: 3.0.4
- Wire Protocol: 2.1.1 (unchanged)
- Council seed version: 25 (unchanged)
