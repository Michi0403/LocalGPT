# LocalGPT 3.2.8 — DX Functions interactive render repair

## DX Functions catalog loading

- Repairs `/dx-functions` after the responsive workbench conversion left the first InteractiveServer catalog load in an already-completed component state that was never rendered.
- The page now requests a second render after `ReloadAsync()` completes in `OnAfterRenderAsync`, so the loaded database-backed catalog, status text, navigation badges and enabled controls become visible immediately.
- Preserves the existing explicit first-render loading surface; users still see a connected/loading state while the large catalog is being read instead of receiving apparently active controls before the circuit is ready.
- Keeps `FilteredEntries` as a derived view over the authoritative `_entries` collection. No second catalog cache or duplicate mutable filter collection is introduced.

## Renderer-affine component work

- `ReloadAsync`, `SynchronizeAsync`, `SaveAsync` and `SaveVisibleAsync` are now explicitly renderer-affine in the repository async-continuation policy, matching their component-state and notification responsibilities.
- Catalog reload/synchronize/save continuations use `ConfigureAwait(true)` so component state and DevExpress notifications are updated on the InteractiveServer renderer context rather than on a detached worker continuation.
- Existing method-local catch/log/notification boundaries, InteractiveServer render mode, 1-Wire behavior, user function editor, filters, card/grid views and persistence services are retained.

## Runtime catalog findings

- The supplied runtime log shows 1,560 **unique** catalog rows synchronized with zero duplicate rows removed, so the blank workbench was not caused by an empty database or another duplicate explosion.
- No database migration, schema change, provider contract change, package upgrade or PublisherStudio change is included in this release.

## Version

- LocalGPT, InstallerConsole and WebView wrapper advance to **3.2.8**.
- DevExpress remains **25.2.9**.
