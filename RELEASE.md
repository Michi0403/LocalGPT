# LocalGPT 3.5.1

LocalGPT 3.5.1 is the **Compiler Follow-up and Shared Release-Packaging** maintenance release.

It carries the user-confirmed localization namespace fix, repairs the remaining runtime-policy compile regressions in Theme Fusion, chat rendering, and Human Collaboration, and makes the `LocalGPT.ReleasePackaging` tool a first-class LocalGPT release asset/cache package alongside the authoritative 1-Wire NuGet package.

The Windows/macOS/Linux release matrix and existing `InteractiveServer` boundaries remain intact. This source handoff is statically validated in this environment; the user's Windows .NET build remains the authoritative compile/runtime check. See `CHANGELOG-v3.5.1-COMPILER-SHARED-PACKAGING-FOLLOWUP.md` and `VALIDATION-v3.5.1-source.md`.
