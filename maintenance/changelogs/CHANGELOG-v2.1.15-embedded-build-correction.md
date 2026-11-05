# LocalGPT 2.1.15 — Embedded workbench build correction

## Fixed

- Replaced `EmbeddedHardwareCatalogService.GetProfileDirectories()` and `CreateFallbackProfiles()` iterator implementations with concrete read-only list results. This removes the four iterator-policy failures reported by the Windows build without weakening or baselining the policy.
- Replaced the malformed interpolated multiline raw strings in `EmbeddedFirmwarePlanningService` with explicit `StringBuilder` generation for:
  - Arduino/ESP32 `src/main.cpp`
  - `platformio.ini`
  - `WIRING.md`
- Preserved C++ JSON escaping, PlatformIO build-flag quoting, optional physical 1-Wire includes, generated pin declarations, telemetry calls and learning-round documentation.
- Advanced LocalGPT runtime, project package, seeded project history and organic-wire application advertisement to `2.1.15`.

## Unchanged boundaries

- Firmware planning and source-artifact creation remain separate from compilation, serial access, flashing and actuator execution.
- Artifact creation still requires fresh human approval and rejects danger-level board or wiring findings.
- Physical 1-Wire remains optional and distinct from LocalGPT's protected logical 1-Wire envelope.
- Workspace permission assessment and compiler execution guards remain in place.

## Validation performed in this package environment

- LocalGPT async-continuation audit passed for 133 source files.
- LocalGPT architecture audit passed.
- Iterator-policy logic reports no new unbaselined iterator findings.
- All JSON configuration files parse successfully.
- XML project and configuration files parse successfully.
- Changed C# files were scanned for unclosed strings/comments and balanced structural delimiters.
- The source archive was checked for merge markers and excluded build-output directories.

A real .NET 10/DevExpress Windows build is still the authoritative semantic compilation test because this packaging environment does not contain the required SDK and licensed UI dependencies.
