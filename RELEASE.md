# LocalGPT 3.1.8

LocalGPT 3.1.8 is the **Chat Quick Preset Isolation** repair release. It keeps the three service-backed `/chat` quick selectors and live configuration refresh, while restoring the DevExpress chat composer to the exact pre-feature layout contract.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Quick preset isolation

The Council team, model preset and performance preset selectors remain available, but they are now a compact sibling overlay after `DxAIChat`. They do not reserve space by changing the DevExpress composer or textarea.

The detailed Chat Configuration workspace is unchanged. The complete `DxAIChat` subtree is unchanged. Static release guards hash both regions and the known-good pre-feature Chat CSS so future quick-action work cannot silently rewrite those integration boundaries.

## Renderer-affine refresh

The live service refresh requested for Chat Configuration remains enabled. Service reads happen independently, while component-owned lists and selections are committed through the Blazor renderer with `InvokeAsync`. Provider discovery keeps its existing separate refresh path.

## Preserved behavior

3.1.8 retains the 3.1.7 typed DevExpress callbacks, 3.1.5 repetition watchdog, Council recovery/failover, cancellation handling, benchmark evidence, coverage truth guard and XML documentation completeness work. No database migration or evidence-schema migration is introduced.

See `CHANGELOG-v3.1.8-CHAT-QUICK-PRESET-ISOLATION.md` and `VALIDATION-v3.1.8-source.md`.
