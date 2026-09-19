# LocalGPT 4.5.1 — ASCII color build/compile repair

## Text-service ownership repair

- Moved styled-frame presentation signature composition from `ChatGameConsole.razor` into the already injected `AsciiChatTextService`.
- Added service-owned `BuildFramePresentationSignature` and `BuildAnimationPresentationSignature` operations with diagnostics/fallback behavior.
- Removed the two component-local `string.Join` expressions reported by `Assert-TextServiceOwnership.ps1`; the Razor component is back to presentation/orchestration-only behavior.
- Kept canonical ASCII frame text and style metadata unchanged.

## Kernel Tournament compile repair

- Corrected both tournament style-generation call sites to invoke the maintained five-argument `BuildSemanticAsciiStyleRuns(gameKey, runtimeProfile, frame, frameWidth, frameHeight)` contract.
- Preserved all 4.5.0 tournament animation, subtitle, HP/bracket authority and semantic color behavior; this is a call-contract repair rather than a rules change.

## Release identity

- Advanced LocalGPT from 4.5.0 to 4.5.1.
- Updated application/installer/webview-wrapper versions, browser cache keys, user-agent strings and current documentation release identity.
- Council team seed version stays 33, runtime-class seed version stays 7, and the wire protocol is unchanged.
- Added a 4.5.1 release audit and validation note targeted at the exact reported build regressions.

## Validation boundary

- Source/static validation only in this environment: no .NET restore/build/publish and no GitHub/online repository access.
- The final source ZIP is CRC/path-safety checked, clean-extracted and byte-compared before delivery.
