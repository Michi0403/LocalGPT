# LocalGPT 5.9.3 — source validation and Windows acceptance

## Static validation performed

- Full baseline source archive retained with version-renamed root; zero original files omitted.
- Script delta limited to renaming the shadowed PowerShell identifiers. No asset requirements or CSS assertions removed, and strict mode preserved.
- `$requiredAssets` remains a `HashSet[string]`; the separate `foreach ($requiredCssContract in ...)` cannot replace it. The success message uses `$requiredAssets.Count`.
- LocalGPT project file parsed as XML and version 5.9.3 confirmed.
- Full output ZIP CRC check and unchanged-file byte comparison passed.

## Runtime validation required on Windows

1. In Visual Studio, rebuild LocalGPT; ensure `Assert-StaticWebAssets.ps1` prints `Static web asset validation passed for ... maintained LocalGPT files.` and exits 0.
2. Confirm compilation and documentation generation continue past `Directory.Build.targets` line 211.
3. Check the Council evidence popup after launch; both scroll panes must still stretch to the bottom.

No PowerShell or .NET build was run in this environment. The next Windows build remains authoritative.
