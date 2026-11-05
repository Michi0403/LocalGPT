# LocalGPT 5.9.5 — Windows PowerShell PDF documentation parsing repair

## Cause verified from the user's Visual Studio build

The 5.9.4 documentation build failed immediately after the application compiled, before cache lookup, DocFX, or Edge printing. The Windows PowerShell 5.1 parser rejected two interpolations in `build/Build-Documentation.ps1`:

- Line 1007: `"Skipping $PdfName: ..."`
- Line 1019: `"Skipping published $PdfName: ..."`

In expandable strings PowerShell parses the colon after an unbraced variable name as part of a namespace/drive-qualified variable reference, which is invalid here (`InvalidVariableReferenceWithDrive`). Neither message needs to be executed for the script to fail: the entire file must parse before it runs.

## Repair

1. Changed both diagnostics to use `${PdfName}:` without changing their semantics or the PDF publication/reuse logic.
2. Extended the existing `build/Assert-RepositoryPowerShellBoundary.ps1`, which Visual Studio already invokes as its first LocalGPT build guard, to parse all of its 80 build/publish scripts using `System.Management.Automation.Language.Parser.ParseInput`. Parser failures now give the source file, line and message **before** compilation or lengthy documentation work.
3. Kept `build/Assert-PowerShellCompatibility.ps1` unchanged. It already performs comprehensive parsing for builds entering through `Build-LocalDevelopment.ps1`, but the direct Visual Studio target did not execute that independent preflight.
4. Updated the LocalGPT package/application version to 5.9.5. The ReleasePackaging tool, vendored PDFsharp and PublisherStudio are unchanged.

## Reuse behavior and limitations

Because the version changed, the first 5.9.5 handbook is generated afresh. Later same-version builds can use the existing 5.9.4 repair's validated PDF reuse. A forced rebuild is available through `FUTURE2_DOCUMENTATION_FORCE_PDF_REBUILD=1`.

## Verification

Static integrity checks and the exact previously failing interpolation patterns were checked. No Windows PowerShell interpreter, .NET SDK, Edge renderer or application build is available or was run here. Windows compilation and PDF rendering remain to be verified.
