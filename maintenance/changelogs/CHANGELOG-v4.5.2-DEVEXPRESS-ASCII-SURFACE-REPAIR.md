# LocalGPT 4.5.2 — DevExpress and ASCII surface repair

## ASCII terminal lifecycle and fullscreen

- Reopening the shared ASCII terminal now creates a fresh DevExpress popup subtree instead of trying to reuse the closed popup state.
- Fullscreen requests target the popup-aware fullscreen host, keeping DevExpress overlay/dropdown content inside the active fullscreen tree.
- Closing the terminal exits fullscreen first and leaves the Chat page able to open the terminal again.
- The game-stage layout no longer reserves the former mirrored left-side gutter; the screen takes the available width and the optional control guide owns only its actual side column.
- Follow-tail handling now actively scrolls chat/operator output after live mutations and can be explicitly refreshed after renderer updates.

## Game visibility and tournament preparation

- The Chat terminal action exposes a `GAME READY` success state when the selected Council workflow requires an ASCII surface or the mounted terminal reports an owned running game.
- Game availability is reported from the shared terminal back to Chat without cross-attaching unrelated sessions.
- Kernel Creature Tournament preparation now shows a live status overlay while the engine is waiting for Council lineup/bracket activity instead of presenting an apparently idle black surface.

## DevExpress control completion

- Finished the interrupted DevExpress-first conversion in Chat, Council Teams, Projects and Project Maintenance.
- Remaining ordinary native `<select>`, `<option>`, `<input>`, `<textarea>` and `<datalist>` controls were replaced with DevExpress Blazor editors while preserving persisted values and existing change-handler side effects.
- Provider/model, Council, project/revision, workspace/compiler, game-authoring and workflow policy selectors now use typed DevExpress data sources where display labels differ from persisted values.
- Added a maintained source audit plus an MSBuild guard that rejects ordinary native Razor form controls. Only the two reconnect/reload controls in `App.razor` remain native because they must work while the InteractiveServer circuit is disconnected.

## Render-mode and architecture preservation

- The 20 maintained `@rendermode` declarations are unchanged from the packaged 4.5.1 baseline: 15 direct `InteractiveServer` declarations and five `InteractiveServerRenderMode(prerender: false)` component islands.
- No Council seed-data change, database migration or wire-protocol change is introduced. Council team seed version remains 33 and runtime-class seed version remains 7.
- Renderer-affine continuation policy was extended only for the new game-availability callback, and the direct Razor event lambda was kept free of an embedded `ConfigureAwait(true)` continuation.

## Release identity and validation boundary

- Advanced LocalGPT from 4.5.1 to 4.5.2; version-slot policy remains single digit in the second and third slots.
- Updated application/installer/webview-wrapper versions, browser cache keys, user-agent strings and current documentation release identity.
- Source/static validation only in this environment: no .NET restore/build/publish and no GitHub/online repository access.
- The release ZIP is produced only after static audits, JavaScript syntax and diagnostics-manifest checks, JSON/XML parsing, path-safety/CRC verification, clean extraction and byte-for-byte source comparison.
