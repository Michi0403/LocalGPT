# LocalGPT 3.9.1 — text-service ownership build repair

## Fixed

- Repairs the authoritative `Assert-TextServiceOwnership.ps1` build failure exposed by the 3.9.0 Setup completion. Two new `StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)` checks had been added directly to `InitialSetupAssistantPanel.razor`, which correctly violated the existing component/controller text-service ownership policy.
- Exposes the already existing `AiProviderBootstrapService.IsOllamaProfile(...)` classifier through `IAiProviderBootstrapService` and makes both Setup UI classification sites delegate to that provider-owned policy instead of duplicating provider-key string logic in the Razor component.
- Keeps the text-service ownership baseline and guard unchanged; the source now complies with the existing architecture rule rather than whitelisting the regression.
- Removes a second next-stage build-guard problem found during follow-up static parity review: the new CanIRun JSON request no longer passes the literal `"application/json"` directly inside a `StringContent` constructor. The technical media type is stored as a service constant and the existing constructor-initialization guard remains unchanged.

## Preserved

- All LocalGPT 3.9.0 runtime-setup work remains intact: provider-profile v2 parsing, Ollama lifecycle controls, explicit first-model installation, CanIRun JSON recommendations, and system/unified-memory probing.
- The original eight hosted services and all 15 established `@rendermode InteractiveServer` component boundaries remain unchanged.
- The executable/idempotent 60-row Council knowledge SQL seed contract remains unchanged.
- LocalGPT 1-Wire protocol and release-packaging package versions remain unchanged.

## Version

- LocalGPT: `3.9.1`
- LocalGPTInstallerConsole: `3.9.1`
- LocalGPTWebviewWrapper: `3.9.1`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)
- LocalGPT.ReleasePackaging: unchanged (`1.0.2`)

## Validation boundary

The supplied macOS release log is the authoritative runtime evidence for the 3.9.0 failure. This environment still does not run the .NET/PowerShell release build. 3.9.1 is therefore validated here through deterministic source/static checks, including parity scans for the two PowerShell guards involved in this repair.
