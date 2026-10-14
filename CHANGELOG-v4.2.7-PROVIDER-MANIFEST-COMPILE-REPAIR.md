# LocalGPT 4.2.7 — provider manifest compile repair

LocalGPT 4.2.7 is a source correction release for the 4.2.6 provider-storage work.

## Compile repair

- Fixes `CS1628` in `ProviderRuntimeManagementService.TryReadManifestDigests` by collecting manifest digests in a normal local `HashSet<string>` captured by the recursive local reader and assigning that completed set to the `out` parameter only after parsing succeeds.
- The failure path now assigns a fresh empty digest set to the `out` parameter, preserving the existing conservative deletion contract without capturing or mutating an `out` parameter from the local function.
- No provider-storage behavior is intentionally broadened: exact-manifest matching, retained-manifest verification and shared SHA-blob protection remain unchanged.

## Release identity

- Advances LocalGPT application, installer and webview-wrapper version identity from 4.2.6 to 4.2.7.
- Refreshes documentation/runtime identity strings and browser asset query versions that track the LocalGPT release.
- Preserves the 4.2.6 provider, ASCII terminal/Council display and macOS PDF fixes.

No .NET build, restore, publish, signing/notarization or GitHub operation was performed for this source handoff.
