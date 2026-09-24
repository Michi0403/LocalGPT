# LocalGPT 4.9.0

LocalGPT 4.9.0 is a release-orchestration dependency-order repair on top of 4.8.9.

The macOS release coordinator cleans every repository-local `bin`/`obj` directory before the authoritative build. The RID-neutral documentation compile previously restored the LocalGPT project graph but then invoked the LocalGPT build with `BuildProjectReferences=false`. After 4.8.6 introduced `LocalGPT.PluginContracts`, that could leave the freshly cleaned contract reference assembly absent and fail with `RAZORSDK1007` / `CS0006` before documentation generation.

4.9.0 keeps project-reference builds enabled for the RID-neutral documentation compile while still disabling recursive LocalGPT documentation generation. This lets MSBuild build `LocalGPT.PluginContracts` and the source-backed LocalGPT wire-protocol project in normal dependency order, and it automatically covers future project references without adding release-script special cases.

The 4.8.9 toolchain autodiscovery, auto-validation, approval simplification, responsive layout and runtime-plugin behavior are otherwise unchanged.

This is a source-only handoff. No .NET/MSBuild/NuGet build, restore, publish, native packaging, signing, notarization or installer execution was performed in the handoff environment. The user's macOS 4.8.9 `Build-Release.ps1` output is the authoritative evidence for this repair. See `CHANGELOG-v4.9.0-RELEASE-PROJECT-REFERENCE-ORDER.md`.
