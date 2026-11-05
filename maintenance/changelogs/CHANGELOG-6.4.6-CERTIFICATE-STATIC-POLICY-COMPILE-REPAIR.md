# LocalGPT 6.4.6 — certificate static architecture build failure repair

## Reproduced failure from the Windows build report

The mandatory `Assert-ApplicationStaticPolicy.ps1` build gate correctly rejects the `private static Oid MapExtendedKeyUsage(...)` helper introduced with the certificate enum editor in 6.4.5. Its failure is surfaced via `Directory.Build.targets(177,5)` and MSB3073. The problem is the new application-level `static` declaration, **not** an erroneous guard.

## Repair

- `NetworkCertificateService.MapExtendedKeyUsage` is now an instance-owned pure helper. The certificate service already calls it from an instance workflow, so the call site, EKU OIDs, certificate contents and public behavior are unchanged.
- The existing build-breaking PowerShell architecture policy and `Directory.Build.targets` wiring remain untouched.
- New offline regression tests check the exact forbidden declaration, required OID mappings and the continuing build gate.
- Application version bumped to 6.4.6; installer version 4.9.9 remains unchanged because no installer code was modified.

## Validation

- Python source regression suite and exact static-declaration equivalent scanner executed offline; see `VALIDATION-6.4.6-OFFLINE.md`.
- PowerShell and .NET are unavailable in this environment, so a successful Windows MSBuild has **not** been claimed.
