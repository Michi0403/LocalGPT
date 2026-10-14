# LocalGPT 2.9.0 source validation

This is a source-only compile repair. No `dotnet`, MSBuild, Visual Studio build, or GitHub access was used during preparation. The user's Windows .NET build remains authoritative.

Static validation covers:

- LocalGPT application/wrapper/installer version `2.9.0` and single-digit minor/patch slots.
- The invalid `composerDraft = await InvokeAsync(...)` result assignment is absent.
- Composer draft capture occurs inside the renderer-dispatched callback and retains renderer-affine JS continuation handling.
- The 2.8.9 single-bind Council transcript rejoin/circuit-recovery path remains present.
- Existing 2.8.8 role coordination remains opt-in and intact.
- Existing strict-async, Council, trace, architecture, resilience, 1-Wire and XML documentation audits are rerun.
- LocalGPT `@rendermode` directives remain unchanged from the 2.8.9 source baseline.
