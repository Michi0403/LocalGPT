# LocalGPT 4.5.7 — Chat approvals, ASCII combat and compiler orchestration

## Chat and review interaction

- Projected pending AI/human collaboration requests directly into the active `/Chat` session instead of requiring the separate approvals panel for every decision.
- Reused the existing durable `HumanCollaborationRequest` contract for inline Approve/Decline, AI-suggested response buttons and optional free-text replies rather than creating a parallel approval store.
- Added the same pending-request interaction to the ASCII console in conversation mode, active game mode and a non-game **Council Work Control** Game-plane surface, so function/review requests stay actionable while the terminal is open even when no deterministic game exists.
- Preserved deferred DXAIFunction execution: approving a protected function request queues the already-described operation instead of bypassing the existing review boundary.

## DevExpress selection and numeric editor repair

- Reworked Chat provider/session selectors around strongly typed `LocalGptSelectionOption<string>` data so displayed labels and persisted string values no longer depend on mixed object/string inference.
- Raised active DevExpress adaptive dropdown portals above fixed configuration/dialog surfaces and restored pointer input plus bounded list scrolling.
- Removed the compact-editor misuse of `DxRangeSelector`. Shared bounded numbers and Council hardware load overrides now use `DxSpinEdit` with explicit min/max/step contracts.
- Kept dense Council and setup surfaces on the one-column/readable layout direction introduced in 4.5.6.

## InteractiveServer responsiveness and cancellation safety

- Reduced renderer work during Council streaming by avoiding full ASCII transcript reconstruction when the Game/operator plane is visible.
- Cached ASCII browser attachment, input, scaling, frame, animation-sequence and layout signatures so repeated Blazor renders do not re-run equivalent JS interop.
- Coalesced high-frequency console-output render notifications instead of scheduling a render for every process-output fragment.
- Renderer-affine component callbacks and JS interop continue on the Blazor renderer context; background/service work remains free to use the existing non-renderer continuation policy.
- `TaskCanceledException`, `JSDisconnectedException` and disposal during navigation/reconnect are treated as transient UI lifecycle termination rather than fatal Chat/ASCII failures. Attachment state is reset so the next healthy render can reattach.
- Chat composer/reconnect browser state is only republished when its effective state changes.

## Kernel Creature Tournament actors, combat and recovery

- Extended tournament fighter state with creature species, style, form, trait and voice metadata supplied by the bounded trainer/creature workflow.
- Added persistent ASCII trainer and creature sprites. Trainer appearance is stable per model identity and creature shape is derived from bounded species/form/style/trait descriptors.
- Every deterministic exchange now produces a bounded multi-frame presentation sequence: command, movement, arena clash, engine impact, recovery and bracket/next-exchange transition. HP, damage, guard, elimination and winners remain engine-owned.
- Added collision-safe creature display names so two models selecting the same nickname remain distinguishable without restarting naming deliberation.
- Preserved the feature-derived single-name guidance and repetition watchdog introduced in 4.5.6.
- Tournament compact roles now use `RetrySameThenEligibleRolePool` recovery with bounded attempts. A failed provider/model slot can be fulfilled by another eligible configured member while the original slot/pair identity remains auditable.
- Added `RecoveryTargetModelName` to recovered Council steps so downstream trainer/creature association uses the role slot being replaced rather than accidentally rebinding to the recovery model's identity.
- Supplied Council team seed version advances from 34 to 35 so maintained, non-user-modified templates can receive the failover policy.

## Compiler, repository curation and release orchestration teams

- Added a preseeded **Program Compiler & Repository Curator** Council team for ZIP/text intake, regex-assisted structure analysis, bounded reconstruction/planning, source curation, toolchain selection, build verification and human handoff.
- The compiler workflow explicitly supports repeated review rounds and narrow file slices for smaller local models; curators can reject bad reconstruction/changes instead of automatically accepting generated source.
- Added a preseeded **Project Maintenance & Cross-platform Publisher** Council team that freezes project/version/revision identity, evaluates trusted 1-Wire peer OS/performance/capabilities, dispatches protected platform work through existing public-service/DXAIFunction paths, assigns documentation/PDF work to the strongest eligible host and curates the final artifact set.
- Release planning uses existing project version/artifact metadata and the convention `artifacts/<version>/<platform>/<runtime-or-package-kind>/` unless the project stores another approved artifact subdirectory. No new database column is required for this release.
- `project.maintenance.get` now exposes project current-version, version-history and artifact metadata to the maintenance/publisher Council.

## Compatibility

- No EF migration.
- No wire-protocol change.
- InteractiveServer declarations remain at the maintained baseline: 15 direct InteractiveServer pages/islands plus 5 explicit non-prerender InteractiveServer islands.
- The two circuit-independent native reconnect/reload buttons in `App.razor` remain intentional.
- PublisherStudio source is unchanged in this release.
