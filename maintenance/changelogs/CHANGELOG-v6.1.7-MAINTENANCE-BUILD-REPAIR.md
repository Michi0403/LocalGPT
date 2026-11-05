# LocalGPT 6.1.7 — Maintained build-source repair

## Scope

Based on the user-supplied LocalGPT **6.1.6** source ZIP; all semantic planning, Council workflows, goal ledger and 1-Wire integrations remain intact. No GitHub access or .NET build was used, and no architecture enforcement was weakened.

## Fixes from Visual Studio diagnostics

- `Build-Release.ps1`: correct the PowerShell 5.1 invalid variable-colon interpolation in the published-runtime assembly verification diagnostic (`${Rid}:`), preserving the existing version/payload check.
- `Services/GoalExecutionLedgerService.cs`: replace a forbidden application-level mutable static JSON options field with an instance-owned `readonly JsonSerializerOptions`, preserving journal writes, restarts and the original serializer configuration.
- `Services/GoalExecutionService.SemanticPlanning.cs`: document the existing optional `recoveryContext` parameter in its XML method contract to resolve `CS1573` without modifying planner behavior.
- `build/javascript-diagnostics-files.sha256`: refresh SHA256 entries over the actual maintained JavaScript assets, including the component-owned game console JavaScript module, using the original manifest's newline normalization. The strict diagnostics rule remains enabled.
- Version **6.1.6 → 6.1.7** (single-digit minor and patch as required).

## Old checkout note

The reported `src/LocalGPT/wwwroot/js/localgpt-game-console.js` does **not** occur in the supplied 6.1.6 source ZIP, and is not referenced by the current Game Console. The maintained component is `src/LocalGPT/Components/Shared/ChatGameConsole.razor.js`. If an existing working directory contains that old, untracked JS file after overlay extraction, it must be reviewed/moved out before running the strict JavaScript manifest validator. The ZIP deliberately does not whitelist unknown additional JavaScript or change the validator to ignore them. Prefer extraction into an empty source folder.

## Validation

Repository source/manifest consistency and version constraints checked; PowerShell parser, PowerShell architecture gates, full .NET compilation, model execution and Windows release packaging were **not** run. This is a source-validated archive, not a compiler-verified one.
