# Chat and AI Council

## Direct chat

The Chat page is the primary conversation surface. One provider-qualified model answers ordinary requests while configuration remains collapsible so the transcript keeps the available viewport.

A chat session owns its selected provider route, protocol profile, formatter, bounded context, and feedback state. Protocol-specific control tokens are normalized by the matching profile instead of being scattered through the UI.

Supported protocol families include Harmony-style channels, DeepSeek/R1 thinking markers, Gemma turn markers, Apple/OpenELM-style roles, and generic think-tag formats. The formatter boundary keeps these families isolated.

## Council teams

A Council team turns one request into an explicit workflow. Each member has a role, provider-qualified model route, optional runtime class, and bounded task. The team configuration is durable; the active run state is not replaced by whichever model speaks last.

Typical roles include:

- coordinator or leader;
- implementation specialist;
- architecture or safety reviewer;
- verifier;
- domain specialist;
- world actor for GameDirector sessions;
- human participant when invited.

The Council spooler and preflight services prepare work, validate routes, and track steps. A model can propose a result, but deterministic application rules still decide whether a step is complete, failed, recoverable, or waiting for a human.

### Choosing an execution mode

For most multi-PC Councils, **one member per AI host with all hosts working in parallel** is the conservative default. In the team editor this is `AllMembersSequentialOnEachAIHostParallel`. It avoids loading several large models on one GPU at once while still using every connected AI PC.

Choose `AllMembersParallel` when each host has enough VRAM/CPU capacity for multiple simultaneous model requests and set the lane limit accordingly. Choose `AllMembersSequential` only when the workflow really needs one global speaking order across every host.

All modes keep the workflow phase barrier: the next round starts only after every member assigned to the current phase has completed, failed, or been explicitly skipped.

## Provider-qualified routing

The visible address follows this shape:

```
Provider — model @ endpoint
```

A bare model name is accepted only when it resolves to one provider route. Ambiguous or stale selections fail clearly instead of falling back to an arbitrary endpoint.

Mixed-provider Councils can combine multiple Ollama hosts, LM Studio or another OpenAI-compatible endpoint, OpenAI, and Azure OpenAI. The correct client is created immediately before each call. Provider-specific settings—such as Ollama GPU options—stay with the provider that understands them.

A provider host is identified by provider kind and normalized endpoint. Adding a second Ollama does not replace the first: the existing primary remains the default and the new endpoint becomes an additional host unless the user explicitly promotes it. On refresh/run preflight, stale provider-qualified selections are deselected or rejected with their exact endpoint; LocalGPT never substitutes a same-name model from another host.

## Benchmark Council

A bounded benchmark measures one or more selected routes using deterministic tasks and explicit limits for context, output, count, and timeout. Other selected models can review the recommendation; when no independent reviewer is available, a bounded self-review may be used.

Every benchmark run is registered as a detachable live Council session. Its current target, profile, task, timeout, review, and recommendation progress can be opened from the benchmark panel or rejoined from **Chat → Running session tools**. Stopping that live session cancels the owned benchmark run. Automatic DXFunctions are disabled during benchmark calls so tool negotiation does not distort the measured route.

Recommendations are never applied automatically. The user chooses whether to save or apply a preset. Applying a preset changes future LocalGPT routing; it does not rewrite provider-global server configuration.

## Result integrity

A step that produces thinking but no substantive final answer receives at most one bounded final-only recovery request. If the result is still empty, the step is failed rather than counted as success.

Peer verification is advisory. A failed verifier can be recorded as a warning while a valid result remains available, but the UI must not relabel unrelated notices as successful verification.

## Human participation

A human can join a running Council, answer a question, approve a proposed boundary, or provide a correction. Human participation does not turn every human message into unrestricted authority; the application still binds decisions to the current request, action, target, and approval scope.

## DX functions and change reviews

DX functions expose bounded application capabilities through typed parameters and explicit handlers. Generated changes can be stored as review records with proposed files, findings, execution status, and human decisions. The function catalog describes what can be requested; it does not bypass the service that owns the real operation.

##### Note

Council output is a structured proposal pipeline, not a magic quorum. The boring deterministic parts are what keep the fun parts usable.

## X-Rounds: revisable Council control flow

X-Rounds make cross-step revision a first-class Council workflow feature. They extend the existing ability to keep one round alive until its completion condition is met: an enabled workflow step may finish normally and later request a controlled return to another configured step when new evidence invalidates an earlier assumption.

The original transcript is never rewound or overwritten. A revisit appends another revision of the target step. For example, `R2.v1 → R3.v1 → R5.v1 → R2.v2` preserves both versions and records the X-Round reason that caused the second visit.

