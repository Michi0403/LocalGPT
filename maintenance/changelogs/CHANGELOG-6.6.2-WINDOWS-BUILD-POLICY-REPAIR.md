# LocalGPT 6.6.2 — repair two source-policy build failures

Base: complete LocalGPT 6.6.1 source archive. PublisherStudio is unchanged.

## Repairs for Visual Studio build errors

1. **Application static policy:** Changed the `GameStudio.razor.cs` `ArtworkModes` list from a `static readonly` field to a component-instance `readonly` field. All `GameSceneArtworkMode` enum values and the existing DevExpress renderer selector are preserved. The Game Studio UI does not require shared process-level state for these choices.
2. **Text-service ownership policy:** Moved the `.pfx` certificate-path extension and file-existence check out of `Install.McpGateway.razor.cs` and into `NetworkCertificateService.IsExistingPfxFile(string? path)` exposed by the existing `INetworkCertificateService`. The Install page already injects this service; the existing singleton registration is unchanged. Both the optional remote web listener and the MCP listener use the same shared file picker. Invalid paths continue to display the existing warning and do not mutate either listener's draft settings.
3. Changed only LocalGPT's `<Version>` in `src/LocalGPT/LocalGPT.csproj` from `6.6.1` to `6.6.2` (one-digit version segments).

## Boundaries preserved

- Kept `build/Assert-ApplicationStaticPolicy.ps1`, `build/Assert-TextServiceOwnership.ps1`, `build/Invoke-ArchitectureAudit.ps1`, and all other PowerShell guards unchanged. No alternative Python source tests or exemptions were introduced.
- No changes to the Council backend / detached observer lifecycle, project/knowledge/regex authoring, renderer semantics, teams, database, UI route templates, JavaScript, TLS cryptography, installer, OneWire, or operating-system-specific paths.
- No change to the PFX file-picker acceptance policy: only existing files with a case-insensitive `.pfx` extension are selected. The injected service does not open/import certificates when choosing a path, preserving the subsequent startup security validation.
- All Windows, Linux, and macOS runtime IDs and the existing host-platform service registrations remain unchanged.

## Offline verification and limitations

- Evaluated the same static-declaration and UI/controller text-operation search expressions used by the two PowerShell policies across the extracted sources: zero findings after the repair.
- Cross-checked the new service method signature across the interface, implementation, existing Razor injection, and existing service registration.
- Checked untouched source entries against the 6.6.1 ZIP, project XML, version format, source line lengths in changed files, JavaScript integrity manifest, ZIP entry CRC, and absence of new Python tests.
- **Windows PowerShell and .NET are not available in the packaging environment.** The PowerShell guards were not executed here, and there was **no compilation or runtime test**. Further Visual Studio compiler and build-gate findings remain possible; these checks cannot prove successful Windows/Linux/macOS builds.
