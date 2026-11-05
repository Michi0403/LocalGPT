# LocalGPT 5.7.5 – Navigation, Theme Fusion, Help and Debug documentation

**Basis:** The complete supplied LocalGPT 5.7.4 source ZIP, cross-checked against 5.7.1 / 5.6.1 and the supplied rendered HTML, screenshots and runtime log. No PublisherStudio files were modified.

## Regression corrections

- Moved the interactive `LanguageSwitcher` from the *page content toolbar* (which is covered by the open overlapping navigation drawer) into `MenuIsland`, below the logo and Theme Fusion button and ahead of the navigation entries. Added owning-component scoped CSS for responsive selector width, and maintained a renderer-affine `ValueChanged` continuation. The ineffective cross-component `MainLayout.razor.css` selector rules are removed. The selected culture URL mechanism is preserved.
- Replaced Theme Fusion's manually positioned, drawer-contained fixed overlay with the existing DevExpress `DxPopup` mechanism, keeping the same interactive `MenuIsland`, `ThemeService` and `ThemeJsChangeDispatcher` as before. The popup has a viewport-bounded 1360px maximum width, user-dismiss callback, internal scrolling and the original Base/Style/Fusion Route content. A fixed descendant of the animated drawer cannot reliably expand outside its containing/stacking context.
- Fixed the **logged Theme Fusion reset `TaskCanceledException`** at its cause: JavaScript no longer calls `window.location.reload()` inside the awaited `resetFusionRoute` interop invocation. It persists/removes the route and returns; the existing C# dispatcher then requests the clean full reload via `NavigationManager.NavigateTo(..., forceLoad: true)`. This keeps the same route semantics without canceling the outstanding JS return. The PowerShell theme contract guards against a relapse.
- Corrected theme option sizing: form-layout choice wrappers and anchors now use their allotted full widths; the legacy `max-width: 50%` no longer halves each choice twice. The responsive Fusion Route grid-column rule targets the actual third DevExpress *form-layout grid child*, not its nested content.
- Corrected unreadable secondary Help actions in mixed light-shell/dark-DevExpress themes with explicit shell-color contrast, background, border and hover states. Existing `/help` actions and documentation viewer routes remain intact.
- Restored default documentation generation for **ordinary Debug MSBuild builds** by removing the Debug opt-out in `Directory.Build.targets`. This was why Debug output showed `HTML not built` and `PDF not included`, disabling both open actions. The `Build-LocalDevelopment.ps1` *orchestrated* sequence already deliberately builds the app/wrapper with documentation disabled and then invokes `Build-Documentation.ps1` exactly once. Those explicit passes are left unchanged: no duplicate DocFX/PDF generation was added. The existing mandatory PDF release/development contract is preserved.
- Reinstated missing `build/Assert-XmlDocumentationCoverage.ps1`, which `Invoke-RepositoryValidation.ps1` still invokes, as a native PowerShell file-level coverage gate. It is **not** claimed to be semantically equivalent to the old Python parsers: Roslyn, Razor and the documentation build continue to own semantic validation.
- Updated `Assert-ThemeArchitecture.ps1` to guard the portaled popup and new correct responsive form-layout sizing rather than hard-code the old 960px fixed overlay.

## Constraints and validation

- **No Python interpreter required** for Debug, release, architecture checks, documentation, or GitHub Pages. Optional application Python.NET runtime unchanged; no Python files added.
- No .NET, PowerShell, DocFX, DevExpress or live interactive Windows UI execution was performed in the editing environment. All changed source, CSS, XML and PowerShell entrypoints require the user's Windows build and UI tests before this release is considered runtime verified.
- The generated build documentation and PDF remain generated artifacts; they are not included as stale binaries in the source ZIP. The source retains all DocFX inputs and the tracked Pages snapshot.
- Version 5.7.4 → **5.7.5** (single digits for both minor and patch).
