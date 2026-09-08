# LocalGPT 3.9.9 source validation

## Scope

This handoff was validated only from the supplied LocalGPT 3.9.8 source ZIP, the supplied runtime HTML/log capture, and the supplied progress PDF. No `dotnet build`, `dotnet publish`, Roslyn/.NET syntax runner, GitHub access, remote repository access, or native release packaging was performed. The user's local build remains the authoritative C# / Razor compiler and runtime gate.

## Repair targets

The supplied `/install` capture showed two separate model-source/install gaps: CanIRun.ai recommendations carried a clickable CanIRun.ai model source, while the combined provider model rows did not consistently expose their provider-owned model page; hardware-fit rows without a previously known provider ID also remained manual-review-only with no action that could safely resolve and install them.

3.9.9 gives every combined provider/model row a provider-owned detail link when an exact provider ID is known, otherwise a provider-owned search/catalog link. An explicit **Resolve & install model** action searches the selected provider's maintained public catalog and only proceeds when exactly one conservative normalized identity match is returned. No new broad heuristic provider-ID inference was added; ambiguous results remain visible for review and are not installed.

The supplied command-console capture contained raw ANSI cursor/private-mode sequences and mojibake in Ollama's spinner/progress output. The shared console path now requests UTF-8 decoding, normalizes common redirected-terminal controls into a final plain-text line state, and renders the setup console with non-wrapping monospace `white-space: pre` geometry. Chat uses the same `ConsoleCommandService.GetRecentDisplayText` feed, so the normalization is shared rather than duplicated in one page.

The existing provider/bootstrap and Ollama process services were retained. `/install` now adds one explicitly confirmed guided Ollama path that installs only when missing, re-detects, starts only when stopped, and registers the provider endpoint only after a running state is observed. A macOS installer-success / executable-not-yet-visible state stops with an explicit first-launch/provider-source recovery message rather than claiming completion.

The Council/team reconciliation and self-cleaning path was intentionally left unchanged because the supplied runtime observation reported that it recovered the stale team interface state successfully.

## Executed source checks

- `build/audit_release_3_9_9.py` — passed: version surfaces, one-digit minor/patch-slot policy, provider model-link/resolution contracts, guided Ollama sequence, UTF-8/ANSI console normalization, setup console geometry, localization parity, routed render-boundary ownership, and source-package hygiene.
- `build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `build/audit_cross_platform_boundaries.py` — passed: 22 checks; no platform leaks detected.
- `build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 259 source files: 3,072 await tokens, 2,715 `ConfigureAwait(false)`, 135 renderer-affine `ConfigureAwait(true)`, and 217 explicitly configured async disposals.
- `build/audit_codegen_dxfunction_wiring.py` — passed.
- `build/audit_configurable_behavior_policy.py --root .` — passed.
- `build/audit_provider_qualified_council.py --root .` — passed: 282 checks.
- `build/audit_provider_stream_repetition_policy.py` — passed.
- `build/audit_xround_wiring.py` — passed.
- `build/audit_council_sql_seed.py` — passed: 60 deterministic current-schema rows and idempotency checks.
- `build/audit_service_resilience.py --root . --product localgpt` — passed: 2,242 service methods covered by the maintained resilience audit.
- `build/Assert-XmlDocumentationCoverage.py .` — passed: 10,403 direct C# declarations across 656 maintained source files and 793 direct Razor `@code` members across 45 component types.

Direct routed-page ownership check: 15 routed pages total, 14 explicit routed `InteractiveServer` boundaries, with only `Error.razor` intentionally static. `InitialSetupAssistantPanel.razor` remains render-mode-free and inherits `/install`'s existing interactive circuit.

## Limitations

Static/source checks do not prove C# or Razor compilation, provider-site HTML stability, macOS installer execution, or an actual model download. The next user-side Debug/Release build and a runtime pass through `/install` remain the required final gate.
