# LocalGPT 3.1.9 — Chat Quick Preset Row

## Why this release exists

The service wiring introduced in 3.1.6 and repaired through 3.1.8 works, but the visual placement still did not match the requested `/chat` workflow. The three selectors were first implemented as a page-level strip and then as an absolute overlay near the DevExpress composer. Both approaches added unnecessary layout coupling.

3.1.9 finishes the feature as a normal Blazor/DevExpress layout: one sibling row directly **under the chat** and directly above **Running session tools**.

## One normal-flow DevExpress row

- Removed the 3.1.8 absolute-positioned quick-selector overlay completely.
- Removed every selector-specific quick-layout CSS rule; there is no selector-specific CSS left for the quick row, including fixed widths, right/bottom offsets, overflow scrolling, pointer-event routing, media-query resizing and per-selector card styling.
- Added exactly one explicit Razor `<div>` for the quick configuration surface.
- Inside that one container, a `DxFormLayout` owns the layout and three `DxFormLayoutItem` components each use `ColSpanMd="4"`, producing one left-to-right three-column row on normal desktop widths and allowing DevExpress to adapt naturally on smaller widths without a custom horizontal scrollbar.
- The three `DxComboBox` components keep their existing DevExpress theme/style contract and typed callbacks.
- No custom `<div>` is wrapped around each selector.

## Existing Chat remains protected

- The entire detailed Chat Configuration ribbon/workbench remains structurally unchanged.
- The complete `<DxAIChat ...>...</DxAIChat>` subtree remains byte-identical to the known-good source.
- Attach, Send, Stop, the memo editor, transcript and prompt suggestions are not moved or resized.
- The only required existing CSS change is structural: the main Chat grid gains one normal-flow row for the new sibling surface. Optional ASCII-game grid definitions gain the corresponding extra row so Running session tools remains after the quick configuration row.
- After normalizing those row-count changes, `Chat.razor.css` still hashes to the known-good pre-feature value `3bc9693f026e410de1cd03c24544ab5695f58a13d238bc9710498eab6e090ad1`.

## Data behavior retained

Council-team, model-preset and hardware-performance-preset selection keeps the already-working service-backed data path from 3.1.8. The live Chat Configuration refresh and renderer-affine state commits remain unchanged.

## Preserved behavior

3.1.9 retains the provider-stream repetition watchdog, Council round recovery/failover, expected cancellation handling, benchmark evidence persistence, benchmark coverage truth guard, XML documentation completeness enforcement and all existing database compatibility contracts.

No EF migration, BenchmarkEvidence schema migration or 1-Wire protocol change is introduced.
