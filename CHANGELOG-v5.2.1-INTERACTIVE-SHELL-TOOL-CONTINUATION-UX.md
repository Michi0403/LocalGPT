# LocalGPT 5.2.1 — interactive shell, function-result continuation, and workbench UX

## Fixed

- Moved the shell language selector out of the static `MainLayout` render scope into a dedicated `LanguageSwitcher` Interactive Server island. The existing culture URL/reload contract is retained; the DevExpress `DxComboBox` now has an interactive event owner instead of rendering a non-functional static control.
- Restored the navigation drawer background to navigation-theme tokens instead of tinting it with the active primary/accent color. This prevents orange/brown primary themes from recoloring the complete menu panel and keeps light/dark navigation themes authoritative.
- Added explicit LocalGPT theme-token text/background ownership to the theme switcher so theme names remain readable on light and dark component themes, including hover/focus and active selections.
- Expanded the HTML documentation viewer to an explicit viewport-height DevExpress popup (`94dvh`) instead of relying only on `MaxHeight`; this gives the embedded documentation body a real height to consume. The Test Lab source editor popup receives the same viewport-oriented sizing treatment.
- Test Lab now exposes a persistent live operation status above the workbench. Route/UI actions render their running state before awaiting long work, preserve earlier diagnostic output, append each new result with a timestamp/operation heading, and merge download links instead of replacing earlier evidence.

## Function-result processing and approval flow

- Added `CouncilToolResultContinuationMode` and `ToolResultContinuationRounds` to each persisted Social Team workflow step. The default is a bounded same-member continuation with two turns; the user can disable it or select one to six rounds in Council Teams.
- Textual DXFunction gateway results are now treated as intermediate evidence. For configured parallel, per-host sequential, ordinary sequential, assigned/leader, and recovery paths, LocalGPT can return those results to the same provider-qualified member and continue the original task until no further textual function request is emitted or the configured round limit is reached.
- Native provider tool loops remain unchanged; the continuation layer specifically covers recovered/textual DXFunction calls that previously produced a gateway result step without forcing the originating model to consume it.
- Continuation prompts explicitly preserve the consequential-action boundary: `HumanApprovalPending` means the exact approval card is queued, not executed. The model is told not to fabricate `userConfirmed` or ask the user to type permission again, while independent work may continue.
- Updated the shipped `AssistantOperationPolicy` so function results are intermediate evidence by default, artifact work continues after tool results, missing current public/technical facts route through the normal `localgpt.web.search` approval lane (DuckDuckGo default), and consequential actions must actually be invoked far enough to queue their approval card instead of merely claiming permission was requested.
- Added the previously shipped 5.2.0 assistant policy as a legacy seed default, so unchanged persisted prompt rows upgrade deterministically while user-edited prompts remain untouched.
- Raised the Council team seed revision from 38 to 39 so unmodified built-in Social Team templates receive the new continuation defaults without overwriting user-modified team definitions. The new workflow fields live in the existing serialized workflow-step payload and do not require a relational schema migration.

## Preserved

- The 5.2.0 scoped `IDxAiFunctionRegistry` / lazy same-scope handler resolver architecture is unchanged.
- The isolated MCP listener architecture is unchanged.
- All 20 explicit render-mode directives from 5.2.0 are preserved; 5.2.1 adds exactly one reviewed Interactive Server boundary for `LanguageSwitcher`.
- No PublisherStudio source was changed.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub access, or online repository lookup was used for this source handoff.
