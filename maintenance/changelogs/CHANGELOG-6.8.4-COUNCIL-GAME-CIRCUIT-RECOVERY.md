# LocalGPT 6.8.4 — Council/game circuit recovery

Base: 6.8.3. Source-only maintenance release; no .NET build, Windows runtime test, or GitHub access was performed.

## Confirmed defects addressed

- `Chat.StartAsciiConversationMirrorLoop()` previously read `componentLifetimeCts.Token` while the Blazor renderer could already have called `Dispose()` on that source. A concurrent circuit teardown raised `ObjectDisposedException` during `EnsureRequiredAsciiSurfaceState()` / `CreateCouncilRequest()`. Capture the token once in the component constructor; all Chat partials use that immutable token to supervise/cancel background work. The original cancellation source is still canceled and disposed by the owning component.
- Apply the same ownership correction to `ChatGameConsole`'s console rendering, deferred approval submissions, and other asynchronous workers. The component no longer re-reads `consoleRenderCancellation.Token` after it has been disposed.
- `ChatGameConsole.OnInitializedAsync()` could attach an already-running Council game while leaving `conversationMode = true`; the terminal consequently showed Chat rather than the active game. Enter Game mode on initial active-game attachment and first late attachment, without forcing the user back out of Chat on later routine rerenders.
- When a Council run id is present, the game terminal now looks up and accepts **only** that run's game. A conversation-associated game from another run cannot displace it. Standalone conversation-based game lookup remains available when no run id is bound.
- Ignore game-change callbacks after the console has been disposed. On a newly restored Chat runtime, reevaluate whether the persisted Council team requires the ASCII surface, without starting another Council or game.
- `CreateCouncilRequest()` no longer returns a fabricated empty `MultiModelCouncilRequest` after a preparation exception. The existing Council client observes a failed factory instead of accidentally launching a different workflow/team.

## Architecture and validation

- Updated `build/Assert-CouncilXRoundWiring.ps1` to validate constructor-cached cancellation tokens, both game recovery presentation paths, correct per-run game selection, and fail-closed request creation. Old 6.8.3 source does not satisfy these new regression contracts.
- Repaired stale `build/Assert-ServiceArchitecture.ps1` source-location assumptions: the synchronous Routes component is checked for its actual subscribed event/disposal contract, while Chat workers and autosave are checked in their owning Razor code-behind partials, not mistaken for inline markup. Existing checks for service DI, diagnostics, and async supervision stay present.
- Existing game engine, game rules, contest progression, game project layer, Council role assignment and persistence logic are unchanged.

## Runtime/source version discrepancy

The supplied Windows runtime log starts with assembly version `6.4.1.0` even though the latest supplied source ZIP is `6.8.3`. Verify the executable that Visual Studio or the installer actually launches after building; this source archive alone cannot prove which binary was running when the failure occurred.

## Validation limitation

Offline static source checks and negative controls passed, but **PowerShell was not executed** and **no .NET compilation or end-to-end live game test** was run. A successful release/Debug build and an interactive Council game session remain to be verified on the maintainer's environment.
