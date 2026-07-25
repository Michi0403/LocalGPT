# LocalGPT v0.1.2 — DXAIFunction and reviewed generation wiring

## Added

- Dynamic DI-backed DXAIFunction registry with per-function JSON parameter schemas.
- Native Ollama tool calling for explicitly automatic-safe read-only functions.
- Read-only functions for project metadata, change reviews, operational log summaries, approved knowledge summaries, and conversation metadata.
- Database-backed `CodeGenerationChangeReviews` and EF Core migration.
- Council heartbeat UI for reviewing, approving, rejecting, generating, downloading, and optionally building an exact proposal.
- Structured Council `<localgpt-change-review>` proposals containing exact files, CodeDOM types, and output targets.
- Generation outputs for source files, class libraries/DLLs, console applications/EXEs, solutions, disabled LocalGPT addon projects, C# scripts, and JavaScript modules.
- Structured operation logging across the registry, plan parser, workflow service, and Council integration.

## Changed

- DXAIFunction parameter metadata now lives with each DI handler instead of a hardcoded Ollama-client switch.
- Council generation creates a review and stops for a user decision rather than immediately producing/building an artifact.
- Reviewed C# source is copied into generated .NET output projects before an optional confirmed build.
- Reviewed `.csx` and `.js` files are used by script/module outputs when provided.
- Generic mutation handlers consume the envelope's fresh human confirmation consistently.
- Automatic invocation denials return HTTP 403 from the generic function controller.
- Repository knowledge seed version increased while retaining regex, prompt, variable, and approved knowledge seeding.

## Safety

- Automatic model calls remain read-only and bounded.
- Review creation, file generation, rejection, and builds are never automatically invoked.
- Source approval is one-use and bound to the exact SHA-256 review hash.
- Builds require a second current confirmation.
- Generated scripts, DLLs, executables, and addons are not executed or loaded automatically.
- All writes remain inside the LocalGPT Council artifact root.

## Validation limitation

The source was structurally validated in the cloud workspace. A licensed Windows/DevExpress build remains the authoritative compile and runtime check.
