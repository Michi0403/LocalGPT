# LocalGPT 4.4.1 — Sidebar state contract repair

## Fixed

- Repaired the navigation drawer regression introduced by the 4.3.8 sidebar-stability work. The menu button now toggles renderer-owned shell state immediately without changing the current URI, so opening or closing the drawer does not recreate the routed body or interrupt an active Chat/game surface.
- Restored the maintained `SupplyParameterFromQuery(Name = NavigationUrlService.ToggleSidebarName)` contract on `Drawer.razor`. The 4.3.8 implementation had replaced that maintained query contract with a plain component parameter even though `Assert-WorkflowContracts.ps1` still required the original contract.
- Added an explicit nullable `OpenState` parameter to `Drawer`. `MainLayout` supplies this only for the live shell interaction; when no live override is supplied, the existing query-backed drawer state remains authoritative.
- Added renderer-owned `SidebarOpen` state in `MainLayout`. Query state is accepted when the URI changes, but an in-place menu click is no longer immediately overwritten by the unchanged query value during component parameter refreshes.
- The header menu/close icon and Home navigation state now read the same live shell state as the drawer. No buttons, routes, page layout, game behavior, or CSS were removed or redesigned.

## Maintenance-rule correction

`Assert-WorkflowContracts.ps1` was not changed for this repair. Instead, the source is again compliant with its original requirement that `Drawer`, `MainLayout`, and `Index` retain the type-qualified sidebar query contract.

The historical `audit_release_4_3_8.py` is intentionally left unchanged: it documents the mistaken 4.3.8 release assumption that replaced the Drawer query contract with a plain `[Parameter]`. The repair is made in current source instead of rewriting that historical audit. No maintained guard was weakened, skipped, edited, or given an exemption.

## Preserved

- Exact `<SafeErrorBoundary @key="NavigationManager.Uri" ...>` operational-diagnostics contract.
- 4.3.8 Chat/game/Operator state ownership and no-URI-mutation goal for drawer clicks.
- 4.3.9/4.4.0 Game-project authoring, build/runtime layering, migrations, requirements and Council preseeds.
- Existing responsive drawer width, CSS/flexbox layout, DevExpress drawer/button controls, localization, logging and component diagnostics.
