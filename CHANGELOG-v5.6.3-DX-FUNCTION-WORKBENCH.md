# LocalGPT 5.6.3 – DXAiFunction integration and function workbench

## Scope

A complete, incremental refactor of function discovery, input validation, approval-bound preview and invocation, persistent diagnostics, DevExpress management UI, and native Ollama/Council tool discovery. The existing `IDxAiFunctionRegistry` remains the **sole execution dispatcher**. Existing business objects, SQLite persistence, user-owned tools, runtime plugins, Human Collaboration inbox, X/1-Wire and game systems remain in place. No additional independent tool runtime or speculative feature stub has been introduced.

## 1 – Two-stage, saved-policy-based function discovery

- `DxAiFunctionDiscoveryRequest` and `DxAiFunctionDiscoveryResult` are shared business objects with paged, scored search and optional full parameter schema. Results originate from the existing catalog and registry, not a static model-side table.
- Discovery respects `IsAvailable`, `IsEnabled`, `ExposeToAiChat`, `AvailableToAi` and `SupportsDirectInvocation` and includes real descriptor hashes and approval metadata. The default stage omits detailed schemas; the second stage requests them explicitly.
- The service interface, HTTP catalog controller, DevExpress workbench and `dxai.tools.discover` DI-backed function all call the same catalog implementation.

## 2 – Schema contract and structured errors

- `DxAiFunctionInvocationResult.ValidationIssues` exposes a machine-readable path, code and diagnostic for malformed arguments without leaking user input into audit logs.
- JSON object parsing and the existing registry's registered schema validation precede preview, approval and execution. The business-object request retains all existing metadata and includes `PreviewOnly` and `ExpectedPreviewFingerprint` for opt-in exact-call contracts.
- AI models can repair an invalid request by re-discovering the schema and issuing a corrected preview. This is not an unbounded hidden automatic re-execution loop.

## 3 – Safe recovery and no blind write retries

- The existing `IDxAiFunctionCallRecoveryService` and approval/deferred services remain authoritative for recovery and continuation.
- Every execution reached through the new workbench/Council broker must carry a fingerprint from a successful non-executing preview. Any modified parameters, live descriptor or persisted policy yield `StalePreview` before approval or handler invocation.
- Function auditing intentionally runs after the result is obtained, does not re-run a handler if recording fails, and is isolated from client cancellation occurring after a side effect.

## 4 – Preview before human approval

- Registry `InvokeCoreAsync` handles `PreviewOnly` **before** `AuthorizeOrEnqueueAsync`, the deferred queue or any `IDxAiFunctionHandler` execution. The fingerprint hashes the function identity, exact parameter payload and invocation context together with live descriptor and saved policy.
- Existing Human Collaboration approval is never fabricated by the model or UI (`UserConfirmed=false`). Persisted confirmation policy may add a requirement, but cannot waive mandatory write-operation approval.
- API: `POST /api/dxai/functions/{functionName}/preview`, original invoke API and new `StalePreview` HTTP 409 handling.

## 5 – DevExpress function workbench and durable metadata history

- The existing `/dx-functions` page gains the **Function workbench** stage with DevExpress function ComboBox, JSON parameter memo, explicit preview and execution requests, displayed policy-bound preview and a filterable invocation history grid.
- The workbench calls the same `IDxAiFunctionServiceClient` and registry as chat, Council and HTTP. It does not implement tools itself.
- An `IDxAiFunctionInvocationAuditService` uses existing `SystemVariables` persistence (bounded to the latest 250 entries, no destructive migration) and stores only operation ID, function name, status, success flag and timestamp – **no prompts, parameters or results**. API: `GET /api/dxai/functions/history`.

## 6 – Council and small native Ollama models

- Three actual DI-registered `IDxAiFunctionHandler`s: `dxai.tools.discover`, `dxai.tools.preview` and `dxai.tools.invoke`. The latter refuses broker recursion and forwards the exact fingerprint and parameters to the ordinary registry with `UserConfirmed=false`.
- With no explicit automatic-function allow-list, the Ollama native tool table now advertises just these three broker tools, dramatically reducing up-front schema volume. Explicit user/team allow-lists retain their exact function identities and prior behavior.
- Permissions are not granted by discovery; target policy and existing approval always run again at execution.

## Non-goals and compatibility

- No renaming/removal of existing function handlers, user functions, plugin entries, Council workflows or public API routes. Existing callers of the invocation registry need not use the optional preview fingerprint; the **new** workbench/broker paths always do.
- No PublisherStudio/Organic/1-Wire wire protocol changes; PublisherStudio 4.1.8 is not modified.
- No physical device manipulation is added without the existing Human Collaboration gate.

## Static tests and restrictions

- `build/audit_dx_function_workbench_5_6_3.py` is wired into the normal Windows build through `build/Assert-DxFunctionWorkbench.ps1` and `Directory.Build.targets`. The existing rules remain intact.
- 19 source-only validation gates passed on the final implementation: DX workbench, Razor, DevExpress, async continuation/architecture, resilience, general architecture, cross-platform, portable PowerShell, game studio/revisions, data management/workspace/text, learning, ASCII, Windows model-free installer, Human Collaboration and code generation.
- Actual .NET compilation, Windows publish, running Ollama/Godot and end-to-end browser tests are not performed under the user's existing no-dotnet agreement. The acceptance checklist is in `VALIDATION-v5.6.3-source.md`.
