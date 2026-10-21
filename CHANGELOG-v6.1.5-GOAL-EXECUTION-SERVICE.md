# LocalGPT 6.1.5 — Goal execution service, roadmap step 1

## Scope and base

Based on the supplied, prior source-only LocalGPT 6.1.4 archive. This release deliberately implements **roadmap step 1 before further work on steps 2–6**. The source is **not certified as compiling or running**: neither .NET nor PowerShell is installed in the review environment.

## Implemented

- Added `IGoalExecutionService` and dependency-injected `GoalExecutionService` **above** `IMultiModelCouncilService`, rather than an independent special-case workflow. Both `PlanAsync` and `ExecuteAsync` accept `GoalExecutionRequest` typed contracts and return `GoalExecutionResult` with the selected saved team, model identities, available functions/skills, missing requirements, execution status and source-attributed evidence.
- Goal planning discovers actual executable DXFunctions and online organic skills through the existing authoritative `IRuntimeCapabilityDirectoryService`; it loads enabled, database-backed Council definitions through `ICouncilTeamConfigurationService`. Matching uses metadata from these catalogs and the user's current goal, not a hardcoded task-family switch. The text overlap matcher is **conservative and lexical**, not a semantic AI planner; selected team preferences are respected and an unavailable explicitly selected team blocks rather than falling back silently.
- Saved team role bindings and candidate availability govern automatic model selection when no provider model was selected. The `MaxParallelModels` concurrency budget is not misused as a number of participants. The original Council continues to enforce role policies, host scheduling, tools, approval, retries and rounds.
- `CouncilChatClient` now enters the goal service on **both** streaming and non-streaming execution. The streaming entry can pre-resolve an installed/configured model for an otherwise empty selection. `GoalText` captures the last user turn separately from the bounded history prompt; the latter remains the execution payload.
- `OneWireOperationExecutor` now routes normal PublisherStudio/1-Wire Council requests through `IGoalExecutionService` and serializes the original Council result for compatibility. The Council game-session bootstrap occurs in the goal orchestration service immediately before Council execution, so the correct configured team is resolved first.
- Completed Council results carry a non-recursive `GoalExecutionSummary`. Evidence entries derive from Council step status, service-produced workspace-read metadata and Council artifact records. Missing mandatory capabilities block execution; external effects requested through functions remain `NeedsVerification` unless independently verified. Model text alone is not treated as a tool receipt.
- Added four source-only regression tests for service/DI wiring, dispatch boundaries, live capability lookup, model route selection, approval preservation, chat and 1-Wire entry points. Updated previous source-only release tests to expect `6.1.5`.
- Increased the LocalGPT project version **6.1.4 → 6.1.5** (single-digit second/third version fields).

## Deliberate limits and pending later roadmap steps

- The directory-backed planner uses lexical correlation of goal terms and saved metadata; it is not yet model-semantic goal decomposition, self-replanning, or durable goal execution. A goal may correctly fall back to the saved `general` team when lexical evidence is insufficient.
- Step 2 (new approval-gated Council mutation functionality beyond what 6.1.4 already included), step 3 (universal model execution adapter registry), step 4 (full PublisherStudio executable media surface), step 5 (cross-app typed event/artifact pipeline) and step 6 (durable resumable ledger and end-to-end evaluations) remain outside this release. Earlier partial work remains untouched.
- Completion evidence is currently run-local, with a summary attached to the existing Council result. There is no restartable goal ledger or automatic external-effect verification.
- A caller still controls the existing Council approval flags; goal routing does not grant approval, install a model, mutate teams or claim an unavailable handler is executable.

## Validation constraints

`python -m unittest discover -s tests/static -p 'test_*.py' -v` passed with 9 tests. Existing and added tests are **source checks**, not a substitute for C# compilation or PowerShell maintenance gates. All `build` PowerShell guards and `Directory.Build.targets` are preserved unchanged. No .NET build, NuGet restore, live model/provider execution, database migration or runtime 1-Wire interoperability test was attempted. Run the unmodified mandatory PowerShell gates and the normal .NET/DevExpress build on a supported host before distributing binaries.
