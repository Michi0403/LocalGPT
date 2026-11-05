# LocalGPT 4.6.3 — install UI, invariant numeric and CanIRun action repair

LocalGPT 4.6.3 is a focused frontend/runtime repair on top of 4.6.2. It restores the `/install` interaction contract after DevExpress conversions without changing the 4.6.2 ingestion, 1-Wire, regex-curator or semantic-ASCII architecture.

## `/install` DevExpress control repair

- Removed Bootstrap native-input classes from DevExpress text, memo, spin and checkbox controls so Bootstrap positioning no longer overlays DevExpress internals or leaves duplicate-looking checkboxes/controls behind buttons.
- Added neutral `localgpt-editor`, `localgpt-check` and `localgpt-check-row` contracts for DevExpress controls.
- Restored compact hardware/model/benchmark layouts instead of forcing every model property onto a separate line.
- Hardware-curated benchmark model rows now keep Use, Curator, model identity and endpoint together in compact responsive cards.

## Invariant numeric input

- Hardware RAM/VRAM editors use a DevExpress text-editor based invariant numeric wrapper rather than slider/range controls or locale-fragile decimal spin editors.
- Numeric input accepts invariant notation and also tolerates the selected UI culture, but normalizes display back to invariant decimal notation.
- Detected RAM/VRAM capacities are normalized to reviewable increments so byte-to-GiB conversion noise such as long binary fractions is not shown as the editable hardware value.
- Request culture keeps the selected UI language/date conventions while using invariant number separators for mathematical/editor values.

## CanIRun.ai resolve/install restoration

- CanIRun.ai hardware-fit result cards again expose a direct **Resolve & install model** action for the selected provider.
- Exact provider mappings install immediately through the maintained provider bootstrap service; unresolved recommendations are checked against the selected provider's official catalog first.
- The same path works through the maintained Ollama and LM Studio provider profiles and does not guess ambiguous model identifiers.
- Already installed models expose the existing provider-safe refresh/check action when supported.

## Regression protection

- Added a 4.6.3 release audit that rejects Bootstrap native-input classes on DevExpress editors/checkboxes, verifies invariant hardware numeric wiring, compact benchmark UI, CanIRun direct action wiring, prior 4.6.2 capabilities and the 4.6.1 service-lifetime repair.

PublisherStudio source is unchanged.
