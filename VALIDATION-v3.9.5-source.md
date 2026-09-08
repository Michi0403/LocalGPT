# LocalGPT 3.9.5 source validation

- Reproduced failure contract from the supplied Windows Debug log: C# compilation, DocFX metadata/site generation, and HTML accessibility/local-link preflight all completed successfully, then the documentation script rejected the intentionally PDF-less Debug runtime help tree.
- `Directory.Build.targets` still sets `RequireLocalGptDocumentationPdf=true` only for Release by default and passes `-RequirePdf` only in that case.
- `Build-Documentation.ps1` now gates the final embedded-PDF assertion with `if ($RequirePdf -or $pdfGenerated)`, matching the MSBuild contract.
- Release behavior is preserved: when PDF is required/generated, the runtime help tree must exist, the versioned PDF must exist, the sane-size ceiling remains enforced, and documentation status is updated with the actual PDF byte count.
- Debug behavior is preserved: generated HTML/API/XML/status artifacts remain required while the PDF link is disabled and `pdfAvailable/runtimePdfPublished` remain false when no PDF was requested.
- The fix is expressed entirely with PowerShell 5.1-compatible constructs and contains no OS-specific path syntax; Windows, macOS, and Linux share the same branch.
- Existing 3.9.3 `/install`, CanIRun.ai, Ollama, packaged runtime/logger, and release source-fingerprint repairs remain present.
- Existing 3.9.4 Windows PowerShell 5.1 `Join-Path` compatibility repair remains present.
