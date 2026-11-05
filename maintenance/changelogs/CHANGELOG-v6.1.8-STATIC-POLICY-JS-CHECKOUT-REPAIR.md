# LocalGPT 6.1.8 — goal-ledger architecture and JavaScript checkout compatibility

## Source baseline and scope

Based on LocalGPT 6.1.7. The goal planner, semantic replanning, Council workflow, durable checkpoint format, chat, and 1-Wire behavior remain intact. No GitHub or .NET build was used. Existing PowerShell architecture guards and `Directory.Build.targets` are unchanged.

## Corrections

- `Services/GoalExecutionLedgerService.cs`: make `ResolvePath(Guid)` an instance operation, preserving the exact goal-ID-to-user-storage mapping and write-ahead checkpoint semantics. The application's static-state architecture guard rejects the former declaration.
- `Services/GoalExecutionService.cs` and `GoalExecutionService.DurableExecution.cs`: move the remaining goal execution helper methods from static declarations to instance methods in the existing DI-owned partial service. Their algorithms and call sites are unchanged. This prevents the next static-policy finding after `ResolvePath` is corrected.
- `wwwroot/js/localgpt-game-console.js`: provide an explicit, guarded backward-compatible *module alias* to the collocated `Components/Shared/ChatGameConsole.razor.js`. This is for installations that retained the obsolete `wwwroot/js` path when a ZIP was extracted on top of earlier source. The current component still imports the collocated module directly; there is no copied rendering logic or change to game-console behavior.
- `build/javascript-diagnostics-files.sha256`: review and record the normalized SHA-256 of **all** current maintained browser JavaScript sources, including the compatibility alias. The strict rule still fails for unreviewed additional or changed JavaScript.
- Version: `6.1.7 -> 6.1.8` (single-digit minor/patch).

## Verification

- Source-level source/manifest SHA-256 parity for all 27 maintained JavaScript sources, including the collocated game-console module and the legacy compatibility URL.
- Inspected all `GoalExecution*.cs` sources for forbidden static declarations; none remain.
- `node --check` on the compatibility module succeeded.
- Checked requested application version, XML-method signatures and preservation of original architecture checks.
- **Not performed:** PowerShell 5.1 architecture-gate execution, .NET compilation, Windows browser/Blazor runtime verification. Those require the host application environment.

The compatibility alias is intentionally included in the ZIP so overlay extraction overwrites the previously unreviewed file instead of leaving the same build failure unresolved. Other untracked scripts from an arbitrary older working directory still require review; the guard is not bypassed.
