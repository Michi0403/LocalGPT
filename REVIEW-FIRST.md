# 7.4.0 BUILD CORRECTION 1

Five missing XML `<param>` documentation entries fixed in the two existing service constructors. See BUILD-FIX.md and review/7.4.0-buildfix-xml-doc.diff. No runtime logic changed; a new .NET build has NOT been run.

---

# LocalGPT 7.4.0 — review only (automatic goal resources / visible agents)

BASE: GitHub main at d8783ff5e8bffb94788743bc3e2ff3816909d381 (7.3.9 regression snapshot).
KNOWN WORKING COMPARISON: 5f27971604aca190e8fde34133451b5afae9b81a (7.3.5).

The ZIP contains only 14 complete changed files at exact repository-relative paths, unified diff, and Git/SHA audit.
No files were deleted. There is no install script, Git write, commit or push.

Changes:
- GoalExecutionService calls existing DI-scoped IConfigurationStewardService directly. No new service layer.
- Runtime policy GoalExecutionResourceMode defaults to Automatic; Manual preserves exact run context and hardware settings.
- Steward uses host-specific read-only RAM/GPU discovery, exact provider-qualified benchmark profiles and bounded, user-editable hardware probe thresholds. It NEVER writes hardware settings or changes approvals.
- On unresolved capacity failures, the Steward can consult the already selected provider model as a bounded, read-only AI subagent (GoalStewardAiEnabled default true, 25-second configurable timeout). Only a validated advisory JSON object is accepted; invalid/empty/timeout response falls back to deterministic goal replanning. Chat distinguishes deterministic Steward advice from a successful Steward AI response.
- With no verified larger-context profile on a small/unknown host, GoalExecution attempts smaller goal/AI subagent packets rather than inventing an enormous memory allocation.
- With a matching measured benchmark, GoalExecution may use a larger run-local context. After a structured overflow, a high-resource host can make one bounded model-advertised capacity probe; otherwise use subagents.
- Recovery requests are explicitly tagged as GoalRecoveryCompactContext and Council builds a small role-isolated bootstrap (no gigantic preflight/replayed full memory), preserving DX tools, original goal in the durable checkpoint, team selection and actual receipts.
- The configured Council returns a known context-preflight failure early to GoalExecution instead of exhausting all team steps, attempts and X-rounds. A verified model result or artifact prevents early exit.
- GoalExecution AI, Configuration Steward, and subagent-planning decisions now appear as named status messages in Chat's existing live spooler.
- Failed automatic rejoin attempts become retryable.
- Re-enables the application static-architecture build target commented out by the failing source commit.

Important scope limits:
- Not a measured MacBook Neo / Windows integration pass. Run on real host(s) and validate the actual .NET 10 build and tests first.
- Existing /chat streaming observer ownership logic is otherwise unchanged; retrying initial rejoin is not a complete circuit-recovery guarantee.
- A failed Ollama /api/chat or OOM may still require other provider or runtime intervention; no model result is fabricated.
- No default team catalog, codegen review functions, approval engine, 1-Wire executor, learning data or application database is replaced.
- An explicitly configured model route remains authoritative. Automatic context probe is only an attempt, not proof of full KV cache affordability.

Review/usage:
1. Extract to an EMPTY REVIEW folder, not the live repository.
2. Inspect review/d8783ff-to-7.4.0.diff, verify review/SHA256SUMS.txt.
3. GitHub current main MUST still be d8783ff, or merge manually after reviewing divergence.
4. To check patch without applying: git apply --check review/d8783ff-to-7.4.0.diff
5. To review file differences: git diff --no-index original/path REVIEW/path (read-only).
6. After applying by manual choice, restore/build with dotnet build src/LocalGPT/LocalGPT.csproj --configuration Debug.
7. Confirm architecture audit runs and passes. It was disabled in regression commit.

Runtime settings: GoalExecutionResourceMode = Automatic (default) or Manual (expert); GoalStewardAiEnabled = true; GoalStewardAiTimeoutSeconds = 25. Thresholds are GoalAutoResourceProbeMinimumHostGiB and GoalAutoResourceProbeMinimumGpuGiB. These are policy values, not hardcoded model-family assumptions.
