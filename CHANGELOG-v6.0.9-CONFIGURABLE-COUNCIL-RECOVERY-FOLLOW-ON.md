# LocalGPT 6.0.9 — user-configured council recovery and team follow-on

## Purpose

Replace the 6.0.8 game-team-specific code branches with user-editable and persisted team/step policies. LocalGPT must not infer behavior from a team's fixed key; shipped team definitions are editable templates, not runtime exceptions.

## Configurable team behavior

- New `CouncilCompletionRecoveryPolicy` and `CouncilRequiredFunctionRecoveryRule` BusinessObjects: any team can define its own required function receipt, eligible workflow step, retry count, prerequisite function receipts, and recovery instruction. Rules are evaluated in prerequisite order, not solely in list order. Missing function evidence cannot be substituted with a model's prose.
- New `CouncilTeamFollowOnPolicy` BusinessObject: optionally select the next saved Council team, provide an explicit task prompt and previous-result substitution, require verified completion, require owner approval, and set the maximum child-chain depth. Blank target disables automatic team transition. Parent project, revision, models, and attached upload-workspace references are forwarded through the existing child-Council path. Cycle protection retains the chain's ancestry and the originating depth limit across children.
- Per-workflow-step missing workspace file-read recovery attempts, source-excerpt inclusion, recovery prompt, and filename priority terms are now editable and saved. A curated upload manifest never counts as a successful model file-read DXFunction receipt.
- A team's optional completion notice and post-recovery review instruction are saved template data. The benchmark notice and game team's build/playtest recovery rules are only shipped preset data.
- Dedicated DevExpress controls on the Council Teams editor expose these settings for arbitrary teams. Source-file priority and prerequisite names use the existing injected CouncilTextService.

## Persistence / contracts

- EF Core SQLite migration `20261019220000_AddCouncilConfigurableRecoveryAndFollowOn`, matching snapshot and context mappings. New `CouncilTeamConfiguration` fields: `CompletionRecoveryJson`, `FollowOnPolicyJson`, `CompletionNoticeTemplate`.
- Complete service normalization/validation and database read/write mappings; retains user-modified team rows instead of overwriting them with a seed refresh. System seed version 46.
- The bounded retry limits are maintained in `CouncilExecutionRuntimeParametersJson` (`maximumCompletionRecoveryAttempts` and `maximumWorkspaceFileReadRecoveryAttempts`); the seed/default values are 10, while actual team retry settings are saved per rule/step.
- Technical tool-authority, receipt checks, approval gates, strict per-step/team allow-lists, cancellation, X-Round bounds, and normal child-Council identity remain intact.

## Removed runtime special cases

- Removed `team.Key == "game-project-development"` hard-coded after-workflow retry branch and its embedded game function identifiers, retry counts, repair instructions and final-curation instruction.
- Moved the benchmark-specific runtime completion message into its configurable seeded team definition.
- Moved game-specific source path prioritization from executable source selection to the game-team seed's step configuration.

## Verification limits

- Source-only checks confirmed BusinessObjects, DevExpress editor bindings, storage normalization, EF migration/snapshot, seed policies, generic executor, X-child reuse, and absence of team-key special cases in the changed executor.
- The installed environment has no .NET SDK or PowerShell. No Windows Debug/Release build, EF migration application, registered DXFunction integration run, full runtime-team chaining, or production database upgrade has been performed here. See `VALIDATION-v6.0.9-source.md`.
