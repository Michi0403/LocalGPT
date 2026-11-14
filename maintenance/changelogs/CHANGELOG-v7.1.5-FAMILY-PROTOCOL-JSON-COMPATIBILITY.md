# LocalGPT 7.1.5 — family protocol JSON compatibility

## Scope

- Centralized one-wire JSON representation (case-insensitive property input, null omission, string-enum output, legacy numeric-enum input, and quoted-number input without quoted-number output) in `FamilyWireJsonConventions`, matching PublisherStudio's corresponding convention. `OneWireEnvelopeCodec` uses it for both outgoing envelopes and digest verification.
- Routed MVC and SignalR JSON enum handling through the common family convention without changing MVC's established `ReferenceHandler.IgnoreCycles`, SignalR's `ReferenceHandler.Preserve`, LocalGPT's established property/number format, or MessagePack's contractless/untrusted-data resolver.
- Applied the enum-compatible `JsonSerializerDefaults.Web` convention to the MCP JSON-RPC endpoint and gateway accessor while retaining MCP's JSON-RPC request parsing, method dispatch, response format, and authorization.
- Removed three direct/default JSON option usages in one-wire WorkResult generation so those properties use the already-injected one-wire codec. No new trust bypass or payload shape was introduced.
- Kept MVC HTTP profile responses on the original MVC formatter. The profiles remain readable with case-insensitive clients; there is no forced output-property-case change.
- Added a repository-wide PowerShell preflight source contract for relevant transport boundaries. It is called by the existing PowerShell compatibility guard on each build host.

## Nonchanges

- No wire-protocol version bump, no installer/bootstrap modification, no change to pairing/MFA/crypto/signature rules, no new LAN exposure, no change to Ollama/model scheduling.
- PublisherStudio has its own versioned companion change; it is not included in this archive.

## Required integration checks

1. Build LocalGPT and PublisherStudio with their current shared `LocalGPT.WireProtocolVersion` dependency; run both PowerShell compatibility preflights.
2. Verify prior/new cross-product pairing on Windows, macOS, Linux, including MFA and signature/digest verification.
3. Round-trip `CapabilityResponse` with `InteractionEditor` in string and legacy numeric enum form, `CouncilRequest`, `WorkResult` success/failure, and `ApprovalRequired`.
4. Compare 1-Wire HTTP/JSON `profile` and work polling with pre-change clients; retain existing profile field casing and accept provider-qualified model selection.
5. Verify SignalR JSON/MessagePack with the actual LocalGPT browser clients; confirm Blazor Server UI connection on PublisherStudio is unaffected.
6. Verify MCP JSON-RPC `initialize`, tool listing/call, request identifiers, error responses and approval flow.

**Evidence:** source-level consistency checks only. No .NET compilation, PowerShell execution or cross-platform round trip was run in the preparation environment.
