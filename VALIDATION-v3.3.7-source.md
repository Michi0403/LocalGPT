# LocalGPT 3.3.7 source validation

Static validation performed without running `dotnet` or `pwsh`:

- all three shipping project versions are 3.3.7;
- the reported `Build-Documentation.ps1` interpolation is `${restoreExitCode}:` and no longer contains the invalid `$restoreExitCode:` form;
- `Assert-PowerShellCompatibility.ps1` now invokes `System.Management.Automation.Language.Parser::ParseInput` for every maintained PowerShell script and reports parser errors with file/line information;
- no other non-scope `$name:` token was found in maintained `.ps1`/`.psm1` sources;
- `docs/DocfxDependencies.csproj` still pins `System.Formats.Nrbf` 10.0.11 with `PrivateAssets="all"`, while the LocalGPT application project itself has no direct `System.Formats.Nrbf` package reference;
- Node.js 20–22 cross-platform bootstrap, DevExpress license preflight, documentation source preflight, and generic DocFX unresolved-reference repair remain wired;
- existing `@rendermode InteractiveServer` declarations are unchanged from 3.3.6;
- source archive packaging follows the previously working root-layout ZIP structure and is checked for duplicate, case-folded, and Unicode-normalized path collisions.

A real .NET/PowerShell build remains intentionally unexecuted in this packaging environment and should be verified on the target developer machine with `pwsh ./Build-Release.ps1`.
