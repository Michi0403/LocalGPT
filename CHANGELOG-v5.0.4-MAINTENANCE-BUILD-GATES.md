# LocalGPT 5.0.4 — maintenance and build-gate repair

## Fixed

- Replaced the temporary native language `<select>/<option>` implementation in `MainLayout` with a DevExpress `DxComboBox`, preserving culture navigation while satisfying the repository's DevExpress-first Razor control policy.
- Converted `CouncilRuntimeService.EscapeUnescapedJsonControlCharacters` from an application static helper to an instance-owned service method and retained a logged exception boundary.
- Converted `LocalGptUiLayoutService.NormalizeLayoutType` and `NormalizeOrientation` from static helpers to instance-owned service methods and added logged exception boundaries.
- Preserved the 5.0.3 malformed-provider-JSON recovery, chat scrolling, microphone, layout-family, code-artifact workflow, and InteractiveServer behavior.

## Version

- Application version advanced from 5.0.3 to 5.0.4.
- Active browser-module cache busters were advanced to 5.0.4.
