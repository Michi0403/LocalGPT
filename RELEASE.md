# LocalGPT 3.0.1 namespace, wiring, structure and live-rejoin repair

LocalGPT 3.0.1 is a stabilization release over 3.0.0. It keeps the 3.0.0 EF startup migration repair intact while correcting namespace/documentation ownership, live Council browser rejoin, text/Regex service boundaries, and several oversized/mixed-responsibility source areas.

Key changes:

- Lightweight marker-only live Council rejoin while a run is active, with full transcript persistence after completion.
- Correct `LocalGPT.Controller`, `LocalGPT.Services`, and `LocalGPT.Hubs` namespace/folder ownership; stale TacosPortal/Endpoints documentation rewrites removed.
- Zero-baseline Razor/controller text/Regex ownership with internal extensions reachable only through DI services.
- Dedicated Regex, JSON text, reviewer policy, Minecraft project, Minecraft datapack and Council knowledge-content services.
- Responsibility-named partials/code-behind for the remaining genuinely large types while preserving public DI/runtime contracts.
- New 3.0.1 regression gate plus partial-aware historical release gates.

Versions:

- LocalGPT: 3.0.1
- LocalGPTWebviewWrapper: 3.0.1
- LocalGPTInstallerConsole: 3.0.1
- LocalGPT Wire Protocol: 2.1.1

See `CHANGELOG-v3.0.1-source.md` and `VALIDATION-v3.0.1-source.md` for the detailed source-only validation record.

This source package was not compiled in the repair environment. No GitHub or .NET/MSBuild invocation was used.
