# LocalGPT 5.7.8 static validation and owner-side checks

## User-reported build error

On LocalGPT 5.7.7, the three vendored PDFsharp assemblies, ReleasePackaging, and LocalGPT compiled successfully. DocFX built 1,603 HTML files; Edge rendered 134 PDF pieces, and PDFsharp merged them. The documentation stage rejected its 280,373,424-byte PDF against the fixed 268,435,456-byte (256 MiB) limit. There was no licensing or C# compilation error in that log.

## Static checks performed without .NET

- Verified XML parsing of main and vendored project declarations.
- Reviewed the local .NET 10 PDFsharp compression/default writer options and the import-stream compressor guard.
- Confirmed that filtered PDF streams, decode-parameter streams, and streams for which Flate would grow the bytes are left untouched.
- Checked the page-adjusted size policy and unchanged explicit operator override. At 1,596 source pages its default hard ceiling is 373,030,912 bytes (about 356 MiB), above the reported 280,373,424-byte PDF, but still below the absolute 384 MiB cap.
- Retained PowerShell-based documentation generation, durable PDF chunks, cache reuse, optional native optimizers, and the PDFsharp 6.2.4 MIT license.
- Compared against every file in the 5.7.7 source archive and verified that only eight intended existing files changed; no original files were deleted.
- Verified source ZIP contents and CRCs.

## Required owner-side validation

1. Build LocalGPT in Visual Studio using Debug. Verify the local `PdfSharp.Shared`, `PdfSharp.System`, `PdfSharp`, `LocalGPT.ReleasePackaging`, and `LocalGPT` projects compile.
2. Confirm output reports `LocalGPT 5.7.8` and no `PdfSharp.License` exception.
3. Confirm DocFX HTML builds, PDF chunk merge completes, and final documentation PDF passes its size policy without requiring Ghostscript/qpdf.
4. Open the PDF from `/help`, confirm the beginning, middle, and end pages render and that fonts, embedded links, and images remain readable.
5. Verify Release packaging embeds the updated PDF and the third-party notices.
6. If there is still a failure, share the resulting `pdf-optimize` diagnostics and merged PDF byte count. No actual .NET build or end-to-end merge has been run in this environment.

## Residual risk

Managed lossless compression may or may not reduce the file substantially: most input streams may already be compressed by Edge. The page-adjusted upper bound is an intentional independent fix for the incorrectly fixed 256 MiB policy. It does not prove that the PDF is smaller or that an unbuilt project compiles.
