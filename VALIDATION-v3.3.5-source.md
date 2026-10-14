# LocalGPT 3.3.5 source validation

Static validation for the clean-source documentation package and DocFX dependency repair.

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are all version **3.3.5**.
- The source tree contains `docs/index.md`, the nine documentation files linked by `LocalGPT.csproj`, all maintained conceptual documentation pages, category/root TOCs, `docfx.json`, the complete-PDF TOC/cover and the Kawaii theme source assets required by `Build-Documentation.ps1`.
- `docfx.json` retains the modern template, nested namespace layout and same-page member layout expected by the documentation guards.
- `Initialize-BuildPrerequisites.ps1` fails immediately if required documentation source files are absent, before Node provisioning and the long build.
- The cross-platform Node.js 22.23.2 portable bootstrap from 3.3.4 remains present for Windows, macOS and Linux.
- `Build-Documentation.ps1` still materializes lock-file assemblies and retries unresolved DocFX metadata references.
- Unresolved DocFX references now additionally probe `NUGET_PACKAGES` and the per-user `.nuget/packages` cache, with `net10.0` assemblies preferred.
- No direct `System.Formats.Nrbf` application dependency was introduced.
- Release/local-development documentation guards still require zero unresolved assembly references.
- Existing `InteractiveServer` render-mode declarations remain untouched by this release.
- No .NET build/restore/test or PowerShell execution was run during preparation of this archive.

## Static checks executed during packaging

- `build/audit_release_3_3_5.py` passed.
- `build/audit_application_architecture.py --root src/LocalGPT --product localgpt --mode static` passed.
- `build/audit_async_continuations.py --source-root src/LocalGPT` passed across 258 source files.
- `build/audit_codegen_dxfunction_wiring.py` passed.
- The documentation source tree was checked for all project-linked and build-required files.
- `docfx.json` was parsed as JSON and all authored TOC YAML files were checked structurally.
- The Razor render-mode map was compared against the 3.3.4 source tree and remained unchanged.
- The final ZIP was checked for duplicate/case-colliding member names, a single top-level directory, required `docs/` entries and archive integrity.
