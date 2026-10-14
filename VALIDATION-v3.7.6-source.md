# LocalGPT 3.7.6 source validation

This source validation specifically guards the reported release regression where successful adaptive documentation generation ended with `PDF mode: html-browser-chunked` and was then rejected by `Build-Release.ps1`.

Source checks completed in this environment:

- version policy and current-version references: passed for 3.7.6; minor/patch remain single digits;
- current release audit: passed;
- `Build-Release.ps1`: accepts `html-browser-chunked` and still validates browser-backed source-page/API completeness and PDF accessibility policy;
- `Build-LocalDevelopment.ps1`: accepts the same supported browser-backed modes and applies the same page/API completeness checks;
- `build/Build-Documentation.ps1`: post-processing recognizes chunked browser PDFs as browser-backed for accessibility fallback semantics;
- InteractiveServer source audit: all routed application pages are guarded as `@rendermode InteractiveServer`, with only `Error.razor` retained as the intentional static fatal-error fallback;
- historical adaptive PDF, release packaging, source ownership, size ceiling, resume/notarization and architecture guards remain present.

No .NET or PowerShell runtime is available in this environment, so no `dotnet` build/publish and no `pwsh Build-Release.ps1` execution is claimed. The source-level regression audits are the available verification here.
