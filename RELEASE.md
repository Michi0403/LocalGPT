# LocalGPT 4.4.5

LocalGPT 4.4.5 is a focused ASCII runtime, UI stability, and documentation-atmosphere release on top of 4.4.4.

The built-in ASCII DOOM Council adventure now ships a resettable, copyable `games.ascii.doom.campaign` runtime-class seed with ten deterministic starter levels. The starter curve progresses from difficulty 1 through 10 with explicit map dimensions, room counts, enemy density, hostile health/damage multipliers, and starting resources. Automatic level advance is enabled by the supplied class. These values are configuration, not hidden engine policy: a user can copy the runtime class, change the level count/order/parameters, assign the copied class to a copied Council team, and the game service resolves that team assignment before the system seed. Invalid or empty campaign configuration is rejected instead of silently inventing a replacement difficulty curve.

The ASCII DOOM Council preset no longer implies that the absence of a Council-role human response disables game input. Its three roles allow optional human participation while the authoritative runtime control remains independently switchable between **Human**, **Human + AI** (`Shared`), and **AI hunter**. Shared mode accepts explicit human and AI controls without enabling background autoplay; AI hunter remains the only autonomous mode.

`ChatGameConsole` keeps the repository's explicit continuation convention. Renderer-affine lifecycle, DevExpress selection, JS interop, callbacks, and component-state continuations use explicit `ConfigureAwait(true)` rather than implicit awaits, while background/service continuations remain explicit `ConfigureAwait(false)`. The popup layout no longer forces the game stage to consume 100% of the popup in addition to its header/footer, and the popup viewport no longer reserves an unnecessary scrollbar gutter.

The Kawaii documentation sky also gains rare shooting-star/meteor passes. They remain bounded to the fixed documentation sky, respect reduced-motion preferences, and use infrequent randomized scheduling so the effect stays atmospheric rather than distracting.

All 4.4.4/4.4.3 protections remain in place: Razor-safe DevExpress `CssClass` bindings, the required ASCII surface, popup lifetime behavior, low-level `data-game-action` input contract, macOS Installer product title metadata, Mermaid layout recovery, the maintained `InteractiveServer` route model, and the restored 4.4.2 navigation/menu implementation.

No .NET build, restore, publish, NuGet operation, GitHub access, GitHub API call, or other online repository access was performed in this environment. See `CHANGELOG-v4.4.5-ASCII-CAMPAIGN-UI-DOCS.md` and `VALIDATION-v4.4.5-source.md`.
