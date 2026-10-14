# LocalGPT 2.7.5 changelog

## Build guard repair

- Fixed the `Assert-TextServiceOwnership.ps1` failure introduced by the live-upload attachment presentation.
- `Chat.razor` no longer owns `string.Join`, HTML encoding, filename normalization, or attachment-chip construction.
- Added `CouncilTextService.BuildAttachmentPresentation(...)` as the single service-owned path for safe attachment presentation.
- `ChatMemoryMessageMapper.BuildPersistedContent(...)` now reuses the same service path, so live messages and restored/persisted messages cannot drift into two separate implementations.
- The text-service ownership baseline was **not** widened and the MSBuild guard was **not** disabled or weakened.

## Code-generation controller compile repair

- Added the missing `using LocalGPT.Services;` import to `CodeGenerationController.cs`, resolving `LocalGptCatalogService` from its actual namespace.
- Extended the code-generation source audit so the controller's catalog dependency and required service namespace import are checked in future source validation.

## Existing behavior retained

- Council live activity/results, parallel provider-qualified participants, X-Rounds, heartbeat handling, per-host/per-road controls, model prose spacing repair, code/file generation, PowerShell output, approval-gated workspace writes and 1-Wire behavior from 2.7.4 remain unchanged.
- `Directory.Build.targets` keeps the text-service ownership target enabled. The repair conforms to the architecture rule instead of bypassing it.

## Version

- LocalGPT application/wrapper/installer: **2.7.5**.
- `LocalGPT.WireProtocolVersion` remains **2.1.1**; no wire message contract changed.