Council Teams owns the policy. Each workflow step can independently grant the following X-functions:

- `council.x.status` — inspect the X policy currently granted to the executing step;
- `council.x.revisit` — request either `reconsider` or `reexecute` of another configured step;
- `council.x.return_text` — return explicit text to the parent workflow and complete it cleanly;
- `council.x.start_single_model` — run one selected member as a bounded derived reasoning task and feed its visible result back to the parent;
- `council.x.start_council` — run another configured Council team with its own run identity and feed its final text back to the parent.

`reconsider` is deliberately side-effect-free: the revisited step may reason again but LocalGPT suppresses DX/organic function execution for that revision. `reexecute` is the explicit alternative when the workflow really needs the target step's normal function policy again; its ordinary tool approvals remain in force. A revisit may target the current or an earlier configured step only. X-Rounds do not jump forward across workflow gates; forward progress happens through the normal configured workflow after the revisited revision completes.

Every source step has an explicit transition budget. Child-Council nesting has a separate depth budget. A team can also require a local human to approve every accepted X transition. Declining the gate continues the ordinary workflow instead of silently selecting another route. These boundaries prevent a useful feedback graph from turning into an unbounded retry loop or bypassing a human gate.

A gatekeeper is therefore not a special hard-coded Council type. It can be an ordinary role/step whose responsibility, prompt and X permissions allow it to send work back, request another specialist/Council, return a result, or require the configured human decision.

### Why a step runs again (7.0.1)

When an X-Round sends work back, the revisited step now receives the reason in its prompt, under **Why this step runs again — resolve this before anything else**. Before 7.0.1 the reason was stored only as run metadata, so a revisited model usually repeated its first answer.

The X-functions a step is granted are also offered to the model as native tools, even when the step uses a strict function allow-list. A gatekeeper therefore no longer needs a team-level exception to send work back.

The seeded **program compiler** team uses this. Its curate, toolchain and verification steps may call `council.x.revisit` with mode `reexecute` against `compiler-generate`, at most twice per step. They must name the exact missing files or the compiler error lines when they do.

## Required completion receipts (7.0.1)

A team's `RequiredCompletedFunctions` used to produce only a warning when a required function never completed. LocalGPT now treats a missing receipt as unfinished work:

1. **Recovery.** After the workflow, every required function without an explicit recovery rule gets an implicit one. LocalGPT picks the last step that is allowed to call that function and runs it again with a prompt that names the missing receipt.
   - The number of attempts is the runtime value `ImplicitCompletionRecoveryAttempts` (default 2), capped by `MaximumCompletionRecoveryAttempts`.
   - The prompt is the runtime value `ImplicitCompletionRecoveryPromptTemplate`; `{{FunctionName}}` is replaced.
   - A plan, a description or a claim of success does not count. Only a completed function result does.
2. **Generation gate.** If a required receipt is still missing after recovery, implementation artifacts are not generated, and the run says which receipt is missing.
3. **No placeholder output.** When the Council produced no structured code-generation plan, no change review is created. LocalGPT no longer substitutes placeholder example files.

The seeded program compiler team requires `codegen.review.create`.

## Code generation keeps the parent revision (7.0.1)

Generated code is saved as a new child revision of the selected project revision; the parent revision is left unchanged. The child becomes the current revision, and its name ends with `generated` plus a short id. When a build target fails, the compiler error lines go back into the result as `BuildDiagnostics`. The verification step can hand those lines to the generator through `council.x.revisit`. The runtime values `BuildDiagnosticMaximumLines` (default 40) and `BuildDiagnosticMaximumCharacters` (default 6000) bound them.

Files you promote from an ingested project are now marked as user-approved source when the workspace syncs. Generated files are never marked approved automatically, and an existing approval is never removed.

## Live heartbeat messages during a Council run

A direct live user message has two jobs. One currently executing participant may claim the new message and restart immediately so the owner does not need to wait for the next workflow boundary. The same contribution remains in the Council heartbeat queue and becomes shared context for later participants and later rounds when the normal heartbeat is prepared.

The immediate restart claim is single-consumer per Council run. While ordered presentation has a foreground participant, that participant owns the immediate restart claim; hidden parallel participants keep running instead of all restarting. If the foreground model has already completed, the message simply remains queued for subsequent participants and the next heartbeat. Other active participants do not restart for the same direct message, which avoids multiplying one correction into many duplicate model restarts while still preserving the correction as general Council context.

