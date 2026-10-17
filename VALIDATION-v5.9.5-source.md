# LocalGPT 5.9.5 — Source-level validation

- Baseline: complete user-supplied LocalGPT 5.9.4 source archive; all original entries retained.
- Corrected both `InvalidVariableReferenceWithDrive` source occurrences in `Build-Documentation.ps1` (the error reported at lines 1007 and 1019).
- Early Visual Studio guard uses `System.Management.Automation.Language.Parser.ParseInput` on build/publish scripts and reports file/line/parser message. It does not change the normal repository's PowerShell-only architecture or require Python.
- The existing `Assert-PowerShellCompatibility.ps1` and `Directory.Build.targets` were not altered.
- All other Windows PowerShell qualified variable references (`$env:`, `$script:`, etc.) remain intact.
- Documentation cache lookup, manifest checks, PDF-verify, browser rendering, release packaging and vendored MIT PDFsharp code were not modified.
- Application version changed from 5.9.4 to 5.9.5. ReleasePackaging remains 1.1.0.
- Static checks: XML parse, source-file retention, targeted PowerShell source-shape assertions, archive CRC comparison and ZIP test.
- **Not executed:** Windows PowerShell, .NET compilation, DocFX, Edge printing, PDF merge, or application runtime tests.
