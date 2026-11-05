# LocalGPT 6.4.7 — validated certificate form

- Replaced unguarded certificate creation UI callback with a dedicated `EditForm` / `DxFormLayout`, DataAnnotations validation, and `OnValidSubmit`.
- Preserved combinable `DxListBox` Key Usage and Extended Key Usage enum lists and existing safe HTTPS defaults.
- Added `IValidatableObject` cross-property rules and a field-level `CertificateExtendedKeyUsageSelectionAttribute` for incompatible CA and EKU selections.
- CA toggle restores a coherent CA purpose (KeyCertSign, CrlSign; no end-entity EKUs, no Kestrel assignment); disabling CA restores HTTPS-end-entity defaults. All options remain individually editable and independently validated.
- DevExpress editors show contextual validation messages and valid/invalid states. Invalid combinations disable certificate creation before the service call; double submit is rejected.
- Retained independent security checks in `NetworkCertificateService` (server authentication EKU must be present for HTTPS), certificate path handling, and service lifetime registration.
- Extended `build/Assert-NetworkCertificateContract.ps1` with non-bypassable source contract checks.

See `VALIDATION-6.4.7-OFFLINE.md`. No .NET or Windows build was executed in this environment.
