# LocalGPT 4.4.2 — Menu regression restoration

## Scope

This release removes the 4.3.8–4.4.1 navigation-drawer workaround and restores the proven shell/menu implementation from the last coherent pre-regression source instead of introducing another sidebar state model.

## Restored behavior

- `MainLayout.razor` is restored byte-for-byte to the working 4.3.6 shell implementation for the navigation/menu surface.
- `Drawer.razor` is restored byte-for-byte to the working 4.3.6 drawer implementation.
- The menu and drawer-header controls again use the existing `NavigationUrlService` query contract and `NavLink` flow that the application and workflow guard were designed around.
- `Drawer.ToggledSidebar` is again supplied directly from the maintained query contract; the 4.4.1 `OpenState` override and `SidebarOpen` shadow state are removed.
- No new render-state bridge, alternate lifecycle state, or maintenance-rule exception is introduced.

## Deliberately not changed

The requested ASCII-terminal modal/fullscreen lifetime correction and the broader DevExpress-control cleanup are not folded into this emergency regression repair. They remain the next feature/refactor work so the shell fix stays minimal and independently testable.

The 4.4.0 Game Project authoring, requirements, migration, Council preseeds, runtime boundary, and prior ASCII/Council features remain in place.

## Validation boundary

The repository's maintained workflow and diagnostics rules were not weakened or edited for this repair. Source-only audits were run where available. A .NET/DevExpress compiler build was not available in this environment and is not claimed.
