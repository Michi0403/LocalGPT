# LocalGPT 3.3.8 source validation

Static validation performed without running `dotnet` or `pwsh`:

- all three shipping project versions are 3.3.8;
- `docs/reference/index.md` links to the authored `../api/index.md` source instead of pre-emptively linking to generated `api/index.html`;
- `docs/docfx.json` treats `LocalGPT-*.pdf` as a resource, allowing the existing build-time PDF link stub to satisfy DocFX link validation before the final PDF is rendered;
- `Invoke-LocalGptDocfx` streams native DocFX output live while retaining captured output for retry/failure diagnostics;
- the PDF invocation uses verbose DocFX diagnostics and the console message explains that large page sets can take several minutes;
- the 3.3.7 PowerShell parser preflight remains present;
- `docs/DocfxDependencies.csproj` still pins `System.Formats.Nrbf` 10.0.11 with `PrivateAssets="all"`, while the LocalGPT application project itself has no direct NRBF package reference;
- Node.js 20–22 cross-platform bootstrap, DevExpress license preflight, documentation source preflight, and generic DocFX unresolved-reference repair remain wired;
- existing `@rendermode InteractiveServer` declarations are unchanged from 3.3.7;
- source archive packaging follows the Finder-compatible root-layout ZIP structure and is checked for duplicate, case-folded, and Unicode-normalized path collisions.

A real .NET/PowerShell build remains intentionally unexecuted in this packaging environment and should be verified on the target developer machine with `pwsh ./Build-Release.ps1`.
