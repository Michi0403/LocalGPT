# LocalGPT 2.1.20 — onboarding, development teams and chat formatting

## Added

- Persisted first-run onboarding with bounded loopback model discovery, installer profiles, documentation links, Council-team links and user-confirmed dismissal.
- Read-only onboarding controller and `localgpt.onboarding.status` DXFunction.
- Seeded `Adaptive Ollama Benchmark Council` for already-installed models, independent code-curator scoring, performance evidence and non-destructive preset recommendations.
- Seeded low-latency `GameDirector Runtime Council` with player-controller, creature-subdirector, reactive-object-subdirector and runtime-verifier roles. Proposals remain non-authoritative until the deterministic GameDirector accepts them.
- Seeded development teams for modern hosted C#, PowerShell build systems, Java services and Minecraft projects. Each follows preflight/regex, architecture, implementation, policy, build, curation and release rounds.
- First-run model presets for low-B games, code curation and benchmark candidates.
- Chat query-string quick starts for selecting a seeded team and model preset.
- XML documentation for the newly maintained onboarding, formatting, Council configuration and seed contracts, including primary-constructor parameters, properties, method parameters and return/Task behavior.

## Corrected

- Council prompt reconstruction no longer recursively feeds complete prior Council transcripts, model-thinking blocks or LocalGPT process panels into later runs. It retains at most twelve unique recent user turns and the latest cleaned assistant consensus.
- User messages containing older generated `AI Council request` wrappers are reduced to the innermost user-authored request.
- Harmony, think-tag and plain-text provider output now travels through the same safe Markdown path. Model-owned HTML is encoded; only LocalGPT emits trusted disclosure/process markup.
- Model thinking is rendered inside a Markdown-capable disclosure instead of a raw preformatted block, improving consistency with the final answer while preserving separation.
- Windows `Mark of the Web` is removed from repository-local DocFX inputs before tool restore. An isolated tool-path fallback is reused or installed when the manifest restore fails.
- Diagnostic builds preserve the preceding help site and remain usable when DocFX cannot be restored; Release/PDF-required builds still fail explicitly.
- The invalid XML comment placement in `Program.cs` remains corrected as a normal source comment.

## Preserved boundaries

- First-run actions do not install, download, start, stop or remove models automatically.
- Benchmark and development teams do not overwrite existing presets or execute consequential workspace actions without the existing approval boundaries.
- The GameDirector remains the sole authority for committed game state.
- The separately versioned `LocalGPT.WireProtocolVersion` package remains unchanged.
