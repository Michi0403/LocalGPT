# LocalGPT 4.4.2 source validation

This is source-only validation. No `dotnet`, NuGet restore/build/publish, EF CLI, GitHub, or online repository operation was performed in this environment.

## Menu regression restoration

- `src/LocalGPT/Components/Layout/MainLayout.razor` was restored byte-for-byte from the supplied working 4.3.6 source archive.
- `src/LocalGPT/Components/Layout/Drawer.razor` was restored byte-for-byte from the supplied working 4.3.6 source archive.
- The 4.4.1 `SidebarOpen` state, `Drawer.OpenState` override, direct `DxButton Click="ToggleSidebar"` shell toggle, and associated resynchronization logic are absent.
- The menu and drawer-header controls again use the existing `NavLink` + `NavigationUrlService.GetUrl(..., !ToggledSidebar)` workflow.
- `MainLayout`, `Drawer`, and `Index` retain the type-qualified `NavigationUrlService.ToggleSidebarName` query contract expected by `Assert-WorkflowContracts.ps1`.
- The maintained `SafeErrorBoundary @key="NavigationManager.Uri"` diagnostics contract remains intact.
- `Assert-WorkflowContracts.ps1`, `Assert-OperationalDiagnostics.ps1`, `Assert-InteractiveServerRenderModes.ps1`, and `Assert-TextServiceOwnership.ps1` are byte-for-byte unchanged from the 4.4.1 source package. No maintained rule was edited or weakened for this repair.

## Source checks executed

- `python3 build/audit_release_4_4_2.py` — passed.
- `python3 build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python3 build/audit_service_resilience.py --root . --product localgpt` — passed; 2469 service methods own try/catch + diagnostics, with the existing 29 iterator and 3 direct Program/Startup exclusions reported by the audit.
- `python3 build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 268 source files / 3322 await tokens; 2962 `ConfigureAwait(false)`, 137 reviewed renderer-affine `ConfigureAwait(true)`, 218 explicitly configured await-using disposals, and 5 configured async streams.
- `python3 build/audit_configurable_behavior_policy.py` — passed.
- Source-equivalent reproduction of the sidebar portion of `Assert-WorkflowContracts.ps1` — passed.
- Exact byte comparison of `MainLayout.razor` and `Drawer.razor` against the supplied 4.3.6 archive — passed.
- Final ZIP CRC test — passed.
- Extracted package rerun of the 4.4.2 release audit, application architecture audit, and async-continuation audit — passed.
- Extracted package comparison of `MainLayout.razor` and `Drawer.razor` against the supplied 4.3.6 archive — passed.

## Deferred work

The ASCII terminal/game modal/fullscreen lifetime correction and the DevExpress-component cleanup are intentionally not included in this emergency menu repair. They will be handled after the restored shell behavior is verified so those larger changes do not mask another navigation regression.

## Build limitation

A compiler/build result is intentionally not claimed. The Windows/macOS build still needs to be run in the user's normal .NET/DevExpress environment.
