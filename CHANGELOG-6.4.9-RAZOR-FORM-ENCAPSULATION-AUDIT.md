# LocalGPT 6.4.9 — Reusable form boundaries and all-form source audit

- Audited all Razor components in both provided applications; see `FORM_AUDIT_ALL_COMPONENTS.md` and CSV for 96 files that contain form/layout boundaries (out of 112 scanned).
- The mandatory read-only `build/Assert-RazorFormEncapsulation.ps1` is wired to normal app builds and release preflight. It must not rewrite implementations or suppress other guards.
- Fixed TLSCONTRACT0036 by restoring the fully qualified framework `X509KeyUsageFlags` enumeration in the certificate editor, without weakening the existing certificate guard.
- Extracted the physical host hardware form from `InitialSetupAssistantPanel` into `InitialSetupHardwareForm`, retaining the reviewed device list, host grouping, DevExpress editor controls, HWiNFO upload, and parent-owned hardware service callbacks.
- Preserved the certificate editor with BusinessObject DataAnnotations, 1-to-1 certificate HTTP methods and service safety validation.

## Validation limit

Source static and JS tests run offline. `pwsh` and `dotnet` were not available in the environment, so the mandatory PowerShell gates and .NET Razor compiler could not be executed. Treat this as a source patch pending the next Windows build; do not call it a successful build.

- Copied the component-scoped hardware CSS into the child to keep row styling across Blazor CSS-isolation boundaries.
