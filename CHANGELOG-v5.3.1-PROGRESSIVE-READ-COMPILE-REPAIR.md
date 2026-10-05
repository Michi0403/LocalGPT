# LocalGPT 5.3.1 — progressive read compile repair

## Summary

LocalGPT 5.3.1 is a focused source maintenance follow-up to 5.3.0. It repairs the compiler failure reported after the progressive upload-workspace read contract gained a character-offset parameter. The 5.3.0 source-evidence, safe extraction, whole-file continuation and non-game Council DXFunction behavior is preserved unchanged.

## Compiler repair

- `LocalGptDiagnosticController.Workspaces.GetChatUploadWorkspaceWorkspaceNameFile` now calls `IChatUploadWorkspaceService.ReadFileAsync` with the required `characterOffset` argument before the cancellation token.
- The diagnostic endpoint keeps its previous behavior by starting at character offset `0`; the progressive DXFunction callers continue to supply their requested offsets.
- No interface/service contract, upload evidence semantics, quarantine behavior, Council function policy or game allow-list behavior is changed by this follow-up.

## Maintenance integrity

- No file under `build/` is changed.
- No GitHub or online repository access is used.
- No .NET restore/build/publish is performed while preparing this source package.
