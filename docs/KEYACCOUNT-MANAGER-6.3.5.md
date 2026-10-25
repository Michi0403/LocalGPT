# Keyaccount Manager for User: operational and acceptance contract (LocalGPT 6.3.5)

## Why

All regular Council entry points use the existing GoalExecution service rather than adding a hardcoded texture-model agent. Each completed Council subrun is checked against the **original human goal**, not merely whether the last participating model produced text. The reviewer can propose another distinct objective using the same saved Council team and existing DXFunction approvals.

## Runtime controls

- In `/chat` → Council team configuration, **Keyaccount Manager: check full user goal** is on by default.
- **Keyaccount extra rounds**: 0 uses the current DB value of `GoalKeyaccountDefaultContinuations` (seed: 3); positive values 1–100 give a bounded additional-work allowance for this goal. These are separate Council subruns, not the internal critique count.
- **Required output files**: optional numeric floor. If you require 40 SVGs or OpenSCAD source files, enter `40`. The floor counts unique artifact names with a successful technical receipt showing an existing file. Naming/format and actual visual quality still need human verification.
- Runtime policy `GoalKeyaccountManagerSystemPrompt` contains the editable role briefing. `GoalKeyaccountReviewerModelKey` optionally identifies a provider-qualified configured reviewer model; when empty, the role uses an available model from the current Council. Its tools are disabled; it cannot grant itself permissions.
- Selected team roles, function policies, hardware roads and approval queue are not replaced by this manager.

## Inspect and extend a user goal

- The run's durable state is stored in LocalGPT's per-user `GoalExecution/<goal-guid>.json` path, not as an AI self-reported memory.
- `GET /api/goals/{goalId}` shows planned objectives, retained Council receipts, source evidence, advisory reviews, pending objective and observed state.
- `POST /api/goals/{goalId}/continue` accepts JSON like `{"objective":"Create five distinct additional requested source textures, excluding existing assets","additionalRounds":20}`. The endpoint is protected by `HumanApprovalRequired`. Omit `objective` to accept the saved pending objective when there is one. Neither call resurrects old permissions or replays an earlier committed operation.
- `POST /api/goals/{goalId}/resume` is still the separate human-approved route for interrupted uncertain work. Do not confuse continuation of **new** tasks with a replay of an interrupted tool call.

## Expected behavior

1. Produce an initial technical Council result through a regular, selected team. An empty answer, failed model or unavailable capability remains a failure/blocked condition.
2. After each executed goal subrun, ask the configured reviewer model to compare the original user goal and actual receipts. Record the review in the durable ledger. The review's words are not new proof of created outputs.
3. If **incomplete**, **safe**, and a distinct objective is proposed, append a new plan step under the original team while the configured budget remains. Subsequent work goes through the *ordinary* Council DXFunction approval and audit path.
4. If reviewer is unavailable, effect is uncertain, the target is unverified, or the batch budget is exhausted, expose `NeedsVerification` or `NeedsApproval` rather than claiming success. User can inspect receipts and approve a distinct continuation.
5. A user may explicitly request additional batches later; no secret maximum lifetime number of batches is imposed. Each batch remains bounded so stop/cancellation remains meaningful.

## Concrete test example: Unreal Dortmund assets

The supplied game ZIP has a relatively small `wwwroot/vector-textures` material library and several `docs/ImportedOpenSCAD` source models; existing image patterns are **not** evidence that the newly requested assets were created. Select a Council team with live, approved file-generation tools. Ask for several new **distinct** SVG or `.scad` files in the existing game workspace, exclude names already present, set a numerical output target and a larger round budget. Inspect `GoalExecution` receipts and the actual generated files. Provide further correction/curation tasks as new continuation objectives. New source files should only be imported into the game after the user reviews them and any project revision approvals are satisfied.

## Windows acceptance checklist (not executed here)

- Check C# build and Razor component compilation under the source-declared .NET 10 SDK. Ensure no new analyzer/architecture guard failures.
- Confirm normal General, Learning, Coding, Development, Model Council, Minecraft Mod Builder and 1-Wire Council requests all invoke the Keyaccount review. Diagnostic smoke and recurring screen-reader calls are intentionally not user-goal sessions.
- With two configured installed Ollama models, request ten distinct source files and a budget of twenty, ensure that unfinished goal work is not declared complete after two textual replies.
- Force Ollama connection refusal/timeout and reviewer JSON corruption; check `NeedsVerification`/failure, recorded reasons, and bounded retries instead of false success.
- Stop a long-running child Council and restart LocalGPT; earlier successful receipts must not be replayed automatically.
- Verify human approvals on DXFunctions, on a new `/api/goals/{id}/continue` request and on interrupted `/resume`; no approval is manufactured.
- Confirm a deliberately absent file does not count toward the user-selected output target; inspect visual quality and OpenSCAD validity separately.
- Confirm no unrelated seeded configurations or existing user-team modifications are overwritten by this new seed-owned prompt.
