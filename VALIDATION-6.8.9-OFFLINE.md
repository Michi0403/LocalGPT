# LocalGPT 6.8.9 — Offline validation record

Baseline: LocalGPT 6.8.8 full source ZIP provided in this conversation.

- 33 source-level contract and negative-control checks passed using an external offline validator.
- XML project version parsed and checked: `6.8.9`.
- Changed only five existing source files plus this validation and changelog.
- `CouncilGameSessionService.cs`, `MultiModelCouncilService.ParticipantExecution.cs`, and `ProviderModelRuntimeService.cs` byte-identical to 6.8.8.
- Original installer / SQLite recovery files retained unchanged from 6.8.8.
- ZIP content CRC and per-file source parity checked during packaging.

### Test cases needing real Windows verification

1. Start a Council, disconnect the circuit, reload and rejoin the *same* run and persisted Chat; preserve user and assistant messages with no duplicate Council run.
2. Select a completed run with matching saved SQLite Chat: restore all saved messages and exact project context, then show full Council evidence.
3. Select a completed run with no matching SQLite Chat: show prompt/step previews and final answer in DxAIChat; expose all original Council evidence without changing the database.
4. Reopen the same completed run after another browser reload, preserving the run identifier.
5. Select a different run while a previous recovery is loading: never swap the new selection with the earlier run's messages.
6. Disconnect during composer draft capture: avoid killing the Council backend or creating a second run; the visible UI may require reconnect.
7. Confirm full chat/function-call visibility on Council streams separately from the saved-evidence fallback; raw spool text cannot reconstitute DevExpress FunctionCall objects.
8. Confirm `TryClaimPendingStream` timeout / Blazor WebSocket stability under long Council streams; these issues are outside the proven scope of this change.

### Tooling limitations

PowerShell was unavailable in this environment. No PowerShell audit, .NET build, full Windows installation, Blazor circuit or DevExpress interactive execution was run; passing static checks does not establish these runtime outcomes.
