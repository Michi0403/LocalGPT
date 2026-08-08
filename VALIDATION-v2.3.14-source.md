# LocalGPT 2.3.14 source validation

Source-only validation performed without claiming a .NET compile in the packaging environment.

- `Assert-StaticWebAssets.ps1` no longer dereferences missing XML dynamic properties under StrictMode.
- 353 files are present under `src/LocalGPT/wwwroot/images`.
- `TacosLogos.svg` and `Information.svg` are present at the exact runtime URLs used by the application.
- All image URLs referenced by maintained Razor/CSS sources resolve to files in `wwwroot`.
- `Directory.Build.targets` keeps the static-web-asset guard enabled; no guard was commented out or bypassed.
- Source package contains no `bin`, `obj`, DLL, EXE, or PDB build artifacts.

The first Windows build should generate the real 2.3.14 DocFX/PDF payload and update the pinned GitHub Pages snapshot from that build output.
