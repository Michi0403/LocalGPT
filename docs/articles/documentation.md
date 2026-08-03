# Generated documentation and XML comments

LocalGPT documentation is built from two maintained sources:

1. Every maintained Markdown document under `docs`, organized through `guide/toc.yml`.
2. Compiler-generated `LocalGPT.xml` comments for public services, controllers, business contracts, and capabilities.

DocFX reflects `LocalGPT.dll` with the side-by-side XML file and creates the API YAML graph under `docs/api`. The root `toc.yml` is intentionally navbar-only. `guide/toc.yml` provides the Microsoft Learn-style conceptual sidebar, while the generated `api/toc.yml` provides the nested namespace, type, and member sidebar.

## Build outputs

A Windows LocalGPT build restores the repository-local DocFX tool, extracts metadata, and builds the complete modern DocFX website. The PDF pipeline then assembles every generated conceptual and API HTML page into one print document and prints that document with an installed Microsoft Edge, Google Chrome, or Chromium browser. This makes the versioned PDF contain the same maintained LocalGPT and XML-generated API information as the working HTML site instead of accepting an empty DocFX PDF shell.

The repository keeps DocFX's own PDF plug-in as a secondary compatibility route. Both routes must produce a real, sufficiently sized PDF; the retired one-page fallback index is never accepted.

The running application exposes:

- `/help-docs/index.html` for the complete generated website;
- `/help-docs/api/index.html` for the generated API landing page;
- `/api/documentation/status` for artifact, renderer, source-page, and API-page counts;
- `/api/documentation/comments` for bounded XML-comment search;
- `/api/documentation/comment?memberId=...` for one stable compiler member ID;
- `/api/documentation/pdf` for the current versioned PDF.

## Required toolchain

HTML and API generation require the .NET SDK and the repository-local DocFX tool. Complete PDF generation normally uses the Microsoft Edge installation included with supported Windows systems. Google Chrome or Chromium can be selected through `LOCALGPT_DOCUMENTATION_BROWSER`. Node.js 20 or later is only required when the secondary DocFX PDF route must be used.

Set `BuildLocalGptDocumentation=false` only for an explicit infrastructure diagnosis. A normal LocalGPT build requires the complete HTML-backed PDF. `RequireLocalGptDocumentationPdf=false` is reserved for an explicit HTML-only diagnosis and does not create or accept a one-page fallback PDF.

## Verification

The build validates that metadata produced `api/toc.yml`, that API HTML pages were rendered, that the exact versioned PDF exists, and that `documentation-status.json` reports `documentationMode: docfx`, an accepted PDF mode, a meaningful HTML source-page count for browser printing, and nonzero API counts. This prevents a successful application build from silently delivering an empty API section or a PDF without LocalGPT content.
