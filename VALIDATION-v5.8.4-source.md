# LocalGPT 5.8.4 — validation and Windows acceptance

## Static checks performed without dotnet or PowerShell

- Confirm XML project version is 5.8.4 and project XML remains well formed.
- Confirm the prior 5.8.3 source members are all preserved and the only modified existing members are the LocalGPT version, documentation build script, and its PowerShell compatibility gate.
- Verify the Windows Edge PDF output and profile path reductions using the exact old paths in the supplied build log: 269 → 228 and 253 → 220 characters.
- Verify the PowerShell gate requires the new short cache paths and still requires repository-owned build storage.
- Verify `Assert-LocalGptBrowserPdfPaths` runs before starting Edge, and checks for a preexisting output directory and Windows-specific path headroom.
- Check ZIP integrity.

## Windows acceptance (not executed here)

1. Build LocalGPT Debug. The PowerShell architecture validations should pass, and DocFX should again generate/validate HTML.
2. Confirm the `Printing complete documentation PDF cover/index` phase now produces `front-matter.pdf` quickly without Windows error `0x3`.
3. Confirm portrait body chunks render, the PDF merge passes page-count verification, and `/help` opens the final mixed-orientation PDF.
4. If a developer checks out the repository under a substantially longer prefix, the new preflight should report the problem immediately, suggesting the documented build-storage overrides rather than waiting 16+ minutes.
5. On macOS/WSL, check the PowerShell build and repository-owned caching remain functional. The extra Windows-only character limit must not apply there.

## Known limits

The diagnosis is strongly supported by the native Edge errors and path measurements, but a real Windows print run is still required to prove the fix in your environment. No full .NET build, Windows PowerShell invocation, or browser print was executed here.
