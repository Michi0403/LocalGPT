# Generated documentation and XML comments

LocalGPT documentation is built from two maintained sources:

1. Markdown articles under `docs/articles` for workflows and architecture.
2. Compiler-generated `LocalGPT.xml` comments for public services, controllers, business contracts and capabilities.

`DocumentationUpdatedAttribute` records the LocalGPT version in which a documented contract was last reviewed. `DocumentationTranslationAdapter` asks the existing localization service for an optional translated display name, summary and remarks while retaining the original XML text as fallback.

## Build outputs

A Windows LocalGPT build restores the repository-local DocFX tool, generates API metadata and HTML, and then attempts PDF generation. The PDF name contains the application version, for example `LocalGPT-2.1.20.pdf`.

The running application exposes:

- `/help-docs/index.html` for generated HTML;
- `/api/documentation/status` for artifact status;
- `/api/documentation/comments` for bounded XML-comment search;
- `/api/documentation/comment?memberId=...` for one stable compiler member id;
- `/api/documentation/pdf` for the current versioned PDF.

Release builds require the PDF by default. Set `RequireLocalGptDocumentationPdf=false` only for a deliberate diagnostic build. Set `BuildLocalGptDocumentation=false` to bypass the complete documentation target while diagnosing build infrastructure.
