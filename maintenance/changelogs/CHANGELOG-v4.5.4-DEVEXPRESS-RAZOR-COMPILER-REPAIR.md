# LocalGPT 4.5.4 — DevExpress Razor compiler repair

## Razor template and component-attribute repair

- Resolved the two Razor `RZ9999` child-content context collisions by giving the Local Path Explorer popup body and the AI Chat message template explicit context names. This keeps nested DevExpress buttons intact without sharing the implicit `context` parameter from their enclosing templates.
- Rewrote the bounded-number slider button `title` and `aria-label` values as single Razor expressions so DevExpress component attributes no longer mix literal text with inline C# (`RZ9986`).
- Kept the existing UI wording and behavior; these changes are compile-surface repairs rather than a layout redesign.

## DevExpress callback typing repair

- Added explicit callback value types where DevExpress generic inference had degraded values to `object` or non-generic `EventCallback`: booleans, integers, nullable integers, hardware kinds, range-selector event args, strings, and provider-selection values now reach the existing handlers with their intended types.
- Repaired the affected bindings in `BoundedNumberEditor`, `TestLab`, `CouncilHardwareRoadEditor`, provider benchmark/history panels, `ProviderModelPanel`, `InitialSetupAssistantPanel`, and `ModelCouncil`.
- Repaired the malformed `DxFunctionCatalog` checkbox conversion. The five catalog flags are again bound to their model properties and call `MarkGridEntryDirty` through supported `@bind-Checked:after` callbacks.

## Release boundaries

- Advanced LocalGPT from 4.5.3 to 4.5.4 while keeping the single-digit minor/patch slot policy intact.
- Preserved all 20 maintained InteractiveServer render-mode declarations, Council seed versions, wire protocol behavior, database model/migrations, deployment flow, and the 4.5.2 ASCII/fullscreen/autoscroll/game-ready work.
- Updated LocalGPT application/installer/webview versions, browser cache keys, user-agent strings, release notes, validation notes, and documentation release identity.
- Validation in this environment remains source/static only: no `.NET` restore/build/publish and no GitHub/online repository access is used.
