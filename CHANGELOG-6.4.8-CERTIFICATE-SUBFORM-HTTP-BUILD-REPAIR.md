# LocalGPT 6.4.8 — Certificate subform/HTTP and Razor build repair

- Moved the full certificate editor out of the oversized Install page into `Components/Shared/NetworkCertificateEditor.razor` with its own form EditContext, DevExpress editors, validation, service execution, notifications and activity callback. Preserved all X.509 selectable enums and CA/end-entity presets.
- Fixed compiler failures seen in the user's Windows build: explicit `EditForm` ChildContent context and a typed `DxCheckBox.CheckedChanged` handler.
- Added `NetworkCertificateController` with distinct HTTP endpoints for both public `INetworkCertificateService` operations. It uses the existing DI service, automatic `[ApiController]` DataAnnotations validation (400 validation problem), explicit human approval for certificate-related API access, and an independent service safety boundary.
- Kept `Install` as parent only for the draft network listener update and workflow logging after creation. No service lifetime change, no static utility, no change to certificate cryptography, installer, persistence or Council logic.
- Extended the existing mandatory PowerShell certificate-contract source guard with subcomponent, controller coverage, approval, and explicit Razor-context/callback rules. Preserved the existing guard registration.
- Added an offline regression test for component/HTTP/service ownership and updated version assertions.

## Verification
101 LocalGPT source/static tests pass; an actual .NET/MSBuild build and Windows integration test were not executed in this environment. The project must be compiled in Windows to confirm Razor generation and runtime behavior.
