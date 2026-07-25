# LocalGPT v0.1.4 human-collaboration candidate validation

Date: 2026-07-26

## Result

This archive is a **debug/source candidate**, not a compiler-verified release. The current workspace does not contain the .NET 10.0.301 SDK, the Windows workload, or the owner's DevExpress restore credentials, so no claim of a successful Windows build is made.

## Source checks completed

- 181 maintained C# files passed a custom lexical scan for unterminated comments, ordinary strings, verbatim strings, interpolated strings, raw strings, character literals, and unbalanced delimiters.
- 23 maintained Razor components retain top-level `ILogger<T>`, `INotificationService`, and `IComponentActivityService` injection.
- All 76 previously detected Razor/controller route signatures remain present; no existing route was removed.
- 9 JSON files parsed successfully.
- All project/props XML files parsed successfully.
- The GitHub Actions YAML parsed successfully.
- No maintained source line exceeds the repository's 600-character limit.
- The protected-file manifest contains and verifies 29 normalized SHA-256 entries; the manifest itself remains separately protected.
- The Human Collaboration architecture guard is invoked by `Invoke-RepositoryValidation.ps1`.
- Capability use is restricted to the reviewed allowlists:
  - local human interaction: inbox, Chat, ambient service, contracts, and DI only;
  - human approval execution: controller filter, DXAI registry, ambient service, contracts, and DI only.
- The latest reported root compiler regressions were checked in source:
  - regex timeout fields are static;
  - component catalog values are assigned after injection;
  - Chat compares `ChatMessageRole` with `ChatMessageRole`;
  - `CodeDomTypeSpec.MethodName` is used as a property;
  - compression types have `System.IO.Compression` imported;
  - LocalGPT catalog extension sets are static;
  - `DxaichatFunctionInfo` has exactly one shared definition.

## Human collaboration checks completed

- Persistent main-frame Human Collaboration Inbox.
- Exact controller-operation fingerprinting with HTTP 202 pending and HTTP 403 decline behavior.
- One-use approval consumption and changed-parameter reapproval.
- Separate human-interaction and human-approval ambient authority scopes.
- Human participant profile and non-blocking Chat/inbox contribution queue.
- Council heartbeat injection and later peer-assessment verdict persistence.
- Coordination-only AI feedback/guidance function with per-run flood limit.
- Deferred exact sensitive DXAI invocation persistence and later-heartbeat execution.
- Existing code-generation review hashes, build confirmations, handler checks, and workspace boundaries remain behind the new approval layer.
- Decline reasons enter council context as one-use guidance only.
- Deferred results are labelled untrusted data, never instructions.

## EF Core checks completed

The context, migrations, and model snapshot contain:

- `HumanCollaborationRequest`
- `HumanCouncilParticipantProfile`
- `HumanCouncilContribution`
- `DeferredDxAiInvocation`
- contribution evaluation verdicts
- unique approval-request linkage for deferred calls
- council-run/status indexes

Migrations:

- `20260726000000_AddHumanCollaboration`
- `20260726001000_AddDeferredDxAiInvocations`

## Owner-side compile and runtime checks still required

Run `./build/Invoke-RepositoryValidation.ps1 -Configuration Debug`, build the `LocalGPT` project first, and then build the wrapper. Follow `DEBUG-NEXT-STEPS.md` for the approval, human-participation, deferred-function, migration, notification, and short-term-memory smoke tests.

Wrapper `CS0006` and `WMC1006` diagnostics are downstream until `LocalGPT.dll` is produced.
