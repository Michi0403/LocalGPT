# LocalGPT 3.1.8 — Chat Quick Preset Isolation

## Why this release exists

LocalGPT 3.1.6/3.1.7 added three service-backed quick selectors for Council team, model preset and hardware performance preset. The data path was useful, but the first visual implementation incorrectly modified DevExpress `DxAIChat` composer/textarea dimensions to reserve space for the new selectors. In a real published build this collapsed the practical chat input area and made `/chat` unusable.

3.1.8 removes that coupling. The quick selectors remain, but they are an additive sibling overlay only. The existing Chat Configuration workspace and the complete `DxAIChat` subtree stay structurally unchanged.

## Composer layout restored

- Removed the 3.1.6 quick-feature CSS that forced `.localgpt-chat-composer` to `min-height: 8.6rem` / `max-height: 13rem`.
- Removed the quick-feature CSS that forced the textarea to `min-height: 8.25rem` and `padding-bottom: 4.4rem`.
- Restored every pre-quick-selector line of `Chat.razor.css` to the known-good 3.1.5 baseline.
- The quick selector dock now uses only its own sibling selectors and `#localgpt-chat-host` as a positioning context. It does not target the DevExpress composer, textarea, submit area, attachment button, send button or stop button.
- The dock is content-width rather than left/right stretched, so the three selectors stay compact beside the prompt actions instead of becoming a full-width configuration strip.

## Protected existing UI

The release audit hashes and protects two existing Razor regions:

- the entire Chat Configuration ribbon/workbench block;
- the complete `<DxAIChat ...>...</DxAIChat>` subtree.

Both hashes are identical to the 3.1.5 known-good source. The only quick-selector Razor markup remains after `</DxAIChat>` as a sibling.

## Renderer-affine service refresh

The service refresh added in 3.1.6 is retained, but its state commits are hardened:

- Council teams, model presets, performance presets, persistent prompt starters, memory and project data are fetched as local snapshots;
- component-owned lists/selections are committed through renderer-affine `InvokeAsync` on the Blazor renderer;
- service/background awaits continue to use `ConfigureAwait(false)` where appropriate, without using the resulting continuation to directly mutate Razor component state.

This preserves the requested live refresh behavior without coupling service refresh to another rendering circuit or DevExpress control lifecycle.

## Preserved behavior

3.1.8 keeps the typed `DxComboBox` callbacks from 3.1.7, live configuration refresh from 3.1.6, provider stream repetition watchdog from 3.1.5, Council round recovery/failover, expected cancellation handling, benchmark audit evidence, coverage truth guard and XML documentation completeness enforcement.

No EF migration, BenchmarkEvidence schema migration, 1-Wire protocol change, or Social Team workflow removal is introduced.
