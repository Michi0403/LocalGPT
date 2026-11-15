# LocalGPT 7.2.5 - tagged calls and visible failures

- A `<localgpt-dx-call>{...}</localgpt-dx-call>` tag written after prose is now recovered when the streamed answer is complete, through the existing recovery service and function registry, so approval-gated functions reach the approval inbox like native tool calls. Already streamed text is not repeated. Only the explicit tag counts outside the start of the answer.
- A Council member step that returns no answer now carries a clear error ("returned no answer (possible model crash, out-of-memory or context overflow)") and the failure block appears before the recovery round. Timeout and exception failures also write a line to the live stream before the safe-limit retry.
- `localgpt.sqlite.tables.list` describes the EF Core storage (LocalGptMemoryDbContext), the HumanCollaborationRequests approval inbox and the DeferredDxAiInvocations table.
- Version 7.2.4 to 7.2.5 (project, docs).

## Not built here

No .NET SDK or PowerShell was available: nothing was compiled, no build guard ran, no test was executed.

## Check

- gemma4:26b council writes a tagged council call after prose -> the approval appears in the inbox.
- A crashed/empty member step shows a failure line before the recovery round.
