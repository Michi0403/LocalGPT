# Generated documentation and XML comments

LocalGPT documentation is built from two maintained sources:

1. Every maintained Markdown document under `docs`, organized through `guide/toc.yml`.
2. Compiler-generated `LocalGPT.xml` comments for public services, controllers, business contracts, and capabilities.

DocFX reflects `LocalGPT.dll` with the side-by-side XML file and creates the API YAML graph under `docs/api`. The root `toc.yml` is intentionally navbar-only. `guide/toc.yml` provides the Microsoft Learn-style conceptual sidebar, while the generated `api/toc.yml` provides the nested namespace/type/member sidebar.

## Build outputs

A Windows LocalGPT build restores the repository-local DocFX tool, extracts metadata, builds the modern DocFX website, and generates one complete PDF from the dedicated `pdf/toc.yml`. That PDF TOC nests both `guide/toc.yml` and the generated `api/toc.yml`. The output uses the built-in `default` plus `modern` templates with small Microsoft Learn-style typography and spacing overrides.

The running application exposes:

- `/help-docs/index.html` for the complete generated website;
- `/help-docs/api/index.html` for the generated API landing page;
- `/api/documentation/status` for artifact and API-page counts;
- `/api/documentation/comments` for bounded XML-comment search;
- `/api/documentation/comment?memberId=...` for one stable compiler member ID;
- `/api/documentation/pdf` for the current versioned PDF.

## Required toolchain

HTML and API generation require the .NET SDK and the repository-local DocFX tool. Complete PDF generation additionally requires Node.js 20 or later. Normal development and release commands require the DocFX site and DocFX PDF by default; they no longer accept a one-page fallback index as a successful documentation payload.

Set `BuildLocalGptDocumentation=false` only for an explicit infrastructure diagnosis. A normal LocalGPT build requires the complete DocFX PDF. `RequireLocalGptDocumentationPdf=false` is reserved for an explicit HTML-only diagnosis and does not create or accept a one-page fallback PDF.

## Verification

The build validates that metadata produced `api/toc.yml`, that API HTML pages were rendered, that the exact versioned PDF exists, and that `documentation-status.json` reports `documentationMode: docfx`, `pdfMode: docfx`, and nonzero API counts. This prevents a successful application build from silently delivering an empty API section or a PDF that only points at the XML file.
