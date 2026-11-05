# LocalGPT 6.5.0 — interactive forms, pairing-ticket visibility and TLS status

## Windows reports addressed

- The generated TLS certificate editor previously used `DxCheckBox Checked/CheckedChanged` without `CheckedExpression` in an `EditForm`, causing DevExpress's `CheckBoxModel.CheckValueExpression` to throw and the Blazor InteractiveServer circuit to disconnect. `NetworkCertificateEditor.razor` now binds the CA checkbox to the BusinessObject expression while retaining the existing DataAnnotations validator. Other calculated, manually controlled checkbox states explicitly opt out of *field identification*, not service permission or business validation.
- Removed the unwanted floating `▲` and `▼` anchors from the global toolbar and removed their unused Install anchors/CSS. The Install page continues to use its native drawer viewport for mouse-wheel/touch scrolling.
- Opening the `Pairing & trust` section now generates a fresh, short-lived signed **public** pairing ticket from the existing local identity; it does not persist private material, automatically display existing MFA enrollment secrets, rotate the identity, or revoke trusted peers. The UI clarifies the distinction between persistent reciprocal trust records, expiring tickets, and active live connections. The existing manual Create pairing material action still controls MFA enrollment.
- The remote endpoint preview is now explicitly a *configuration request*, not the active Kestrel transport. A saved certificate path never asserts that HTTPS is active. The UI explains that a missing/invalid PFX can lead to HTTP fallback, requiring review of the startup `Now listening on` and TLS diagnostic entries.
- Corrected LocalGPT's active version from the forbidden `6.4.10` to `6.5.0`, complying with the single-digit minor/patch policy. Other project versions remain unchanged.

## Regression prevention

- Added `build/Assert-InteractiveFormsAndPairing.ps1`, invoked from normal LocalGPT MSBuild and `Build-Release.ps1`, that fails on reintroduced scroll anchors, missing CA model validation expression, missing public ticket refresh, misleading TLS preview text, unhandled manually checked DevExpress editors, or multi-digit minor/patch versions across active source project files.
- Added 7 `tests/static/test_interactive_forms_pairing_650.py` offline contracts. All original maintenance scripts were preserved, not weakened.

## Validation scope

- 114 source-oriented Python/unittest tests passed on the included source after fixes.
- Source XML and ZIP integrity checked.
- **Not** validated: Windows PowerShell execution, actual .NET/Razor build, DevExpress InteractiveServer browser operation, or an end-to-end LocalGPT ↔ PublisherStudio 1-Wire connection. These require the user's installed toolchain and live runtime. Do not treat static tests as release acceptance.
