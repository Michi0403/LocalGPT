# LocalGPT 3.3.4 source validation

Static validation for the cross-platform documentation runtime bootstrap.

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are all version **3.3.4**.
- `Build-Release.ps1` and `Build-LocalDevelopment.ps1` run `Initialize-BuildPrerequisites.ps1` before build work.
- The prerequisite bootstrap checks `dotnet`, performs the DevExpress license preflight, and resolves a compatible documentation Node.js runtime.
- `NodeRuntime.Common.ps1` maps Windows, macOS and Linux plus x64/ARM64 (and Windows x86) to official Node.js distribution names.
- Missing/incompatible Node.js is provisioned as portable Node.js 22.23.2 in a per-user cache and not installed system-wide; unsupported newer majors are not silently used as the fallback.
- Node.js archives are verified against the official release `SHASUMS256.txt` before extraction.
- macOS/Linux extraction uses `tar`; Windows extraction uses `Expand-Archive`.
- `PLAYWRIGHT_NODEJS_PATH` and process-local `PATH` are configured for DocFX/Playwright.
- `Build-Documentation.ps1` delegates Node resolution to the shared cross-platform runtime helper.
- macOS/Linux documentation sets above 1000 printable pages bypass direct browser printing and use the DocFX PDF plug-in directly; Windows retains the 1500-page threshold.
- `documentation-status.json` records Node version, provisioning state, platform and architecture.
- The 3.3.3 unresolved DocFX assembly-reference closure and zero-unresolved-reference guards remain present.
- No direct `System.Formats.Nrbf` application dependency was introduced.
- Existing `InteractiveServer` render-mode declarations remain untouched by this release.
- No .NET build/restore/test or PowerShell execution was run during preparation of this archive.

## Static checks executed during packaging

- `build/audit_release_3_3_4.py` passed.
- `build/audit_application_architecture.py --root src/LocalGPT --product localgpt --mode static` passed.
- `build/audit_async_continuations.py --source-root src/LocalGPT` passed across 258 source files.
- `build/audit_codegen_dxfunction_wiring.py` passed.
- The Razor render-mode map was compared against the 3.3.3 source tree and remained exactly unchanged at 20 directives.
