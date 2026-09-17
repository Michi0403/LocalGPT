# LocalGPT 4.4.2 source validation

Source-only validation; no .NET command and no GitHub/online repository access was used. The 4.4.2 repair is intentionally narrow: `MainLayout.razor` and `Drawer.razor` are restored exactly to the working 4.3.6 menu/drawer implementation, the 4.4.1 shadow sidebar state is removed, maintained workflow/diagnostics rules are left unchanged, and the 4.4.0 Game-project work is preserved. See `VALIDATION-v4.4.2-source.md` for executed checks and limitations.
