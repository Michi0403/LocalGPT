# LocalGPT 5.4.8 source validation

## Completed without .NET or GitHub

- Compare 5.4.4 Inbox and Chat presentations against the 5.4.7 shared request card; preserve the common DB-backed decision lane and restore request metadata.
- Run `python build/audit_release_5_4_8.py` to check the full DevExpress retention contract, top-level injections, single card, shared approval semantics, disclosure maximum and XML documentation.
- Run the existing source-only Razor maintenance, transient UI-state, async-boundary and human collaboration audits. The targeted 5.4.7 ASCII fullscreen/terminal audit must continue passing.
- The original Inbox and Chat minimum DevExpress counts remain 41 and 99. The shared card has its own 15-control baseline; the guard credits it only where explicitly listed and actually rendered.

## Required on the maintainer's .NET 10 / DevExpress 25.2.10 environment

1. Rebuild LocalGPT with Visual Studio or the repository's maintained build path and confirm `Assert-ComponentSafety.ps1` and `Assert-DevExpressComponentRetention.ps1` pass and no CS1571/CS1572 warnings remain in `HardwarePerformancePresetService.cs`.
2. With Chat and Human Collaboration open in separate tabs, create a pending approval and a guidance/poll request. Verify both views render the same canonical request, the complete question scope/gate/phase metadata, and the same current answer/suggestion state after refresh.
3. Approve in one tab; verify the second cannot overwrite a stale request, that the decision appears in the DevExpress saved-history accordion and Inbox history, and that a deferred approved action executes at most once. Declines must never execute a deferred action.
4. Test accessibility, responsive layout and edit controls for answer, reason, reuse scope and one-use checkbox on both surfaces. Check `DxAccordion` expansion and preserve the existing Chat configuration disclosures.
5. Repeat the 5.4.7 terminal/shell/fullscreen smoke tests. No runtime behavior was verified in this source-only environment.
