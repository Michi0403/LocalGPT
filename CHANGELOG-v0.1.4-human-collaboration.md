# LocalGPT v0.1.4 — human collaboration and deferred approvals

## Added

- Persistent Human Collaboration Inbox mounted in `MainLayout`.
- Exact, fingerprinted approve/decline workflow with reasons and audit metadata.
- Non-blocking Feedback and Guidance forms with suggested responses, free text, and bounded prefill.
- Human Council Participant profile with display name, role, expertise, and working style.
- Live human contributions from Chat or the inbox without cancelling an active council run.
- Later model peer assessment of human contributions using Supported, Needs correction, Mixed, or Not reviewed verdicts.
- Immutable ambient execution snapshots with separate human-interaction and human-approval capabilities.
- Deferred sensitive DXAI invocations that store exact bounded parameters locally and execute only after a one-use approval on an exact retry or later council heartbeat.
- Persistent EF Core entities and migrations for requests, participant profile, contributions, and deferred invocations.
- Main-frame decision history and contribution-review history.
- `Assert-HumanCollaboration.ps1` architecture guard.

## Enhanced

- Sensitive diagnostic, Minecraft, and code-generation controller methods now use the persistent approval filter while retaining their existing confirmation and review-hash safeguards.
- DXAI automatic-tool discovery now distinguishes read-only, coordination-only, and deferred-approval-capable functions.
- Decline reasons become one-use council guidance, never permission.
- Approved deferred function results enter council context as bounded untrusted data, never instructions.
- Component logger, notifier, bounded activity memory, and recoverable error-boundary behavior remain intact.

## Security invariants

- Human participation and human approval are separate capabilities.
- Models, prompts, memory, HTTP flags, persisted rows, and function payloads cannot mint trusted human identity.
- Approval is tied to an operation key, correlation ID, and normalized SHA-256 parameter fingerprint, then consumed once.
- Changed parameters require a new request.
- Exact parameters, prompts, files, generated code, secrets, and returned content are omitted from structured logs.
- Completed council runs are not silently restarted after a late approval; the exact caller must retry.

## Compatibility and feature preservation

- Existing routes, council phases, project/version support, model protocol handling, Minecraft loaders, change reviews, artifact safety gates, notifier behavior, and short-term component awareness remain present.
- Wrapper `CS0006`/`WMC1006` diagnostics remain downstream until the LocalGPT project produces its assembly.
