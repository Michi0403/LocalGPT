# LocalGPT 2.1.23

## Release and documentation

- Keeps documentation enabled during normal builds and publishes it without making a valid application build depend on DocFX or Node.js.
- Attempts repository-local DocFX metadata, HTML and PDF generation first.
- Generates deterministic static HTML/API documentation from `docs/articles` and `LocalGPT.xml` when DocFX metadata or HTML generation fails.
- Generates `LocalGPT-2.1.23.pdf` through DocFX when possible and otherwise creates a small dependency-free PDF documentation index.
- Treats an unexpected documentation-script exit as a build warning rather than a RID publish failure.
- Copies the generated source documentation tree into `$(PublishDir)wwwroot/help-docs` after RID publishing.
- Removes Release-only unused-exception warnings in `DxAiFunctionRegistry` and `CodeGenerationWorkflowService` without removing cancellation diagnostics.

## Chat and Council starters

- Preserves all normal DxAIChat quick prompts.
- Adds team-owned Council starters without replacing normal suggestions.
- Selects the requested Council team and Council chat session from route parameters.
- Avoids racing `DxAIChat.LoadMessages` while the DevExpress provider is changing.
- Submits the full starter prompt through the actual composer/send path; a highlighted suggestion no longer counts as a successful Council start.
- Retries route-driven starts while the interactive renderer, model discovery or Council session is still initializing.
- Sizes Running session tools to its real content and limits internal scrolling instead of reserving an empty viewport-sized body.

## Toolchain setup

- Adds installer discovery for .NET SDK, MSBuild, Java compiler/runtime, Python, PowerShell, C/C++, PlatformIO and Arduino CLI.
- Searches `PATH`, common platform locations and explicit user roots.
- Supports bounded version validation, language-default selection, stored environment metadata and deletion when no workspace/build evidence references a compiler.
- Reuses the existing `ProjectCompilerInstallation` EF entity and project-maintenance controller/service boundaries.

## Durable recent-feature records

Adds migration `20260803000000_AddFeaturePersistenceRecords` and complete EF Core mappings for:

- `CouncilPromptStarterConfiguration`
- `LocalizationCatalogRegistration`
- `DocumentationBuildRecord`
- `EmbeddedFirmwarePlanRecord`
- `CouncilGameSessionRecord`

Each aggregate has:

- a DbSet and fluent mapping;
- indexes and bounded string fields;
- relevant project/conversation foreign keys;
- list and single-record reads;
- approval-gated create/update and delete methods;
- service-level validation, JSON validation, logging and exception handling;
- controller-level bounded logging and safe HTTP errors.

Request DTOs, calculated snapshots, renderer models and runtime-only actor predictions remain transient by design rather than being incorrectly treated as database entities.

## Validation boundary

The source package was checked with the repository Python async and architecture audits, JSON/XML parsing, JavaScript syntax checking, lexical C#/Razor structure checks, version alignment and ZIP integrity. A real .NET 10, EF Core migration, DevExpress, Windows PowerShell, DocFX and RID publish must be confirmed on Windows.
