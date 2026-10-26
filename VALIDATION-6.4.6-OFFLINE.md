# LocalGPT 6.4.6 offline validation

## Scope

A narrow compile-gate repair for the reported 6.4.5 Windows MSBuild failure:
`Services/NetworkCertificateService.cs | private static Oid MapExtendedKeyUsage(...)`.
The existing mandatory PowerShell architecture gate was not changed or suppressed.

- Updated the method declaration to an instance-owned `private Oid` member of the existing DI service.
- Retained `builder.Services.AddSingleton<INetworkCertificateService, NetworkCertificateService>();` in `Program.ServiceRegistration.cs` unchanged. DI lifetime ownership stays centralized.
- Preserved the enum-to-OID mapping, certificate generation call site, certificate presets, installer changes, and DevExpress UX.
- Bumped LocalGPT version 6.4.5 -> 6.4.6; installer version 4.9.9 unchanged.
- Added `tests/static/test_certificate_static_architecture_646.py`, including checks for the existing DI registration and the mandatory static guard wiring.

## Executed offline

- `python -m unittest discover -s tests/static -p 'test_*.py'`: **92 passed**.
- `node tests/static/documentation-mermaid-touch-behavior-642.cjs`: **passed**.
- Full archive ZIP integrity: **passed**.
- Confirmed the only changed implementation file (other than version metadata) is `NetworkCertificateService.cs`; guard and DI registration files are byte-for-byte unchanged from 6.4.5.

## Not executable here

- The container does not have Windows PowerShell, `pwsh`, or the .NET SDK. Actual `Assert-ApplicationStaticPolicy.ps1` execution, MSBuild, cert issuance, and Windows installer acceptance could not be performed. The successful Windows build remains to be confirmed by the maintainer.
