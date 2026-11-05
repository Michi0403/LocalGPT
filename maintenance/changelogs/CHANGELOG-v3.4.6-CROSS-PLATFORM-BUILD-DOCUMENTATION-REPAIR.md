# LocalGPT 3.4.6 — Cross-Platform Build and Documentation Runtime Repair

## Fixed

- Removed Windows-only gating from active MSBuild repository guards. `Directory.Build.targets` now selects Windows PowerShell on Windows and `pwsh` on macOS/Linux, while keeping every existing guard enabled.
- Made LocalGPT’s Python-backed architecture, async-continuation, and service-resilience guards resolve `python3` on macOS/Linux instead of silently missing the syntax-aware audit when only the standard Unix executable name is installed.
- Brought the new hardware/platform/console/runtime-secret adapter methods under the existing service-resilience contract with local try/catch diagnostics. No exemptions or guard relaxations were added.
- Replaced the newly introduced Ollama candidate iterators with ordinary collections so the existing iterator policy is satisfied without changing candidate order or executable discovery behavior.
- Fixed LocalGPT documentation browser discovery on Windows by initializing the per-user local application-data path used by Edge/Chrome probing.
- Restored the fast single-browser PDF lane for the current documentation size on macOS/Linux by retaining the reviewed 1,500-source-page limit used by the earlier working release path.
- Changed project builds so Debug continues to generate the complete HTML help site but does not force the heavyweight PDF. Release builds and `Build-Release.ps1` still require the complete versioned PDF.
- Reused any already-installed Node.js 20+ runtime, including newer supported installations, instead of provisioning a second Node.js copy only because it is newer than the preferred LTS major.
- Made redirected DocFX/Playwright progress compact and platform-neutral: carriage-return redraws no longer flood macOS/Linux terminals, mojibake block bars are not printed on Windows/MSBuild, and repeated unchanged PDF percentages are de-duplicated. Diagnostics and terminal failure text remain captured and visible.

## Preserved

- Complete documentation content, accessibility/link validation, PDF requirement for release packaging, PDF outline/tagging, application behavior, UI, InteractiveServer boundaries, persistence, packaging, and LocalGPT wire protocol version 2.1.1.
- No build guard was disabled, bypassed, or weakened.
