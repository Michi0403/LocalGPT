# LocalGPT 3.0.5 source changelog

## AI-guided, reopenable initial setup assistant

- Extended the existing Install **Setup guide** instead of introducing a second onboarding application or page. Incomplete first-run onboarding opens the guide automatically unless the user already chose another Install section; the same guide remains reopenable later through Install.
- Added an `InitialSetupAssistantService`, controller surface and normal DI-discovered DXFunctions so the manual UI and an AI Council use the same hardware/provider/model/benchmark orchestration.
- Added the source-controlled `initial-setup-assistant` Council team plus the `initial-setup-council-start` prompt suggestion. The team begins with `initial.setup.status`, uses the existing `human.collaboration.request` suggested-response mechanism for user choices, and stops at normal human-confirmation boundaries.
- The AI-guided Council link is offered only after LocalGPT can see at least one installed local model. Manual setup functions remain available before that point so a first local runtime/model can be bootstrapped without pretending an AI is already available.
- Council team seed version advances from 25 to 26 solely to deliver the new maintained setup team without replacing user-modified Council teams.

## Multi-host / multi-GPU hardware evidence

- Hardware setup is a list rather than a single GPU text field. Each accelerator row retains its physical/provider endpoint and host key so GPUs from separate Ollama/AI hosts are not merged into one artificial machine.
- `ConfiguredAiHostHardwareDraft` now carries the full existing `ConfiguredAiHostGpu` list while preserving the older primary-GPU compatibility fields.
- Local detection and HWiNFO text import collect multiple supported GPUs; save groups reviewed rows by physical host and persists the full GPU collection through the existing `GpusJson` hardware profile field.
- HWiNFO report text stays local; report bodies and hardware values are not copied into ordinary logs.

## Optional CanIRun.ai compatibility evidence

- Added an explicit-opt-in CanIRun.ai recommendation service and setup UI. No CanIRun.ai request occurs merely because the setup guide is opened.
- The UI visibly credits **CanIRun.ai by midudev**. Requests use HTTPS, only `canirun.ai` / `www.canirun.ai`, a redirect-disabled named HTTP client, bounded page/recommendation sizes and canonical trailing-slash device URLs.
- HTML parsing uses database-backed regex definitions (`builtin.canirun-model-card-pattern` and `builtin.html-data-attribute-pattern`) rather than embedding scraping policy in Razor.
- GPU names are converted into editable device slugs without hardcoding the user's RX 7900 XTX or RTX 3060 examples.
- Recommendations retain device endpoint/host identity, so compatibility evidence from multiple physical hosts is never collapsed into a single global result.
- The current CanIRun HTML supplied for validation yielded all 83 model cards through the seeded parser shape.

## Knowledge-owned provider bootstrap and model installation

- Added offline Knowledge profiles for Ollama and LM Studio / llmster on Windows, Linux and macOS. Detect/install/start/list-model/install-model commands remain Knowledge data, not provider-specific shell policy hardcoded in the setup service.
- The existing Knowledge-file seed is losslessly evolved to include `docs/reference/ai-provider-installation.md` only when the persisted collection is still exactly the former built-in default; user-edited Knowledge file lists remain authoritative.
- Added `AiProviderBootstrapService` using the existing Knowledge, database-backed regex, JSON policy, bounded console service, configuration writer and provider registry.
- Provider detect and local model listing are read-only. Provider installation, startup, endpoint registration and model download require the maintained consequential-action / fresh-human-confirmation path.
- Ollama is registered through the existing Ollama host configuration. LM Studio / llmster uses LocalGPT's canonical `openai-compatible` identity at its loopback `/v1` endpoint.
- Model installed-state matching is provider-kind + normalized-endpoint + model-name qualified so the same model name on a different physical/runtime endpoint cannot satisfy the selected provider accidentally.

## Shared cross-platform Terminal / Console engine

- Added one `ConsoleCommandService` for Direct executable, PowerShell/pwsh, Bash and Windows Cmd adapters instead of separate shell-specific application architectures.
- Auto shell chooses PowerShell on Windows and Bash on Unix-like hosts; explicit Direct/Bash/PowerShell/Cmd requests remain available where appropriate.
- Commands run with redirected stdout/stderr, no shell execute, bounded timeout/capture/event history and best-effort process-tree cleanup on timeout/cancellation.
- Consequential commands require fresh user confirmation. The generic `localgpt.console.execute` DXFunction is not automatic-safe; read-only detection/listing capabilities use their dedicated setup functions.
- Normal diagnostics log operation identity/status but deliberately omit command text, arguments and captured output.
- Recent bounded command output is reused by the existing Chat ASCII console whenever no ASCII game owns the surface; starting a game retains precedence.

## Setup Controller and DXFunction surface

- Added controller/service/DXFunction wiring for setup status, hardware detect/HWiNFO/save, attributed CanIRun lookup, provider profile list/detect/model-list/install/start/configure/model-install, benchmark-team creation and console history/execute.
- New capabilities are ordinary `IDxAiFunctionHandler` implementations, so runtime discovery/catalog synchronization, parameter-schema validation, direct/automatic policy and human-approval behavior stay on the existing DXFunction path.
- Generic terminal execution is intentionally not used as a shortcut inside the setup Council; maintained provider/setup DXFunctions remain the preferred AI-facing actions.

## Hardware-curated benchmark team

- The setup assistant lists provider-qualified models from configured/reachable endpoints and can map optional CanIRun evidence to provider model identifiers through Knowledge-owned aliases.
- Existing `ProviderModelReviewerPolicyService` ranking is reused so known stronger models are preferred as initial curator/reviewer candidates even when CanIRun is not used.
- A reviewed selection creates or refreshes the user-owned `hardware-initial-benchmark` team by cloning the maintained `adaptive-model-benchmark` template through the existing `ICouncilTeamConfigurationService`.
- Curator/director/reviewer/auditor/analyst/synthesizer-style roles use the stronger preferred pool; smaller models remain valid members of the broader benchmark subject pool.

## Database / compatibility

- No 3.0.5 EF schema migration is required. All 26 migration/snapshot files are byte-identical to 3.0.4.
- Existing 3.0.4 Remote Control, user-defined DXFunctions and Knowledge-backed toolchain architecture remains the integration substrate used by the new setup workflow.
- The original 20 explicit `@rendermode` directives, all 137 browser JavaScript files, and the Wire Protocol 2.1.1 tree remain byte-identical to 3.0.4.

## Version

- LocalGPT: 3.0.5
- LocalGPTWebviewWrapper: 3.0.5
- LocalGPTInstallerConsole: 3.0.5
- Wire Protocol: 2.1.1 (unchanged)
- Council team seed version: 26
