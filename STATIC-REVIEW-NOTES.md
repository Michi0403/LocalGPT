# Static review notes

This source package is based on `LocalGPTCleanedByCorruption.zip`.

Restored/reworked:

- Council DX function request parsing, policy data, orchestration, DI wiring, and ordered result steps.
- 1-Wire capability-provider registration and catalog implementation.
- Readable gateway failures that preserve the Council run instead of aborting it.
- Practical, respectful bootstrap and repository guidance without protected/immutable governance files.
- Explicit build behavior with no automatic repository-policy scripts in `Directory.Build.targets`.
- Source package cleanup: `.git`, `.claude`, IDE state, user files, logs, and build output are excluded.

Validation limitation: reviewed statically only. No .NET SDK/runtime or PowerShell execution was available or used. XML/JSON structure, source references, archive paths, and source-level syntax heuristics were checked before packaging.
