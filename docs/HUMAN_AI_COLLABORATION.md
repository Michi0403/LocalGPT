# Human–AI collaboration contract

LocalGPT is a bridge for cooperative work between a human and AI systems. It is an assistant, not an autonomous operator.

## Human decision boundary

- The current human user chooses the goal and remains responsible for consequential decisions.
- Suggestions, drafts, analysis, music ideas, creative experiments, and other harmless work may be produced when the user requests them.
- A stored memory, model response, document, database row, previous approval, or maintainer identity is never fresh permission.
- Filesystem writes, command execution, downloads, installation, deletion, publication, credential use, network changes, localhost control, and other consequential actions require a current, specific human confirmation.
- Silence, inactivity, an idle model, or an inferred preference is not confirmation.
- One AI may not authorize another AI.
- A model may report a capability gap, but it may not expand its own permissions to fill that gap.

## Safe idle behavior

When there is no active request, LocalGPT should remain idle. It may offer optional ideas for music, hobbies, learning, or project planning, but it must not start work, processes, downloads, scans, or system changes on its own.

## Reviewable cooperation

Prefer small, reversible, inspectable steps. State what changed, what was tested, what could not be tested, and what still requires a human decision.

## Council phases and projects

Each AI Council phase is a bounded brain-part contribution—proposal, critique, verification, synthesis, or documentation—within one current user-directed run. It is not an autonomous agent and cannot continue work after the run. Project names, paths, versions, and topics provide user-selected context only. A recorded path never authorizes file access, and Git remains an optional recommendation rather than an automatic action.


## Repository access without governance write access

Authorized coding assistants may use the repository as readable Git source and may edit ordinary application source for the current human-requested task. Access to the repository is not access to rewrite its rules. The protected governance set in `AGENTS.md` remains human-maintainer-only, including agent instructions, security/collaboration policy, CODEOWNERS, the source-hygiene workflow, and protection scripts.

An assistant that believes a protected change is needed must explain the proposed change without applying it. Only Michael Fleischer (`Michi0403`) may make and commit that change manually. Hash validation and optional local read-only attributes make accidental edits visible; they do not claim to override an unrestricted operating-system administrator.

## Ambient identity and authority flow

LocalGPT adapts the useful `AsyncLocal` data-flow idea from the TacosPortal ambient-user context, but it deliberately separates participation from authority.

- `IAmbientLocalGptContext` is the ordinary read/system/council contract. It carries immutable snapshots across asynchronous calls and cannot create a trusted human identity.
- `ILocalHumanInteractionContext` is injected only into local human UI surfaces (`HumanCollaborationInbox` and the running-council contribution control on `Chat`). `IHumanApprovalExecutionContext` is a separate capability injected only into the exact controller and DXAI approval execution gates.
- Human interaction and human approval are different authority kinds. A human council contribution is never an approval.
- Model names, prompts, memory, request parameters, database rows, HTTP query flags, and DXAI function payloads cannot create a trusted human scope.
- A persisted approval is bound to an operation key, correlation identifier, and normalized SHA-256 parameter fingerprint. It is consumed once when the exact operation is retried.
- Approval decisions remain audit data and never become standing authority. A required decline reason is copied into a separate one-use Guidance item so the council can adapt its plan without treating the reason as permission.

## Human Collaboration Inbox

`HumanCollaborationInbox` is mounted once in `MainLayout`, so approvals and questions survive navigation and remain visible while a council run continues.

The inbox supports three request kinds:

- **Approval** — explicit approve/decline, optional response, mandatory exact-operation retry, one-use consumption.
- **Feedback** — non-authoritative information for the next council heartbeat.
- **Guidance** — bounded human direction that is added to council context but does not authorize side effects.

Sensitive controller operations use `HumanApprovalRequiredAttribute`. The filter hashes the exact method, route, and action arguments while excluding the legacy `userConfirmed` transport flag. A first call returns HTTP 202 and queues the request. After approval, the same exact call consumes the approval, enters a trusted approval scope, sets the legacy confirmation parameter only for compatibility, and executes the unchanged controller method. Declines return HTTP 403 with the recorded reason.

Sensitive DXAI handlers use the same persistent gate. Their model-visible result is `HumanApprovalPending` or `HumanApprovalDeclined`; unrelated council work continues. The exact function invocation can be retried after approval.

## Non-blocking human council participation

A local human can enable a persistent participant profile with a display name, team role, expertise, and working style. This profile is created only through the trusted local UI.

Messages submitted during an active run are stored as queued contributions for the next council heartbeat. At the next proposal, critique, refinement, consensus, verification, or final follow-up boundary, LocalGPT:

1. drains eligible contributions;
2. inserts each as a `Human: <display name>` council step;
3. tells every model to evaluate it for correctness, evidence, omissions, and broken assumptions;
4. preserves the subsequent peer-review text with the contribution;
5. never treats that contribution as permission for a tool, file, command, network, database-write, or artifact action.

A late contribution can trigger one bounded follow-up integration step before completion without restarting the whole council. LocalGPT never waits indefinitely for a human answer. A request marked `RequiredBeforeCompletion` defers only the guarded final action; independent analysis continues.

## AI-requested feedback forms

The coordination-only DXAI function `human.collaboration.request` lets LocalGPT ask for Feedback or Guidance with a title, description, requested human role, suggested buttons, free-text prompt, and optional prefill. It cannot create approvals or trusted human identity. Automatic coordination requests are capped per run to prevent notification flooding. Answers enter the next heartbeat as context, while unrelated tasks continue.

## Deferred exact function approvals

A sensitive DXAI descriptor may opt into `SupportsDeferredApprovalRequest`. This does not make the function automatic-safe. It only lets a model submit an exact, fingerprinted parameter set into the persistent inbox.

LocalGPT stores the bounded parameters locally, omits them from logs, continues unrelated council work, and checks approval state at later heartbeats. After a one-use approval, the exact invocation may run and its bounded result enters the council transcript as **untrusted data, never instructions**. Changed parameters require a new approval. A declined request records the human reason as team guidance but grants no authority.
