# LocalGPT 2.5.6 — Install native scroll and provider delete

## Scope

This release is intentionally small and limited to the two `/install` usability gaps reported after 2.5.5: the setup page could be moved with the visible scrollbar but did not behave like a normal wheel/touch page, and already stored provider bindings were visible in the Configured AI hosts catalog without a direct delete action for every entry.

## Fixed

### `/install` uses one native vertical page scroller

- Removed the nested `overflow-y:auto` ownership from `install-page-scroll-owner` and let the already existing DevExpress drawer viewport remain the single page-level vertical scroll owner.
- The setup workbench remains in normal document flow, so native mouse-wheel scrolling and native touch `pan-y` target the same page scrollbar instead of competing with a second nested scroll surface.
- Bounded child controls keep their own internal scrolling; no model list, DevExpress control or workbench section was virtualized or rewritten.
- Added compact up/down helpers beneath the existing approval/Council tool rail on `/install`. They jump to maintained top/bottom anchors and do not replace native scrolling.
- Added localized accessible labels for the helper controls in `en-US` and `de-DE`.

### Configured AI hosts can be deleted directly

- Every card in `Configured AI hosts` now exposes a visible `Delete` action, including primary local OpenAI-compatible hosts, primary Ollama hosts, additional host bindings and the configured OpenAI/Azure provider entries.
- Deleting a card removes only LocalGPT's stored provider binding/settings and persists the updated configuration through the existing `Save()` path.
- Remote Ollama/LM Studio processes and remote model files are not deleted or modified.
- A still-reachable local/LAN host may continue to appear under discovery after its saved binding is removed; discovery remains intentionally separate from persisted configuration.
- Existing provider test, discovery, save, Council and Chat wiring is preserved.

## Preserved

- The 2.5.5 full-width `/install` workbench layout and section navigation remain unchanged.
- `@rendermode InteractiveServer` remains on Install and the other maintained interactive pages.
- Existing logging, component diagnostics, `ConfigureAwait(false)` policy, provider-qualified Council behavior and durable configuration writer remain unchanged.
- No JavaScript runtime file was added or modified for this patch; native browser/drawer scrolling is used instead.
- No LocalGPT 1-Wire protocol version change was made.

## Version

- LocalGPT: `2.5.6`
- LocalGPTInstallerConsole: `2.5.6`
- LocalGPTWebviewWrapper: `2.5.6`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)

## Build boundary

Per the delivery constraint, this package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Validation is source/static only and is recorded in `VALIDATION-v2.5.6-source.md`.
