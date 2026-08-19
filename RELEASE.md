# LocalGPT 3.1.9

LocalGPT 3.1.9 is the **Chat Quick Preset Row** release. It keeps the working service-backed Team, Models and Performance selectors while completing their requested visual placement as one normal-flow DevExpress row directly below Chat and above Running session tools.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Quick Preset Row

The three selectors are now bundled in a single `DxFormLayout`. Each item occupies four of twelve medium-width columns, so normal desktop layouts show Team, Models and Performance side by side from left to right. Smaller layouts are handled by DevExpress FormLayout rather than selector-specific CSS or a horizontal scrolling dock.

The quick row is outside the DevExpress Chat host. It does not modify the memo editor, Attach, Send, Stop, transcript, prompt suggestions or detailed Chat Configuration workspace.

## CSS boundary

All 3.1.8 quick-selector overlay CSS was removed. The only existing Chat CSS change adds one normal-flow grid row (and the matching optional ASCII-game row) so the new sibling has a real layout slot. The release audit normalizes those row-count changes and verifies the rest of `Chat.razor.css` still matches the known-good pre-feature baseline.

## Preserved behavior

3.1.9 retains live service refresh, renderer-affine state commits, the repetition watchdog, Council recovery/failover, cancellation handling, benchmark evidence, coverage truth guard and XML documentation completeness. No database migration or evidence-schema migration is introduced.

See `CHANGELOG-v3.1.9-CHAT-QUICK-PRESET-ROW.md` and `VALIDATION-v3.1.9-source.md`.
