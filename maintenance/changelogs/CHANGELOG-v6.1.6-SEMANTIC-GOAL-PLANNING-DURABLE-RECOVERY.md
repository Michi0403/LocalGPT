# LocalGPT 6.1.6 — Semantic goal planning and durable recovery

## Base and scope

Source archive derived exclusively from LocalGPT 6.1.5 (supplied ZIP); no GitHub or .NET build environment was used. This release addresses the explicit 6.1.5 limitations: lexical-only goal planning, no semantic task decomposition/replanning, and no goal-level crash recovery. Previous source and PowerShell architecture gates are preserved.

## Implemented

- Model-assisted JSON goal planning via the existing `IProviderModelRuntimeService` with automatic tools **disabled**, provider-qualified candidate selection and a configurable database-owned system prompt. The proposal is validated against live enabled Council teams, registered executable DXFunctions and online organic capabilities before use. The previous lexical/catalog path remains as a declared fallback when planning inference fails.
- Bounded ordered subgoals with configurable maximum step and replan counts. Explicit caller team selections remain authoritative. Completed subgoals are not re-executed on a resumed goal. Each subgoal executes through the **existing** configured Council, not through new hardcoded executors or approval bypasses.
- Per-user, write-ahead goal ledger with exclusive per-goal execution leases and atomically replaced JSON checkpoints under `LocalGptApplicationDataPaths`. Saves original non-secret run preferences, provider selection keys (not provider credentials/endpoints), plan, completed outputs and service-observed evidence. Commits a step's Council result before starting the next step.
- Conservative crash recovery: an incomplete Council call becomes interrupted after process restart; retry requires explicit operator approval via `POST api/goals/{goalId}/resume` and the existing `HumanApprovalRequired` action filter. A `GET` endpoint inspects the checkpoint without running tools. Previously completed subgoals are reused. Prior approval flags are **not** restored as granted after a restart.
- Safe semantic replanning for a removed team before invocation, or for a failed step when no possible external effects were observed under the currently available evidence. Persisted model-generated objectives cannot register new executable handlers, team keys or provider routes. Bounded replan attempts and failure metadata are recorded in the ledger.
- Existing chat streaming preflight now retains an in-process, JSON-ignored plan so the same streamed Council run does not pay for semantic planning twice. Council result summaries include the stable goal ID independently of individual Council invocation IDs, planner provenance, checkpoint state and committed subgoal count for PublisherStudio/1-Wire clients.
- Fixed duplicate goal-service DI registration inherited from 6.1.5. Added source-level regression tests for planning, journal ordering, resume approval, DI cardinality and unchanged chat/OneWire entry points.
- Version increased **6.1.5 → 6.1.6**. No minor/patch field has two digits.

## Safety and known limitations

- The persisted journal is a **goal/subgoal boundary** journal. It does **not** reconstruct the exact internal model token stream or arbitrary in-progress DXFunction call after a crash. Any uncertain external effects require human inspection/approval; retry of an interrupted step can have side effects, even when earlier completed steps are skipped.
- Generated artifacts and tool operations are not automatically accepted solely on model prose. Subgoals with unverified consequential effects remain `NeedsVerification` until independently resolved; no claim of semantic content validation is made.
- The new version has not been compiled or exercised against real models, SQLite runtime, 1-Wire peers, or the PowerShell maintenance gates. Those tests must run on the supported .NET/PowerShell host. This source-only archive is **not a build-validated release**.
- Later roadmap tasks (universal model adapter registry, full PublisherStudio typed capability execution, shared event/artifact bus, end-to-end runtime evaluations) remain open; this change is focused on the two requested limitations.
