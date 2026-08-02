# Generated documentation and XML comments

LocalGPT documentation is built from two maintained sources:

1. Markdown articles under `docs/articles` for workflows and architecture.
2. Compiler-generated `LocalGPT.xml` comments for public services, controllers, business contracts and capabilities.

`DocumentationUpdatedAttribute` records the LocalGPT version in which a documented contract was last reviewed. `DocumentationTranslationAdapter` asks the existing localization service for an optional translated display name, summary and remarks while retaining the original XML text as fallback.

## Build outputs

A Windows LocalGPT build restores the repository-local DocFX tool, generates API metadata and HTML, and then attempts PDF generation. The PDF name contains the application version, for example `LocalGPT-2.1.23.pdf`.

The running application exposes:

- `/help-docs/index.html` for generated HTML;
- `/api/documentation/status` for artifact status;
- `/api/documentation/comments` for bounded XML-comment search;
- `/api/documentation/comment?memberId=...` for one stable compiler member id;
- `/api/documentation/pdf` for the current versioned PDF.

Normal and Release builds keep documentation enabled, but a valid application compile or RID publish is not invalidated by DocFX or Node.js failure. LocalGPT publishes deterministic HTML and a dependency-free versioned PDF index when the external toolchain cannot produce them. Set `RequireLocalGptDocumentationPdf=true` only when a CI policy must reject a build that cannot produce either the DocFX PDF or the fallback PDF. Set `BuildLocalGptDocumentation=false` only to bypass the complete documentation target while diagnosing build infrastructure.

## Metadata and release fallback in 2.1.23

The documentation stage first gives DocFX access to the complete application output assembly set. When metadata extraction or the DocFX site build still fails, LocalGPT generates a searchable static site directly from `LocalGPT.xml` and maintained Markdown articles. It also writes a small valid versioned PDF index without Node.js. The source help tree is copied into every RID publish after `Publish`, while unexpected documentation-script failures are emitted as warnings instead of converting a successful application compile into a failed release.
