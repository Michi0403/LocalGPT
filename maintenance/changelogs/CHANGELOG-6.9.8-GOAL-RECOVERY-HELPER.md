# LocalGPT 6.9.8 — goal recovery helper for failed members and AI host outages

## 1. Goal recovery helper (first part of the "let me do it for you" GoalExecution helper)

New scoped service `IGoalRecoveryHelperService` / `GoalRecoveryHelperService` in `Services/GoalExecution/` (namespace `LocalGPT.Services.GoalExecution`). Configured Council rounds now ask it how a failed member slot continues; before, `MultiModelCouncilService` chose the replacement itself (`SelectConfiguredRoundRecoveryParticipant`, removed).

Security boundary: the helper only chooses members of the current run. Every member of the run runs on an AI host that the run already uses, so recovery never adds a model, provider or host. The configured run members are passed in and checked again inside the helper.

Order of a decision:

1. **Same member only** when the step's policy is "same member" (`RetrySameMember`) or the step runs one assigned model. If that member's host is unavailable in this run, the slot stays empty instead of waiting on the host.
2. **Same member first** when its own retry has not happened yet (repetition-watchdog stop) and its host is usable.
3. **Another member of this run**: the role's configured members first, then members of other roles of the run when they are suitable. A member of another role is suitable when it has no failure in this role during the run and has not failed more often than it succeeded in this role in recent runs. Members reserved by another role of the same distinct-assignment group are not used unless the role allows distinct fallback. Members on a host that is down or excluded in this run are skipped.
   Ranking inside each group: a host other than the failed one first, then fewer failures in this run, then the success rate in this role (this run counts double, recent runs from the Council spooler once), then the configured/observed-health order.
4. **Paired roles** (a role with `PairedRole`, a role that another role pairs with, or a role with run pairings): another member may answer for a failed paired member only after the local user approves it in **Approvals & team**. The request is created once per role and run ("Let another member answer for a failed paired member in role … (this run)"). Until you decide, the slot stays empty and the run continues; after approval the substitute takes over the failed member's pairing (its own pairing is left out of that turn), so it plays against the right partner.
5. If nobody else is available, the same member retries again while its host answers; otherwise the slot stays empty and the reason is added to the run warnings. A slot left empty by a decision is not sent to the AI help curator; only exhausted recovery attempts still are.

Every decision is logged at Information level with the candidates and their evidence (role/run member, host, usable/failed counts in this run and in recent runs).

## 2. AI host outages

Before, every member on a host that went offline waited up to 120 s (model timeout / 6, clamped 30–120 s) before failing, one after another.

Now, inside a Council run:

- A failed request first gets **one reachability check** of its host (one Ollama `/api/tags` request, bounded by the provider discovery timeout). A host that answers and lists the model is retried at once. A host that answers but does not list the model is a model problem, not an outage, and gets the bounded same-model wait (30 s).
- The **first outage of a host in a run** gets one bounded wait (`firstHostOutageWaitSeconds`, 30 s), then one more check. A notice is shown in the member's stream.
- A host that is still silent is **marked down for this run**, and **"Exclude unreachable AI host for this run: …"** is offered in Approvals & team (risk Low; not required before completion).
- While a host is marked down, each of its members is checked **once before it starts** instead of sending a full request. If the host still does not answer, the member gets a failed step flagged `HostUnreachable`, and round recovery continues the slot with members on other hosts of this run. A host that answers again is used normally again; there is no second wait in the same run.
- If you approve the exclusion, members on that host are not started any more in this run. If you decline, the host stays in the run and is checked before each member. Later runs use the host normally again.
- One run warning per unreachable host: "AI host … was unreachable in this run. Members on other hosts already used by this run continued its work; no other host or model was added."

Outside a Council run (no run identity), the previous bounded availability wait is unchanged. Non-Ollama providers have no host check and keep their previous behavior.

## 3. Configuration (runtime policy, editable)

- `CouncilExecutionRuntimeParametersJson`: new `firstHostOutageWaitSeconds` (30) and `memberRecoveryHistoryRuns` (20; 0 switches history evidence off). Existing databases use these defaults until the values are added to the stored JSON.
- New runtime value `GoalRecoveryHelperTextsJson` (`GoalRecoveryHelperTexts`): every notice, approval title/description/answer, step text, decision reason and evidence summary of the helper, with `{Model}`, `{Host}`, `{Role}`, `{Seconds}` … placeholders. It is added to existing databases by the normal startup seeding of missing runtime values.

## 4. Supporting changes

- `IProviderModelRuntimeService.ProbeHostOnceAsync` and `ProviderHostReachability` (`NotSupported`, `ModelAvailable`, `ModelMissing`, `HostUnreachable`): one check without waiting. Any HTTP answer counts as reachable; only a transport failure or timeout counts as unreachable.
- `ICouncilSpoolerService.GetMemberOutcomes(takeRuns, excludeRunId)` and `CouncilMemberOutcome`: compact per-member outcomes of retained runs (no prompts, thinking or answers), read once per run as evidence.
- `MultiModelCouncilStep.HostUnreachable`; the spooler copy of a step now also keeps `RecoveryTargetModelName`, `RepetitionWatchdogStopped` and `HostUnreachable`.
- `MultiModelCouncilService` receives `IGoalRecoveryHelperService` (scoped registration next to the Council service).

## 5. Repository layout: maintenance/

- All changelogs moved from the repository root to `maintenance/changelogs/`. They stay published; older changelogs can be sorted into sub-folders there.
- Validation records, manual-merge patches, guard logs and the older review reports are in `maintenance/validation/`, `patches/`, `guard-logs/` and `historical/`. `.gitignore` excludes everything in `maintenance/` except `changelogs/`; the release ZIP still contains all of it.
- The LocalGPT Core release history reads `CHANGELOG-v*.md` from `maintenance/changelogs/` including sub-folders (and still from the root for working copies that have not moved them). A file name found in both places is read once.
- `RELEASE.md` and `VALIDATION.md` point to the new locations. `maintenance/README.md` has the one-time git commands.

## Version

`6.9.7` → `6.9.8`.
