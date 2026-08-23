# LocalGPT 3.2.8

LocalGPT 3.2.8 repairs the `/dx-functions` InteractiveServer catalog presentation regression introduced by the responsive workbench loading flow.

The database-backed entries were loading, but the first interactive `OnAfterRenderAsync` path rendered the loading frame before awaiting the catalog and never scheduled the completed state for rendering. The release adds the required post-load render and aligns the DX Function catalog's asynchronous state-changing helpers with the repository's renderer-affine continuation policy.

PublisherStudio remains at 2.9.7 because it is unchanged in this round.

This archive is **SOURCE-NOT-COMPILED** in the preparation environment. The user's Windows build remains authoritative.

See `CHANGELOG-v3.2.8-DX-FUNCTIONS-INTERACTIVE-RENDER-REPAIR.md` and `VALIDATION-v3.2.8-source.md`.
