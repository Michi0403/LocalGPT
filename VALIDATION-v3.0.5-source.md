# LocalGPT 3.0.5 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build or GitHub/repository access was used. PowerShell is not installed in the inspection environment, so the user's Windows build remains the authoritative compile and `.ps1`-guard confirmation.

## Targeted 3.0.5 validation

- `build/audit_release_3_0_5.py`: 100 checks covering the cross-shell console, human-confirmation policy, ASCII-console reuse, multi-host/multi-GPU hardware, CanIRun opt-in/attribution/network/parser policy, Knowledge-owned provider bootstrap, model listing/install/configuration, provider+endpoint qualified installed-state matching, setup Controller/DXFunctions, benchmark-team creation, AI-guided setup Council and Install-guide reopenability.
- Application architecture audit passes with no new application-static policy violations.
- Service resilience audit passes for 2,086 service methods with owned logged try/catch diagnostics; 29 yield methods and 3 direct Program/Startup methods remain governed by dedicated policies.
- Async continuation audit: 250 source files, 2,817 await tokens, 2,545 `ConfigureAwait(false)`, 63 explicitly allowlisted renderer-affine `ConfigureAwait(true)`, 205 configured async disposals (all false), and 4 configured async streams.
- Provider-qualified Council audit: 282 checks.
- Configurable Council behavior policy, X-Round/heartbeat, code-generation/DXFunction, benchmark/rejoin, ASCII-console, human-visible entity, documentation/1-Wire and strict async/Council Teams regression audits pass.
- XML/DocFX coverage and quality passes for 8,295 direct C# declarations across 567 maintained source files.
- Localization catalogs remain byte-identical to 3.0.4: 1,973 keys with exact key parity across `en-US`, `de-DE`, `es-ES`, `fr-FR`, `ja-JP`, and `uk-UA`.

## Windows-guard source-equivalent checks

- The current system-variable initialization PowerShell rule was reproduced against the final source tree: 0 new violations relative to its maintained baseline.
- The current text-service ownership PowerShell rule was reproduced against Components/Controller sources: 0 new direct UI/controller string/regex ownership violations.
- No changed/new 3.0.5 source file introduces an iterator (`yield`) method, so the existing iterator baseline is not expanded by this release.
- Operational diagnostics and InteractiveServer requirements are also covered by the maintained architecture/render-mode sources; the actual PowerShell wrappers must still be confirmed by the user's Windows build.

## CanIRun parser validation

The current CanIRun HTML supplied with the setup request was parsed using the exact seeded model-card/data-attribute regex shapes used by the service.

- 83 `<article data-model-id=...>` cards were found.
- 83/83 cards produced model attribute dictionaries.
- The parser reads model id/name/provider/grade/status/score and selected quantization/VRAM metadata without needing GPU-specific source code.
- The service uses the canonical `/device/<slug>/` URL form because automatic HTTP redirects are intentionally disabled.
- Network access remains explicit opt-in and the UI credits `CanIRun.ai by midudev`.

## Provider bootstrap validation

- Provider installation profiles are local Knowledge records rather than application `switch` policy.
- Profiles cover Ollama and LM Studio / llmster on Windows, Linux and macOS.
- Detect/model-list actions are read-only; installation/start/model-download are confirmation-gated and use the common console service.
- LM Studio uses LocalGPT's existing `openai-compatible` provider identity with `http://127.0.0.1:1234/v1`; Ollama uses its existing host registry at `http://127.0.0.1:11434`.
- The provider Knowledge document is included in the maintained KnowledgeFiles seed. Existing databases receive that new built-in document only when their persisted list still exactly matches the former untouched built-in default; user-edited lists are not overwritten.

## Hardware / model / benchmark validation

- The reviewed setup hardware object is a list, with endpoint/host identity per GPU row.
- Save groups accelerators by physical endpoint/host and persists the complete GPU list through existing host hardware persistence.
- Local detection and HWiNFO import both populate the multi-GPU list while preserving legacy primary-GPU compatibility fields.
- CanIRun recommendation rows carry endpoint/host identity; recommendations from separate GPU hosts are not globally merged.
- Installed model checks require matching provider kind and normalized endpoint before model-name comparison.
- The initial curator pool reuses `ProviderModelReviewerPolicyService`; CanIRun can add evidence but is not required for avoiding weak default curators.
- Benchmark team persistence reuses the existing `adaptive-model-benchmark` template and `ICouncilTeamConfigurationService`.

## AI-guided setup team

- New maintained team key: `initial-setup-assistant`.
- New starter key: `initial-setup-council-start`.
- The team uses `initial.setup.*`, `human.collaboration.request`, toolchain listing and console history capabilities and explicitly preserves offline-first, multi-host and human-confirmation contracts.
- Council seed advances from 25 to 26 for this new maintained team. User-modified team rows continue to use the existing seed-preservation behavior.
- The Install guide exposes the AI-guided Council link only after at least one local installed model is visible; before that the same manual functions can bootstrap the first runtime/model.

## Historical regression chain / EF

All maintained source release audits from `build/audit_release_2_8_5.py` through `build/audit_release_3_0_5.py` pass on the final source tree.

- The 3.0.0 EF model/snapshot audit covers 45 DbSet entity types and 644 persisted scalar properties.
- 3.0.5 introduces no EF model change: all 26 migration/snapshot files are byte-identical to 3.0.4.
- No database reset or new migration is required for 3.0.5.

## Stable 3.0.4 invariants

- Explicit `@rendermode` directives: 20/20 byte/content-identical.
- Browser JavaScript: 137/137 files byte-identical.
- Wire Protocol tree: 3/3 files byte-identical; protocol remains 2.1.1.
- EF migration/snapshot tree: 26/26 files byte-identical.
- Localization JSON: 6/6 files byte-identical.
- No default online connector, provider installation, model download or CanIRun lookup is enabled merely by installing/starting LocalGPT.

## Compile note

No .NET compiler was run in this environment by design. C# structural/architecture/XML/source regression checks are not a substitute for the user's Windows compiler. The next real build should therefore be treated as the authoritative compile result, exactly as with 3.0.2/3.0.3.
