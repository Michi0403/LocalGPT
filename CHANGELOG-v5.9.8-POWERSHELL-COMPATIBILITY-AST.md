# LocalGPT 5.9.8 — macOS PowerShell compatibility guard repair

## Reproduced release failure

On macOS, `pwsh Build-Release.ps1` stopped in the initial
`build/Assert-PowerShellCompatibility.ps1` check. The compatibility validator
reported that line 56 of `build/Assert-RepositoryPowerShellBoundary.ps1` passed
a backslash-delimited path to `Join-Path`.

The quoted text on that line is **not an argument to `Join-Path`**. It is a
regular expression that recognizes calls to `Join-Path` in other scripts. The
old validator searched all quotes on any source line containing the text
`Join-Path`, so regular-expression escapes such as `\s` and `\(` were falsely
classified as filesystem separators.

## Repair

- Moved the `Join-Path` literal-path check into the already existing
  `CommandAst` inspection, using actual parsed literal arguments of real
  `Join-Path` commands. A diagnostic string, comment, or regular-expression
  source string no longer masquerades as a filesystem path merely because it
  mentions `Join-Path`.
- Kept the cross-platform ban on genuine backslash-delimited path arguments.
- Retained the existing Windows PowerShell 5.1 two-positional-argument check,
  full repository-relative path literal audit, parser error checks, release
  preflights, PowerShell-only boundary, and all other rules.
- Advanced LocalGPT's application version to `5.9.8` without changing
  domain services, PDFsharp, Council execution, or PublisherStudio.

## Validation and release acceptance

The 5.9.7 validator's false positive was reproduced statically using its exact
line-scanning logic. The replacement uses only PowerShell AST types already
used by the same script. Checked source preservation, PowerShell contract
snippets, `LocalGPT.csproj`, and the complete source ZIP. No PowerShell, .NET,
macOS publish, or PDF generation was executed in this environment.

On macOS, run `pwsh ./build/Assert-PowerShellCompatibility.ps1`, followed by
`pwsh ./Build-Release.ps1`. For a fast retry without changing version, copy
only the updated `build/Assert-PowerShellCompatibility.ps1` into the existing
5.9.7 checkout and run the two commands there.
