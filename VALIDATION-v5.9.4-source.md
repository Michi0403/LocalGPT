# LocalGPT 5.9.4 — static source validation

The following are **static** checks only: no .NET, PowerShell, DocFX, Windows Edge, browser print, or managed PDF verification command was executed in this environment.

1. Complete source ZIP checked against 5.9.3, retaining every original file.
2. `LocalGPT.csproj` changed from 5.9.3 to 5.9.4; release packaging and MIT PDFsharp vendor sources are unchanged.
3. The PowerShell documentation script still performs the original exact-content PDF cache check first.
4. Same-version fallback is restricted to a published PDF with matching version/filename, a DocFX status record, matching source-page count, PDF completeness markers, and `pdf-verify` exit code zero.
5. Validated same-version PDF is staged before the publishing loop deletes and recreates `help-docs` directories.
6. Explicit force bypasses both complete-PDF reuse branches and both cover/chunk cache reuse checks.
7. Existing post-reuse PDF structural verification and publication checks remain in force.
8. Project XML, ZIP entry integrity, and file-content retention are checked programmatically.

**Windows acceptance test:** Complete an initial version 5.9.4 build; rebuild without changing the version and confirm the line `Reused previously published LocalGPT 5.9.4 PDF` or `Reused validated cached LocalGPT PDF`, with no `Printing documentation PDF chunk` lines. For a controlled forced regeneration, set `FUTURE2_DOCUMENTATION_FORCE_PDF_REBUILD=1`, build, confirm newly printed cover and chunks, then unset it. If a previously printed same-version PDF lacks valid `documentation-status.json` or has different source-page count, regeneration remains correct behavior.
