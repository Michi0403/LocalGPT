# LocalGPT 3.3.6 source validation

Static validation performed without running `dotnet` or `pwsh`:

- all three shipping project versions are 3.3.6;
- `docs/DocfxDependencies.csproj` exists and pins `System.Formats.Nrbf` 10.0.11 with `PrivateAssets="all"`;
- the LocalGPT application project does not gain a direct `System.Formats.Nrbf` package reference;
- build prerequisites require the documentation dependency project in clean source archives;
- the documentation pipeline restores the isolated dependency project only when the probe package is absent, stages the probe DLL before initial DocFX metadata extraction, and retains the generic repair pass;
- unresolved metadata references fail before PDF generation with the concrete assembly names;
- Node.js 20–22 cross-platform bootstrap and DevExpress license preflight from earlier releases remain wired into release/local-development builds;
- source documentation tree is present;
- existing `@rendermode InteractiveServer` declarations are unchanged from 3.3.5;
- ZIP packaging is files-only under a single top-level `LocalGPT-3.3.6/` directory and is checked for duplicate, case-folded, and Unicode-normalized path collisions.

A real .NET/PowerShell build remains intentionally unexecuted in this packaging environment and should be verified on the target developer machine with `pwsh ./Build-Release.ps1`.
