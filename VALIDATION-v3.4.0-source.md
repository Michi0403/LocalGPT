# LocalGPT 3.4.0 source validation

Static source validation only. No `dotnet` build/restore/test and no PowerShell script execution was performed in the packaging environment.

## Verified

- All three product projects declare version 3.4.0 and the single-digit minor/patch version policy is satisfied.
- `LocalGPT.csproj` no longer references `System.Drawing.Common`, `System.Data.OleDb`, `System.Diagnostics.PerformanceCounter`, `Microsoft.Windows.AI.MachineLearning`, or `System.Security.Cryptography.ProtectedData`.
- Maintained LocalGPT C# sources contain no `using System.Drawing;`, `System.Drawing.Bitmap`, or `System.Drawing.Graphics` backend usage.
- Common service OS branching is constrained to the application composition root and the dedicated platform implementation files.
- Windows and Unix implementations are registered for platform filesystem semantics, local console execution, hardware probing, and runtime secret-file protection.
- Council artifact/workspace containment, remote ZIP extraction/staging, documentation paths, and project-maintenance ancestor walking use injected host filesystem semantics instead of unconditional case-insensitive string-prefix checks.
- `build/audit_cross_platform_boundaries.py` passes and is invoked by both authoritative Release and LocalDevelopment PowerShell entry points before the long build/documentation stages.
- Existing architecture, async-continuation, and DXFunction wiring audits are run statically against the modified source tree.
- Blazor `@rendermode InteractiveServer` declarations are compared with the 3.3.9 source baseline and remain unchanged.
- Source ZIP is validated for archive integrity and Finder/APFS path collisions before handoff.
- Chromium browser-print arguments request `--export-tagged-pdf` and `--generate-pdf-document-outline`; browser-tagged PDFs are not passed through Ghostscript recompression.
- GitHub Pages PDF validation remains strict for renderers that promise tagging, but explicitly accepts the known DocFX 2.78.x Playwright untagged-PDF limitation only when `htmlPreflightValidated=true` and `pdfAccessibilityMode=html-accessibility-fallback`; deployment metadata still records the actual `taggedPdf` result.
