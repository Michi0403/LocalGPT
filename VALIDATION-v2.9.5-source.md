# LocalGPT 2.9.5 source validation

This release is validated source-only in the packaging environment. No `dotnet`, MSBuild or Visual Studio build is executed here; the Windows build remains authoritative.

Targeted validation covers:

- LocalGPT application/project versions at 2.9.5 while `LocalGptWireProtocolVersion` remains 2.1.1.
- Team-level all-members preflight modes: legacy compatibility, disabled and explicit role-aware probe.
- Backward compatibility: legacy built-in orchestration retains its historical readiness phase and literal custom workflows retain their historical no-extra-preflight behavior.
- Role-aware preflight prompts are bounded, do not execute the original user request, do not call DX/organic functions and use actual run role assignments.
- Explicit preflight output remains visible/persisted but is filtered from later workflow transcript context unless the team explicitly opts in.
- Initial Hardware Calibration Benchmark explicitly disables all-members readiness preflight and retains the 2.9.4 role-task benchmark structure.
- Seed version 23 and supplied-default preservation/custom-copy behavior.
- Existing Council role coordination, provider-qualified addressing, X-Rounds, reasoning/function trace, rejoin, async, resilience, architecture, documentation and InteractiveServer invariants.
