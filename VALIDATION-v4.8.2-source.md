# LocalGPT 4.8.2 source validation

LocalGPT 4.8.2 was validated as a source package only. In accordance with the owner's workflow, this environment did not run `dotnet`, MSBuild, NuGet restore, publish, installers, or GitHub access.

## Owner-reported 4.8.1 documentation finding addressed

- The normal IDE documentation target now receives the repository-owned `LocalGPT.ReleasePackaging` helper through a build-only project dependency and explicit `-PackagingTool` argument.
- Chunking is determined by handbook size rather than by accidental helper availability, preventing the old path where a large handbook could skip bounded browser rendering and report `Browser printing was unavailable` even though a browser was present.
- Windows browser discovery now covers Program Files, Program Files (x86), `ProgramW6432`, LocalAppData, `App Paths`, and PATH-based Edge/Chrome/Chromium/Brave installs.
- macOS `/Applications` and `~/Applications` Chrome/Edge/Chromium discovery remains present and Brave support was added without replacing existing probes.
- Linux/PATH Chromium-family discovery remains present.
- Default browser PDF parts are 8 pages through 16 GiB, 10 pages through 32 GiB, and 12 pages above that. The explicit environment override remains available.

## Maintenance-rule preservation

- `build/audit_application_architecture.py` was not weakened.
- `build/audit_service_resilience.py` was not weakened.
- `build/Assert-MethodDiagnostics.ps1`, `build/Assert-ApplicationStaticPolicy.ps1`, `build/Assert-TextServiceOwnership.ps1`, iterator/system-variable/EF/static-web/DevExpress guards remain enabled.
- `Directory.Build.targets` still executes the existing validation targets; its 4.8.2 change is limited to documentation helper wiring and passing the helper to the documentation script.
- The bounded browser/chunk policy still refuses an unsafe monolithic DocFX/Playwright fallback when chunking is required unless the operator explicitly enables the existing override.

## Static source checks executed

- Application architecture static audit: PASS.
- Broad service-resilience audit: PASS.
- Text-service ownership audit: PASS.
- LocalGPT 4.8.2 release-specific source audit: PASS.
- Versioned `.csproj` files parse as XML and expose `4.8.2`.
- Maintained JSON files parse, and `Runtime/Python/localgpt_runtime_bridge.py` parses with Python AST without importing project code.
- No `__pycache__`, `.pyc`, or `.pyo` files are included in the source tree.
- The three reported maintenance/build-policy repairs from 4.8.1 remain present.

## Runtime boundary

The owner's Windows/macOS .NET builds remain authoritative for compiler, browser invocation, PDF rendering and final runtime verification. This source validation does not claim a .NET build was performed here.
