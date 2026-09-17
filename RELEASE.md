# LocalGPT 4.4.2

LocalGPT 4.4.2 is a focused shell/menu regression repair. The navigation menu and Drawer implementation are restored to the last coherent working implementation from 4.3.6 instead of carrying forward the renderer-owned sidebar workaround introduced later. This removes the `SidebarOpen`/`OpenState` shadow-state path and returns menu opening/closing to the existing query-backed `NavigationUrlService` + `NavLink` workflow that the repository maintenance contract already expects.

The 4.4.0 Game Project authoring, requirements, migration, Council preseeds, runtime boundary, and existing ASCII/Council functionality remain preserved. The separate ASCII-terminal modal/fullscreen lifetime correction and the broader DevExpress-control cleanup are intentionally deferred until after this shell regression is confirmed fixed.

No .NET build, restore or publish was performed in this environment, and no GitHub or other online repository access was used. See `CHANGELOG-v4.4.2-MENU-REGRESSION-RESTORATION.md` and `VALIDATION-v4.4.2-source.md`.
