# LocalGPT 6.8.6 — offline verification report

## Input and constraints

Baseline `LocalGPT-6.8.5-Source-Download.zip`. Work was performed on the supplied source only, with no GitHub fetch, no `dotnet` invocation and no PowerShell runtime available in this environment.

## Previously failing gates

1. **`Assert-CouncilXRoundWiring.ps1`**: 6.8.5 source contains `await Task.Delay(1500, cancellationToken).ConfigureAwait(false);` in `ScheduleLiveCouncilListRefresh()`, but the guard expected the literal `750`. Verified that the revised guard scopes its inspection to that method and requires a 500–5000 ms cancellable delay, atomic pending flag set/reset and lifecycle checks. Negative controls remove the delay, cancel token or scheduling guard and are rejected.
2. **`Assert-RazorMaintenanceArchitecture.ps1`**: 6.8.5 live transcript used `<section class="chat-council-live-replay">`, which the existing architecture guard rejects. Now it uses `<div class="chat-council-live-replay" role="region" ...>`. The explicit `<section>`/`<dialog>` ban remains unchanged, with the live output intact. Source scan finds zero such violations.

## Executed offline checks

- 19 static and negative-control assertions pass (Razor wrapper, method-specific debounce, cancellation, atomic scheduling, accessibility, live output, source delta, XML and version).
- The existing Council guard's 32 required source-contract entries (89 literal required tokens) are present.
- Simulated exact Razor architecture scan of 62 `*.razor` files finds zero disallowed native `section/dialog` or missing `DxGridLayoutItem` template contracts.
- Project version equals `6.8.6`; normal version rollover convention remains respected.
- All existing repository file bytes match 6.8.5 except `build/Assert-CouncilXRoundWiring.ps1`, `src/LocalGPT/Components/Pages/Chat.razor` and `src/LocalGPT/LocalGPT.csproj`.
- The packaged ZIP is CRC-verified and all unchanged entries have their original SHA-256 file hashes (packaging checks follow).

## Not executed / remaining risk

- The actual PowerShell scripts (PowerShell is not installed here); Windows PowerShell 5.1 parser/runtime differences are not independently confirmed.
- The .NET, Razor and DevExpress compiler/runtime, the full maintenance gate sequence and browser performance.
- The prior Council connection-drop, game display, transcript restoration and fullscreen defects still require a long real-run test and cannot be assumed fixed by these two preflight repairs.
