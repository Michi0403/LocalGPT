# LocalGPT 6.2.1 — semantic planner and 1-Wire peer visibility

## Fixed

- Limit the semantic planner's model-facing catalog to the highest-ranked persisted teams, registered DXFunctions, connected executable 1-Wire capabilities, and organic skills. Required function and capability keys take priority; the final proposal is still validated against the **full authoritative directory**.
- Treat illustrative bare-period placeholders outside quoted strings (`"teams": .`, `"teams": ...`) as an incomplete plan without attempting JSON deserialization. Catch other `JsonException` values at the untrusted model-output boundary, log a warning, and continue through the existing catalog fallback. No permissions or actions are inferred from malformed text.
- Give PublisherStudio-origin 1-Wire work a meaningful `CouncilParticipants` error when no configured provider model is available, instead of presenting an unexplained empty result. No model/provider is launched automatically.
- Show connected and discovered 1-Wire peers in the Runtime 1-Wire Security workbench even when encrypted trust count is zero. A saved Human Collaboration link decision and a cryptographically trusted peer are different security states.
- Refresh the 1-Wire peer inventory when viewing the protocol or peers sections, and provide an explicit refresh control.

## Intact

- Existing human collaboration approvals, 1-Wire connection permission checks, typed DXFunction execution, Council team routing, goal checkpointing, and provider selection.
- Existing commented-out Ollama session disposal workaround from 6.2.0. No further HTTP lifetime changes in this version.
- PowerShell maintenance and architecture scripts; no guards removed or weakened.

## Verification boundary

Source-level checks only. A .NET build and the actual Windows PowerShell maintenance gates were not run in this environment. The model's stochastic inference and actual 1-Wire handshake require integration testing on the user's machine.
