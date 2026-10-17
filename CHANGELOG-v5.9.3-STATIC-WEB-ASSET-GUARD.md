# LocalGPT 5.9.3 — Windows PowerShell static-web-asset guard repair

## Failure reproduced from Visual Studio build evidence

The .NET 10 dependencies compiled, and all preceding PowerShell architecture checks succeeded. `build/Assert-StaticWebAssets.ps1` then terminated at its success message (line 79) under Windows PowerShell 5.1 with `PropertyNotFoundStrict` while evaluating `$required.Count`.

Root cause: `$required` originally held a case-insensitive `HashSet[string]` of maintained web assets. The Council popup CSS regression check added in 5.9.2 reused `$required` as the `foreach` variable for a sequence of CSS contract strings. PowerShell does not create a separate loop-variable scope; after iteration `$required` was the final CSS string, not the set. Access to its nonexistent `.Count` property fails with `Set-StrictMode -Version Latest`.

## Changes

- Renamed the maintained-asset collection to `$requiredAssets` throughout the script.
- Renamed the CSS contract loop variable to `$requiredCssContract`, including its contains check and error message.
- Kept asset path enumeration, explicit required paths, missing-file failures, popup CSS contract checks, `Set-StrictMode`, and PowerShell-only execution unchanged.
- Updated the LocalGPT project version to 5.9.3. No PDFsharp, PublisherStudio, Council routing, or runtime changes.

The script reports the actual validated asset collection count instead of trying to read a property from the last CSS selector string.
