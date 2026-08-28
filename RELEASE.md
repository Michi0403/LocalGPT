# LocalGPT 3.4.6

LocalGPT 3.4.6 is the **Cross-Platform Build and Documentation Runtime Repair** release.

This patch repairs the exact build-policy and documentation-runtime failures reproduced from the 3.4.5 Windows/macOS logs. Repository guards remain enabled and now execute on Windows, macOS, and Linux through the appropriate PowerShell host. The new platform-adapter methods satisfy the existing service/iterator resilience rules without exemptions. Debug builds keep generated HTML help but do not force the heavyweight PDF; the authoritative release path still requires the complete versioned PDF once. Documentation tooling reuses an existing Node.js 20+ runtime, the single-browser PDF path again works on Windows and supports the full current manual size on macOS/Linux, and redirected DocFX progress is compact and de-duplicated without hiding failures.

Application features, UI behavior, InteractiveServer boundaries, persistence, wire protocol 2.1.1, documentation content, and release packaging contracts are unchanged. This handoff is source-only; no .NET build and no GitHub access were used while preparing it. See `CHANGELOG-v3.4.6-CROSS-PLATFORM-BUILD-DOCUMENTATION-REPAIR.md` and `VALIDATION-v3.4.6-source.md`.
