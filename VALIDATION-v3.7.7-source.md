# LocalGPT 3.7.7 source validation

Validation is source/static only in this environment. No .NET build, macOS signing, Apple upload/notarization, package publication, or GitHub access was performed.

Checked invariants:
- version references advance to 3.7.7 with no `x.y.10` version;
- every normal routed LocalGPT page keeps its reviewed `@rendermode InteractiveServer` boundary and the error fallback remains intentionally static;
- `Build-Release.ps1` re-runs `Initialize-MacReleaseTrust.ps1` immediately before each `osx-*` release lane;
- fresh native notarization calls `Assert-MacNotaryCredentialsUsable` immediately before `notarytool submit ... --wait --timeout ...` and retries only the explicit missing-keychain-item failure once;
- prior `.notary-state.json` resume remains available through `notarytool info` polling;
- adaptive `html-browser-chunked` PDF acceptance/completeness checks from 3.7.6 remain present;
- no repository-local build output was intentionally introduced.