The Chat configuration exposes the scheduler at two levels. **Load balancing** selects host-balanced versus hardware-road parallel scheduling, and **Parallel models per AI host** controls the per-host ceiling. Expanding **Hardware spooler and per-model CPU/GPU roads** exposes each selected model's hardware kind/device, token road, Ollama GPU-layer choice, lane concurrency, and per-model load override. Different AI hosts remain independent compute roads, so a remote PC can work at the same time as the local machine. **Model response timeout (seconds)** controls how long a not-yet-started/current provider attempt may wait before the existing bounded recovery policy applies; this is useful when one slow remote model would otherwise make an ordered Council transcript appear stalled even though other hosts are busy.

## Prompt size, thinking and native tools (7.0.4)

LocalGPT now sizes each native Ollama request before sending it:

- **Context fits the prompt.** LocalGPT counts the characters of the messages and the attached tool schemas. If they would not fit the configured context together with the answer, the context of that one request is raised, never above `OllamaRequestBudgetPolicyJson.maximumFittedContextTokens` (default 32768). Previously Ollama silently cut the oldest prompt content: system prompt, task and tool list.
- **Thinking has its own budget.** When LocalGPT asks a model to think, `thinkingReserveTokens` (default 2048) is added to the output limit. Thinking no longer uses up the visible answer, which caused truncated stories and empty answers.
- **Compact tool set.** A Council step without a saved tool preference no longer attaches all registered tools (200+). It attaches:
  - the base set `CouncilBaseNativeFunctions`;
  - the functions that serve the capabilities requested for the run, mapped by `CouncilCapabilityNativeFunctionsJson` (for example `publisher.text.insert.propose` → `publisher.text.proposal.request`);
  - attachments, AI help and the tool broker (`dxai.tools.discover/preview/invoke`), which still reaches every policy-approved tool on demand.

  Set `CouncilUnlistedStepNativeToolMode` to `AllPolicyApproved` for the previous behavior.
- **Final-answer recovery keeps the task.** When a participant produced only thinking, the recovery turn now restates the original request (head and tail, `CouncilFinalRecoveryTaskExcerptCharacters`, default 6000) and the partial answer. The model continues the task instead of answering a generic question.

## Goal delivery for 1-Wire requests (7.0.4)

Some goals require only *approval-gated* capabilities: capabilities whose prefix is listed in `GoalApprovalGatedCapabilityPrefixes` (default `publisher.`). A PublisherStudio AI Assist request is one. For such goals:

- **The plan runs to the end.** An intermediate step with an unverifiable external effect no longer stops the goal. Each planned step still runs once and nothing is replayed. The goal ends as *NeedsVerification* until the effect is confirmed.
- **Delivery is the evidence.** The Keyaccount Manager checks the approval-gated function requests the Council recorded, for example a `publisher.text.proposal.request` waiting in Approvals & team. A local file artifact is not expected.
  - If no delivery request exists, the next objective becomes a delivery step (`GoalDeliveryObjectiveTemplate`) within the continuation limit, instead of an unspecific "needs human review" stop.
  - If a delivery request exists, it is waiting for your approval, so no second human stop is added.
- **Clean deliverable.** The Council result now carries `DeliverableText`: the output of the last content step, without intermediate answers or the Keyaccount report. PublisherStudio 4.7.5 inserts it when no proposal call arrived.

**Team choice.** When the goal planner gives no semantic plan, the requested team is kept. Another team is chosen only when required capabilities or a strong keyword match ask for it (`GoalCatalogFallbackMinimumTeamScore`, default 6). Teams whose distinct-model roles need more models than the run has are never chosen automatically. An explicitly selected one stops with `SelectedCouncilTeamNeedsMoreDistinctModels` instead of failing inside role assignment.

## Configuration Steward (7.0.4)

The Configuration Steward helps find and fix configuration and data problems without anyone editing the database by hand. The steward never applies a change itself.

- **`localgpt.configuration.diagnose`** (read-only) runs deterministic checks:
  - default Council power below the threshold (for example the old 30% default);
  - runtime values that cannot be read as their declared type, including broken JSON documents and collections;
  - teams that cannot be staffed with the installed models;
  - steps that attach all native tools.

  Each finding lists the observed values and their source, and what the data cannot prove. For example, a low value may be intentional.
- **`localgpt.configuration.repair.request`** asks you to decide a repair.
  - It recomputes the finding from live data; a model cannot supply values or options.
  - It places the finding's options as selectable answers in **Approvals & team**. The recommended option is preselected; "keep" is always offered.
  - When you approve an option, it is applied once, in one transaction, and only if every stored value still equals the value you saw. If the data changed meanwhile, nothing is changed and you are asked again.
- **Configuration Steward team.** The seeded team runs three steps with any number of models:
  1. investigate with the diagnosis and read-only table previews;
  2. challenge the evidence, treating stored data as evidence and not as truth;
  3. request repairs only for findings that survived the review.
- **Thresholds** are in `ConfigurationStewardPolicyJson`.
