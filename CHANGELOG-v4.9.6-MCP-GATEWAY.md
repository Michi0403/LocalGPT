# LocalGPT 4.9.6 changelog — MCP gateway

## MCP transport and hosting

- Added an opt-in MCP gateway inside the existing ASP.NET Core/Kestrel host; no second application or duplicated business layer is required.
- Added an isolated dedicated MCP listener, default TCP `51142`, plus optional exposure on the primary LocalGPT loopback endpoint.
- Added configurable MCP path, bind address, protocol version, request-size limit and optional PFX certificate/password.
- Added startup port-conflict validation against LocalGPT app/install, 1-Wire TCP/discovery and the optional remote-web listener.
- Added MCP endpoint state to the runtime endpoint report so external tooling can discover whether MCP is enabled, its port and path.
- Added environment overrides for MCP enabled/port/address/path/certificate/password and API key without persisting secrets into source configuration.

## MCP protocol surface

- Added stateless JSON-RPC handling for initialization/ping, `tools/list`, `tools/call`, `resources/list`, `resources/templates/list`, `resources/read`, `prompts/list`, `prompts/get`, and protocol notifications.
- MCP tools bridge the existing `IDxAiFunctionRegistry` and LocalGPT service layer rather than reimplementing function behavior.
- Added project/revision/artifact, knowledge, regex and toolchain resource surfaces plus LocalGPT-oriented prompt templates.
- Existing LocalGPT approval remains authoritative; MCP does not bypass it by setting `UserConfirmed` itself.
- Added stable operation-ID metadata for approval retries so a client can approve a pending LocalGPT request and repeat the same call without entering a repeated-confirmation loop.

## Gateway/data-domain firewall

- Expanded `/install` with a dedicated **MCP gateway** configuration area covering listener topology, ports, path, TLS, client admission, API-key policy, Host/Origin allowlists, primitive exposure, DX function exposure, tool-prefix allow/deny lists and size limits.
- Added independent exposure switches for projects, knowledge metadata/content, regex records, toolchains, artifacts/content, Council, runtime policy, diagnostics, environment variables, sensitive values, system paths and archived records.
- Added project-ID allowlisting and matching enforcement for both project resources and project-scoped tool calls.
- Added result redaction for configured system-path/download-link restrictions and valid JSON truncation envelopes for oversized responses.
- Remote clients require explicit remote enablement plus an API key. Blank Host policy normalizes to loopback-only defaults; browser cross-origin access requires an explicit Origin allowlist.
- Secure defaults keep MCP disabled, remote access disabled, mutating DX functions disabled, knowledge content metadata-only and sensitive/system-path exposure disabled.

## Installer and diagnostics

- `/install` now presents the complete port contract including MCP and displays MCP port-conflict/admission warnings before saving.
- Remote mode forces API-key authentication on save; enabling MCP with no selected endpoint restores the dedicated endpoint to prevent an enabled-but-unreachable configuration.
- MCP settings are persisted through the existing user configuration flow and take effect after restart where listener topology changes require it.

## Build/audit repair

- Added the missing method-level try/catch diagnostics boundary to `HumanCollaborationService.IsReusableDecisionForRequest`, fixing the service-resilience audit failure reported against 4.9.5.

## Microphone follow-up

- Kept the chat microphone on `DxButton` while rendering the microphone/stop glyph as explicit child content with an icon mask, preserving DevExpress control policy without depending on `IconCssClass` rendering.
- Removed the Council interaction state from unnecessarily disabling microphone capture.
- Speech-model discovery now accepts supported speech-recognition capabilities generally, and the UI can record audio without transcription when no speech-to-text model is configured.

## Validation constraints

- Source was validated with LocalGPT's maintained Python architecture, async-continuation, service-resilience, cross-platform-boundary and DX-function wiring audits.
- Per the requested workflow, no `dotnet`, MSBuild, NuGet restore/build/publish, GitHub access, installer execution, signing or runtime deployment was performed.
