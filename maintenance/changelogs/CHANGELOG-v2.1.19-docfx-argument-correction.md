# LocalGPT 2.1.19 — DocFX Windows argument correction

## Corrected build behavior

The LocalGPT assembly and compiler XML file were already produced successfully in 2.1.18, but the subsequent documentation target passed the repository root as a quoted Windows path ending in `\`. Native Windows command-line parsing could interpret that final backslash together with the closing quote, causing the remaining `AssemblyPath`, `XmlDocumentationPath`, and `Version` arguments to be consumed as part of the first value. PowerShell therefore reported those mandatory parameters as missing.

The build target now passes the equivalent repository path with a terminal `\.` segment. It still resolves to the repository root but no quoted argument ends in a backslash. `Build-Documentation.ps1` immediately normalizes repository, assembly, XML, and optional output paths before using them and prints a bounded input summary for build diagnostics.

## Result

A normal Windows LocalGPT build can proceed from the completed assembly build into DocFX tool restore, metadata generation, HTML generation, and optional PDF generation. Debug builds attempt PDF generation without requiring it; Release builds continue to require the versioned PDF unless `RequireLocalGptDocumentationPdf=false` is supplied explicitly.

The generated PDF name is `LocalGPT-2.1.19.pdf`.

## Version alignment

- Application/package version: `2.1.19`
- Runtime `CustomVersion`: `2.1.19`
- Organic application advertisement: `2.1.19-organic-wire`
- Seed history retains every prior release and appends `seed-v2.1.19`
- `LocalGPT.WireProtocolVersion` remains independently versioned
