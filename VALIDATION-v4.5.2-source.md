# LocalGPT 4.5.2 source validation

## Scope

This release resumes directly from the supplied `LocalGPT-4.5.2-WIP-source.zip` checkpoint and treats packaged LocalGPT 4.5.1 as the regression baseline. The repair targets the reported 4.5.1 runtime/UI behavior: the ASCII terminal could not reliably reopen after closing, fullscreen left a large unused area and could separate DevExpress dropdown overlays from the fullscreen tree, live ASCII output did not consistently follow the tail, game/view availability was not visible from Chat, and Kernel Creature Tournament preparation could look idle while Council work was still running. The WIP also contained a newly wired DevExpress-control build guard that still failed against 142 native-control violations in four Razor pages; those violations are completed rather than shipped as a build blocker.

## Repairs verified statically

- Chat recreates the DevExpress popup subtree on terminal reopen via the maintained render key and retains the existing close/presentation-state flow.
- The game console requests fullscreen on a popup-aware host, updates its own fullscreen presentation class from `fullscreenchange`, and exits fullscreen against the same host.
- Game-stage CSS assigns the canvas the real first column instead of the previous mirrored blank gutter and keeps mobile/guide layouts bounded.
- The terminal exposes an explicit follow-tail refresh and live mutation handling for chat/operator output.
- Chat shows a `GAME READY` terminal action when the selected workflow or mounted terminal has an ASCII surface available.
- Kernel Creature Tournament turn-zero preparation exposes live Council activity status without changing authoritative frame text.
- The DevExpress Blazor control audit passes with native ordinary controls removed from Chat, Council Teams, Projects and Project Maintenance; only the two circuit-independent reconnect/reload controls in `App.razor` remain native.
- All 20 maintained render-mode declarations are identical to the 4.5.1 source baseline: 15 direct `@rendermode InteractiveServer` declarations and five `InteractiveServerRenderMode(prerender: false)` islands.
- Council team seed version remains 33 and runtime-class seed version remains 7; no new migration is required.

## Static validation set

The maintained release, DevExpress-control, ASCII color/game-authoring, Kernel Tournament, Chat ASCII, ASCII DOOM, Council role-context isolation, provider-qualified Council, architecture, service resilience, async continuation, configurable behavior, X-Round, codegen/DXFunction, cross-platform and PowerShell interpolation audits are run where applicable. JavaScript is checked with Node, the JavaScript diagnostics manifest is regenerated from LF-normalized maintained files, and JSON/XML files touched by release metadata are parsed.

Because this environment intentionally does not execute `dotnet`, this validation does not claim a compiler/build pass. The release is packaged only after its source-level guards pass and the archive is clean-extracted, path/CRC checked and byte-compared against the working tree.

## Results before packaging

- DevExpress Blazor control audit: passed; only the two `App.razor` reconnect controls remain native.
- ASCII color/game-authoring audit: 58 checks passed.
- Kernel Creature Tournament audit: 43 checks passed.
- Chat ASCII-console audit: 24 checks passed with popup-aware fullscreen-host semantics.
- ASCII DOOM campaign audit: 32 checks passed.
- Council role-context isolation: 7 isolated maintained presets; broad-context presets remain available.
- Provider-qualified Council audit: 282 checks passed.
- Architecture policy, service resilience, async continuation, configurable Council behavior, X-Round/heartbeat, codegen/DXFunction, PowerShell interpolation and cross-platform boundary audits: passed.
- JavaScript syntax, diagnostics-manifest verification, render-mode baseline comparison and JSON/XML parse checks passed against the finalized source tree; release-critical checks are repeated from the cleanly extracted ZIP where applicable.
