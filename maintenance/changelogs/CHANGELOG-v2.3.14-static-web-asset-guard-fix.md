# LocalGPT 2.3.14 — static-web-asset guard fix

- Fixed `Assert-StaticWebAssets.ps1` under PowerShell `Set-StrictMode -Version Latest`.
- The guard now enumerates MSBuild `<Content Update=...>` / `<Content Include=...>` nodes via XPath rather than reading a non-existent dynamic `ItemGroup.Content` property.
- Runtime-critical `TacosLogos.svg`, `Information.svg`, and `documentationViewer.js` are explicitly required.
- Preserves the 2.3.13 restored `wwwroot/images` tree, CLI build ordering, documentation/PDF/Pages snapshot generation, service resilience, and release wiring.
- Version raised to 2.3.14 in the application, installer, wrapper, and runtime version service.
