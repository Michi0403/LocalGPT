# Chat and Council runtime

## Session boundary

A chat session combines a selected provider route, protocol profile, formatter, prompt configuration, function catalog, and bounded context. These dependencies are resolved through services and remain stable for the response stream.

Protocol profiles are stateless. They detect provider/model families and normalize only their own control markers. The response formatter owns presentation of thinking, final content, tools, code, and structured sections.

## Council orchestration

A Council request is decomposed into explicit workflow steps. The runtime owns:

- team and member configuration;
- provider-qualified route resolution;
- preflight and readiness checks;
- role/task prompts;
- step ordering and cancellation;
- human questions and decisions;
- final-result composition;
- durable summaries and artifacts where appropriate.

The spooler represents queued/running work. Runtime classes describe reusable behavior or game actors, but they do not bypass the workflow service.

### Workflow execution modes

Council workflow steps can choose how role members consume the available AI hardware:

- `AllMembersParallel` allows up to the configured model-lane limit on each participating AI host. Separate hosts remain independent compute boundaries.
- `AllMembersSequentialOnEachAIHostParallel` is the default for newly created workflow steps. It creates one deterministic queue per AI host, runs exactly one member at a time inside each queue, and lets the host queues advance concurrently. The phase barrier still waits for every assigned member before the next workflow step starts.
- `AllMembersSequential` is the strict global chain. Only one member runs at a time across every host and later members may observe the earlier completed step output.
- single-member modes (`LeaderSingle`, `RoundRobinSingle`, `AssignedModelSingle`) select one exact participant according to the saved workflow contract.

The per-host sequential mode intentionally gives every member the transcript that existed when the phase began. It does not merge race-dependent output from another host into an in-flight peer prompt. Use strict global sequential execution when each member must review the immediately preceding member.

`AllowParallelHardwareRoads` controls additional CPU/GPU-road concurrency inside an AI host; it no longer collapses different physical/provider hosts into one global runtime lane.

## Deterministic completion

Models are probabilistic; step completion is not. The application evaluates whether a response contains substantive final content, whether required outputs exist, and whether a bounded recovery is allowed.

A verifier can add confidence or a warning. It cannot retroactively make an invalid primary result valid. Conversely, a failed verifier should not erase a valid result when the workflow contract treats verification as advisory.

## Human collaboration

Human participation is modeled as a request and decision flow with an exact question, boundary, status, and optional reuse scope. It supports:

- answering a Council question;
- choosing among alternatives;
- reviewing a proposed change;
- approving a bounded operation;
- contributing domain knowledge.

A human decision is bound to its context. It is not a global bypass token.

## DX functions

DX functions expose typed application operations to chat and Council workflows. The catalog describes parameters and capabilities. The handler/service still owns validation, policy, and execution.

Functions should return structured outcomes that distinguish:

- success;
- user decision required;
- capability missing;
- validation failure;
- execution failure;
- partial/reviewable output.

## Change reviews

Generated changes are stored as reviewable records rather than immediately applied. A review can include proposed files, diffs or replacement content, architecture findings, build instructions, execution evidence, and user decisions.

The workflow can approve a revision for testing without marking it released or deployed.

## Knowledge

Council knowledge is persistent but curated. Automatic context uses reviewed, active, non-expired entries whose provenance is acceptable. Source-backed knowledge is refreshed when its source hash changes. Raw uploads and model answers do not become trusted knowledge automatically.

## GameDirector reuse

GameDirector uses the same Council machinery for role-assigned world actors while retaining a deterministic authoritative resolver. This is an example of the intended architecture: generative proposals inside a strict application-owned state machine.
