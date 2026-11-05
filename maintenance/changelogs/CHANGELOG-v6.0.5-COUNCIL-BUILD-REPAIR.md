# LocalGPT 6.0.5 — Council source-build diagnostics repair

Base: the complete LocalGPT 6.0.4 source archive. PublisherStudio was not included or modified.

## Maintained behavior

- Repaired `build/Assert-TextServiceOwnership.ps1` by defining the reviewed exception set before checking it. The set is deliberately empty, **not** a suppressive baseline; unapproved direct UI/controller string or regex operations still fail.
- Moved Council team-editor function-name tokenization into the existing injected `CouncilTextService.ParseUserEditableNameList` method instead of exempting the component from the ownership rule.
- Repaired XML `<param>` comments for `SetCompletionFunctionsText(value)`, `SelectTeam(team)`, `AddCouncilStepAsync(strictFunctionNames)`, and `OrganicCouncilBlueprintSeedDataService.Step`'s three new flags. Removed the misplaced tag from the unrelated review helper.
- No Council permission, strict-step semantics, model runtime, PDF helper, or shared protocol behavior changed.

## Build

Visual Studio Debug and `pwsh ./Build-Release.ps1` keep their original single-entrypoint prerequisites and policy gates. No extra pre-build command is needed.

## Scope and validation

See `VALIDATION-v6.0.5-source.md`. Static source review only; no local PowerShell/.NET execution occurred. The runtime build and macOS release remain to be confirmed on the target machines.
