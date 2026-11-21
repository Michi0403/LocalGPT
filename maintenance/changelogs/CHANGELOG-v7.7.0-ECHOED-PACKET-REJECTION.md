# LocalGPT 7.7.0 — Reject echoed work-order packets as source

## Why
In run a142abf7 a model saved its received work-order packet JSON (`{"draftOperation": …}`) as the content of
`wwwroot/TwitchDashboard/Index.html`. The packet was accepted as file bytes, hashed and reviewed as source.

## Changed
- `runtime.codegen.echoed-work-order-packet` (runtime policy regex, DB-owned): matches source that starts with a JSON
  object or array carrying a work-order protocol key (`draftOperation`, `draftManifest`, `draftWorkspaceMode`,
  `draftRequirements`, `functionName`).
- `CouncilEchoedWorkOrderPacketRejected` (runtime policy text): diagnostic returned to the author.
- `CodeGenerationWorkflowService.CompleteDraftFileAsync` (DraftAssignments): an echoed packet is rejected before size
  checks and before any hash or assignment write, so the file stays unassigned and the author gets the diagnostic.
- Version: `LocalGPT.csproj` 7.6.9 → 7.7.0 (next rollover slot after 7.6.9 per TASKS.md).

## Not in this version
- Other TASKS items (#59 layout check, #63 build routes, etc.) remain open.
- Not compiled in the editing environment; the owner compiles in Visual Studio.

## Verify
1. Compile in Visual Studio.
2. Send a file completion whose body is a work-order JSON; expect the diagnostic and no committed hash.
3. Normal fenced source completions still commit unchanged.
