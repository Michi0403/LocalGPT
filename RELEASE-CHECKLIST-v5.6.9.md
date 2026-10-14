# LocalGPT 5.6.9 – Release checklist

Baseline: `LocalGPT-5.6.8-source.zip` / extracted 5.6.8 tree. Output: `LocalGPT-5.6.9-source.zip`.

| File | Change |
|---|---|
| `Build-Release.ps1` | Request the refreshed release helper 1.0.5; do not reuse cached 1.0.4. |
| `build/Ensure-ReleasePackagingPackage.ps1` | Require helper version 1.0.5 and preserve package/version/capability checks. |
| `build/Publish-ReleasePackagingPackage.ps1` | Publish version 1.0.5 for the updated helper dependency graph. |
| `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj` | Keep PDFsharp 6.2.4; directly pin vulnerable transitive PKCS to secure 10.0.13; helper 1.0.5. |
| `src/LocalGPT/LocalGPT.csproj` | Roll application source version 5.6.8 → 5.6.9. |
| `src/LocalGPT/Components/App.razor` | Advance three local JS asset version markers to 5.6.9; preserve markup. |
| `CHANGELOG-v5.6.9-RELEASE-PACKAGING-PKCS-SECURITY.md` | Document cause, dependency floor, helper cache version and limitations. |
| `build/audit_release_packaging_pkcs_5_6_9.py` | Source-only regression for the security floor and release helper cache/version contract. |
| `RELEASE-CHECKLIST-v5.6.9.md` | Record all intentionally touched files and preserved behavior. |
| `VALIDATION-v5.6.9-source.md` | Source-only tests and Windows restore/build acceptance criteria. |

## Review checklist

- [x] Source files compared byte-by-byte against 5.6.8: no source files deleted, no unrelated file changed.
- [x] All four helper version references agree on `1.0.5` (project, Build-Release, Ensure, Publish).
- [x] PDFsharp 6.2.4 remains in use; `System.Security.Cryptography.Pkcs` floor explicitly 10.0.13.
- [x] NuGetAudit remains enabled with NU1903/NU1904 warnings-as-errors.
- [x] Existing PDF chunk manifest merge and capability handshake verified by source-only regression.
- [x] DevExpress dependencies, SDK 10.0.402, core services, Razor handlers and LocalGPT runtime behavior left intact.
- [x] Final ZIP CRC/testzip verified.
- [ ] On Windows, restore and compile the ReleasePackaging project and application with SDK 10.0.402, checking for NU1903.
- [ ] Build/install the 1.0.5 release helper tool and smoke-test PDF merging; cannot be confirmed without .NET.

PublisherStudio 4.1.9 is unchanged. Older `1.0.4` NuGet files can remain but must not be selected by the new LocalGPT release workflow.
