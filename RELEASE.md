# LocalGPT 3.0.2 Windows build-guard, compile and DXFunction wiring repair

LocalGPT 3.0.2 is a narrow repair over 3.0.1. It keeps the namespace/service/structure/live-rejoin work intact while fixing Windows build guards that still assumed single-file `Program`/Razor/service implementations and one real controller compile error revealed by the user's Windows build.

Key changes:

- Partial-aware operational diagnostics and InteractiveServer Windows guards.
- Partial-owner normalization for iterator and system-variable historical baselines without weakening new-violation enforcement.
- `StructuredTextController` compile import repair.
- Minecraft project/datapack controller wiring verified and two read-only Minecraft DXAIFunctions added to automatic handler discovery/system-seed catalog synchronization.
- Internal extracted services remain behind their owning API/DXFunction boundaries rather than gaining duplicate controllers.

Versions:

- LocalGPT: 3.0.2
- LocalGPTWebviewWrapper: 3.0.2
- LocalGPTInstallerConsole: 3.0.2
- LocalGPT Wire Protocol: 2.1.1

See `CHANGELOG-v3.0.2-source.md` and `VALIDATION-v3.0.2-source.md`.

This source package was not compiled in the repair environment. No GitHub or .NET/MSBuild invocation was used.
