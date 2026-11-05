# LocalGPT 6.3.5 — Keyaccount Manager for User, durable Council completion review

Base: **LocalGPT 6.3.4**, using the full source ZIP. PublisherStudio and Unreal Dortmund game repositories are unchanged. This is a **source-only candidate**; no .NET, Windows, Ollama or DXFunction end-to-end run was performed.

## Motivation and observed failures

- The uploaded `council-20261024-233340-55f0e90c08f44dbca2608cf7de8fdf0f.md` requests many distinct game SVG textures and improvements rather than another description of the same small batch. Its normal General Council consists of only three configured workflow steps, and the transcript shows both participant failures and unavailable Ollama connections. Missing approved organic skills are explicitly reported.
- The GoalExecution service was already used by normal DXAiChat Council chat and 1-Wire Council requests. However, its completion test accepted nonempty Council text with no technical proof of generated files. The semantic plan default allowed six steps and the replan default allowed two failures; these were not a generic final-user-goal completeness review.
- The `/model-council` and Minecraft Mod Builder pages bypassed GoalExecution entirely and called the Council directly. Those normal interactive routes now use the common goal service. Diagnostic smoke/benchmark and a recurring screen-reader service remain intentionally low-level, not user-goal workflow entry points.

## Implemented

- Added a **Keyaccount Manager for User** review after *each executed GoalExecution Council subrun*, independent of the selected persisted team: General, Learning, Coding, Development, game, and custom teams all share the same code path. The reviewer uses an existing configured installed model, no tools, and a database-owned system prompt. A separate provider-qualified reviewer model can be configured in `GoalKeyaccountReviewerModelKey`.
- Reviewer evaluates original user intent, completed substeps, current answer, Council warnings, and actual technical receipts. Its JSON is a **proposal** (`complete`, `nextObjective`, `needsHumanReview`, `reason`), NOT a tool receipt. If unavailable or malformed, the goal is left unverified rather than declared complete.
- A model-proposed **distinct next objective** can be appended to the durable journal and executed through the same existing selected Council team while the user-configured continuation budget remains available. New substeps get fresh Council run identifiers and their own approval checks. Previous completed work is not replayed, and there is a finite budget per batch.
- Added configurable `KeyaccountContinuationLimit` and `KeyaccountExpectedArtifacts` to normal Council requests. `/chat` exposes a default-on review toggle, an additional-round budget (`0` means the database default, up to 100 in one batch), and an optional minimum count of verified, distinct output files. The reviewer cannot declare completion below that requested file count.
- Added `POST /api/goals/{goalId}/continue` behind `HumanApprovalRequired("goal.execution.continue")`. The operator can submit another *distinct* objective and an additional batch allowance (1–100); the existing checkpoint and prior approvals are retained. It is not an automatic replay/approval or an unrestricted background loop. Existing GET and approved interrupted-resume endpoints remain intact.
- Added a durable `KeyaccountReviews` trail, `KeyaccountPendingObjective`, review status/unfinished wording in the returned Council result, and a `GoalExecutionSummary.KeyaccountReview` projection.
- Generated artifact observations now require an actually existing local file and no artifact-declared missing requirements. A nonempty download URL or a Council assertion alone is not proof of a created file.
- Added a configurable seed-backed reviewer instruction and default continuation allowance through the standard runtime policy seeder. Existing runtime enum ordinals and earlier 6.3.4 seed repair contracts are retained.

## Important limitations — not falsely claimed as repaired

- **This does not itself implement an SVG renderer, OpenSCAD mesher or automatic game import**. LocalGPT must still have configured and approved capabilities that actually create the required artifacts. A model claiming it wrote 100 textures is not accepted as evidence.
- A richer graphical asset gallery for viewing/editing/rating outputs, explicit DXFunction invocation receipts for every external effect, and reliable Windows Ollama availability are separate integration requirements. The existing Council and asset review facilities are unchanged. Technical artifact presence does not imply high-quality visual content.
- The reviewer is a model assessment of completeness, not a mathematical guarantee that the user's intent was fulfilled. Bounded iterations and approval/verification gates intentionally stop uncertain work. No unbounded automatic loops or silent permission escalation.
- The new `/api/goals/{id}/continue` route supports operator-driven batches; a dedicated graphical continuation editor is not part of this source update. The Chat settings control the initial batch.

## Offline validations

- 52 repository Python source/regression tests passed, including 8 Keyaccount regression tests. Historic version assertions were updated for 6.3.5 without removing earlier seed-repair assertions.
- No .NET compiler, Razor compilation, database upgrade, networked model, source-generated OpenSCAD/SVG or release installer was exercised. Source-level checks are not production acceptance.
