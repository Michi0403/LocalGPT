# LocalGPT 4.3.9 source validation

## Scope

This is a source-only handoff. The requested constraint was observed: no `dotnet` restore/build/test/publish and no GitHub/online repository access were used.

## Source-side checks

- `build/audit_application_architecture.py --root . --product localgpt --mode all`
- `build/audit_service_resilience.py --root . --product localgpt`
- `build/audit_async_continuations.py --source-root src/LocalGPT`
- `build/Assert-XmlDocumentationCoverage.py src/LocalGPT`
- direct Python equivalent of `build/Assert-TextServiceOwnership.ps1` against `build/text-service-ownership-baseline.json`
- `build/audit_release_4_3_9.py`
- source ZIP CRC/readback and extraction verification

## Architecture assertions

- The Project system owns `Game` authoring and persists the compiled `ProjectGameDefinition` through the existing approved project-artifact service.
- `CouncilGameSessionService` consumes a built definition but does not depend on the project service or perform project authoring/persistence.
- Runtime behavior for project builds is selected by `CouncilGameRuntimeProfile`; custom project game keys are not treated as engine-profile switches.
- Existing built-in corridor/Green Dragon launch keys remain supported.
- Operator command parsing remains service-owned; the Razor game console receives a typed `GameStartProject` action and does not parse project selectors.
- Project-game selector matching/build normalization is service-owned.
- No maintenance/build guard was disabled, weakened, or given a new exemption for this release.

Compiler/runtime behavior still requires the user's normal .NET build and runtime validation after handoff.
