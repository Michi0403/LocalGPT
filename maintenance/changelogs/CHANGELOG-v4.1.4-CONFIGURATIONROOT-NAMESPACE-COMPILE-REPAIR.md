# LocalGPT 4.1.4 — ConfigurationRoot namespace compile repair

LocalGPT 4.1.4 repairs the C# compile failure reported after the 4.1.3 policy-guard fixes. The RID-neutral application build reached normal compilation and found an ambiguous `ConfigurationRoot` reference between LocalGPT's domain configuration model and `Microsoft.Extensions.Configuration.ConfigurationRoot`.

## Changes

- `OllamaProcessService`, `ProviderRuntimeManagementService`, and `ProviderModelRuntimeService` now use the explicit alias `LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot` at their service boundaries.
- The provider runtime configuration save path now constructs `LocalGptConfigurationRoot` explicitly instead of relying on the short type name.
- Added `build/audit_configuration_root_qualification.py`, which rejects bare `ConfigurationRoot` usage outside the domain type declaration and requires the explicit alias in the affected services.
- The existing system-variable and iterator policy guards are unchanged; no compiler or policy baseline exception was added.
- The 4.1.2 adaptive low-memory documentation policy, 4.1.1 provider workbench, 4.1.0 Matrix operator layer, signing/notarization safeguards, and PDF/Pages protections remain intact.

## Build failure addressed

The macOS 4.1.3 release build passed both strict policy guards and then stopped with CS0104 in `OllamaProcessService.cs` and `ProviderRuntimeManagementService.cs`. Both errors were caused by the same short-name collision with the framework `ConfigurationRoot`. 4.1.4 removes the ambiguous short name rather than deleting framework usings or weakening implicit-using behavior.
