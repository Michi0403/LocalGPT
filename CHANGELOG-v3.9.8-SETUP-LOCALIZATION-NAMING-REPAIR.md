# LocalGPT 3.9.8 — setup localization and naming repair

## Initial setup assistant

- Localizes server-generated setup status messages through the maintained LocalGPT localization catalog instead of emitting English-only runtime text.
- Adds reviewed German setup-assistant translations for the hardware, CanIRun.ai, provider, model-discovery, benchmark-Council, and runtime-path wording visible on `/install`.
- Normalizes setup-assistant `CanIRun` labels to the credited `CanIRun.ai` product name.
- Keeps exact catalog-key parity across every maintained localization file; cultures without a newly reviewed translation retain the existing English fallback behavior for those new strings.

## Interactive rendering contract

- Preserves the existing routed `InteractiveServer` boundaries. The `/install` page remains interactive and `InitialSetupAssistantPanel` continues to inherit that circuit instead of creating a nested render boundary.
- The intentionally static error page remains unchanged.

## Validation boundary

- Source/static checks only were performed for this handoff. No `dotnet build`, `dotnet publish`, GitHub access, or platform packaging was used.
