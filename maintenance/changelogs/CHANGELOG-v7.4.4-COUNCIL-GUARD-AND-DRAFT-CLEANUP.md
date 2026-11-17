# LocalGPT 7.4.4 — Council observer guard and explicit draft cleanup

Source candidate; maintainer-side PowerShell and compiler validation pending.

## Build repair

- Correct both stale mutable-run-ID assertions in `Assert-CouncilXRoundWiring.ps1`. The observer must append, update and touch the stable originating Chat ID; the isolated worker must complete that same parent session. Reject direct use of the mutable execution ID for these live-session operations. Keep existing observer separation, bounded channel, cancellation and ownership contracts. Emit all findings before terminating the guard.
- The 7.4.3 source implementation already uses `chatRunId`. Do not revert it to `request.RunId` to satisfy the old assertion. The latter can change during durable GoalExecution.
- The reported second and third builds still compile abandoned Qwen drafts absent from the clean archive. Add an explicit preview-first PowerShell maintenance command to quarantine the ten supplied drafts outside the repository, using archive fingerprints and explicit consent for locally modified files. No source deletion, exclusion, database change or automatic build-time rewrite. See `maintenance/CONTEXT-DRAFT-CLEANUP.md`.

## Validation

The owner log confirms the stale observer guard was the first blocker and old ContextManagement files were the subsequent blockers. Both stale positive assertions were repaired; all remaining checks are retained. The cleanup script was reviewed against the supplied draft inventory. No PowerShell runtime is available here; no .NET build or GitHub access attempted. Run the maintained Council guard and the normal repository gates after quarantine. Do not certify the package from source inspection alone.

PublisherStudio is unchanged. The existing context, preset and delegated Chat source changes remain intact. Full compaction, distributed specialist routing and individual learning remain tracked in TASKS.md.
