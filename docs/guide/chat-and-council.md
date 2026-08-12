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

```text
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

> [!NOTE]
> Council output is a structured proposal pipeline, not a magic quorum. The boring deterministic parts are what keep the fun parts usable.

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

## Live heartbeat messages during a Council run

A direct live user message has two jobs. One currently executing participant may claim the new message and restart immediately so the owner does not need to wait for the next workflow boundary. The same contribution remains in the Council heartbeat queue and becomes shared context for later participants and later rounds when the normal heartbeat is prepared.

The immediate restart claim is single-consumer per Council run. While ordered presentation has a foreground participant, that participant owns the immediate restart claim; hidden parallel participants keep running instead of all restarting. If the foreground model has already completed, the message simply remains queued for subsequent participants and the next heartbeat. Other active participants do not restart for the same direct message, which avoids multiplying one correction into many duplicate model restarts while still preserving the correction as general Council context.

The Chat configuration exposes the scheduler at two levels. **Load balancing** selects host-balanced versus hardware-road parallel scheduling, and **Parallel models per AI host** controls the per-host ceiling. Expanding **Hardware spooler and per-model CPU/GPU roads** exposes each selected model's hardware kind/device, token road, Ollama GPU-layer choice, lane concurrency, and per-model load override. Different AI hosts remain independent compute roads, so a remote PC can work at the same time as the local machine. **Model response timeout (seconds)** controls how long a not-yet-started/current provider attempt may wait before the existing bounded recovery policy applies; this is useful when one slow remote model would otherwise make an ordered Council transcript appear stalled even though other hosts are busy.
