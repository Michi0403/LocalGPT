# LocalGPT 5.6.9 – source-only validation

## Verified

- `build/audit_release_packaging_pkcs_5_6_9.py`: PASS. Verifies direct PKCS 10.0.13 pin, PDFsharp 6.2.4, tool helper 1.0.5 version consistency, release capability contract, and NuGet auditing.
- `build/audit_documentation_pdf_merge_5_5_0.py`: PASS. Confirms the existing durable, ordered PDF chunk-manifest merge path and size/validation expectations.
- `build/audit_powershell_portable_paths.py`: PASS for 79 PowerShell scripts.
- XML parsed for all maintained projects; JSON parsed for `global.json` and `.config/dotnet-tools.json`.
- Baseline: 3,920 files. Exactly six modified source files; no removals. Four new files: security changelog, source-only guard, release checklist and validation report.
- ZIP file-entry CRC check and complete file-data hash comparison against release tree: PASS.

## Not tested

- No `dotnet restore`, `.NET` compile, NuGet dependency graph resolution, PDF rendering or runtime test were performed.
- NuGet's final resolved `System.Security.Cryptography.Pkcs` dependency must be verified on the owner machine; a source pin is not evidence of a completed restore.
- The Microsoft .NET 10 SDK remains pinned to `10.0.402`; DevExpress stays `25.2.10`.

## Required Windows acceptance

1. In Visual Studio or command line with SDK 10.0.402, restore `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`.
2. Inspect the `LocalGPT.ReleasePackaging` NuGet graph; `System.Security.Cryptography.Pkcs` must resolve to `10.0.13`, not `8.0.1`.
3. Build the full LocalGPT solution with `NuGetAudit` still enabled. The reported `NU1903` for PKCS must be gone.
4. When publishing a LocalGPT release, use release helper 1.0.5; verify the installed `capabilities` output includes `pdf-merge-input-list` and exercise actual PDF merging.

Third-party dependency evidence: https://www.nuget.org/packages/PDFsharp/6.2.4 and https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13.
