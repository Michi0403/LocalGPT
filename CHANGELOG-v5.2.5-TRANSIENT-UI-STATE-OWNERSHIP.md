# LocalGPT 5.2.5 — transient UI-state ownership

## InteractiveServer editor race prevention

- Audited LocalGPT for the same live-control feedback loop that caused PublisherStudio RichEdit caret/toolbar races: transient vendor state must not be written to server component state and immediately rebound through an `InteractiveServer` rerender.
- Found one analogous LocalGPT surface: the runtime-extension `DxHtmlEditor` used delayed two-way `@bind-Markup`, so each live document notification could cause the enclosing Install component to rerender the same editor.
- Replaced that two-way binding with one-way `Markup` initialization plus an explicit `MarkupChanged` observer. The observer captures the current markup for Save/Build but suppresses the event callback's automatic render, leaving the live document/caret/selection vendor-owned while typing.
- Programmatic source replacement (new/edit/template/kind/save) advances an editor generation key, so stale editor-internal state is discarded only when the application intentionally supplies a new source generation.
- LocalGPT already follows the safer slider pattern established in 4.3.7/4.3.8: native range controls do not continuously commit through `@oninput`; durable values commit on `change`.

## Permanent maintenance contract

- Added `build/audit_transient_ui_state_ownership.py` and build-breaking `build/Assert-TransientUiStateOwnership.ps1`.
- The audit rejects `DxRichEdit` live document/selection two-way binding, editable `DxHtmlEditor` live Markup two-way binding, transient DevExpress selection/caret/scroll-style two-way bindings, `DxRangeSelector` server callbacks on every handle move, and LocalGPT native range `@oninput`.
- Observable RichEdit/HtmlEditor live-state callbacks must use a named handler that suppresses the automatic component render. The diagnostic explains the approved repair: one-way initialization, browser/vendor-owned live state, transient-state reset per editor generation, and explicit Apply/Save/change/pointer-up/handle-release commit boundaries.
- `Directory.Build.targets` wires the guard immediately after the InteractiveServer render-mode guard. The existing DevExpress retention guard now also verifies that this new enforcement path and its audit files cannot be silently removed.
- `AGENTS.md` records the same rule and explicitly rejects arbitrary delays, DevExpress removal, or editor disabling as race fixes.

No GitHub access, online repository access, `dotnet`, MSBuild, restore, build, or publish is used for this source handoff.
