# LocalGPT 6.4.9 — source-level validation

- 105 Python tests passed on the extracted and patched complete source.
- Shared report `FORM_AUDIT_ALL_COMPONENTS.md` / `.csv` inventories 96 Razor components with form/layout constructs across both apps (59 LocalGPT, 37 PublisherStudio).
- New mandatory build/release `Assert-RazorFormEncapsulation.ps1` verifies reusable Razor child ownership, event/transport boundaries and retained controllers. Its expected paths/fragments were checked against the source using an independent offline parser.
- LocalGPT existing TLSCONTRACT0036 now finds the exact fully-qualified `Enum.GetValues<System.Security.Cryptography.X509Certificates.X509KeyUsageFlags>()` required by the unchanged certificate guard.
- Existing licensing, permissions, 1-Wire trust handling, stored configuration, DI service lifetimes, and controlled writes have not been changed.
- NO dotnet/MSBuild, Windows PowerShell or pwsh execution was available. No official Windows build success is asserted.
- Real DevExpress Razor compilation and browser interaction validation are still required.
 - Component-scoped hardware CSS preserved across child boundary.
