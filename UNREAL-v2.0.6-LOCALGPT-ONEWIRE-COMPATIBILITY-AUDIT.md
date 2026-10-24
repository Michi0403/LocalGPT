# Unreal 2.0.6 ↔ LocalGPT 6.2.8: optional 1-Wire AI integration audit

## Evidence examined

- `Unreal/src/Unreal/Services/WorldOneWireBridgeService.cs`, `WorldDirectorService.cs`, `WorldNpcControlService.cs`, `WorldSignalService.cs`, and `UnrealServiceCollectionExtensions.cs`
- `Unreal/src/Unreal/Controllers/WorldDirectorController.cs`, `BusinessObjects/WorldDirectorContracts.cs`, and `WorldNpcControlContracts.cs`
- `Unreal/src/Unreal/Components/Pages/LocalGptSetup.razor` and `appsettings.json`
- LocalGPT `Services/OneWire/OneWireExecutionServices.cs`, `OneWireStateServices.cs`, `OrganicPluginDxAiFunctions.cs`, `Interfaces/IOneWireServices.cs`, `CouncilGameDirectorService.cs`, and `LocalGPT.WireProtocolVersion`.

## Compatibility verdict

**Protocol foundations match, but there is no live game 1-Wire control integration yet.** Both source projects reference `LocalGPT.WireProtocolVersion` **2.1.1**. LocalGPT already supplies the generic peer registry, connection registry, work spooler, Council request executor, user approval system, capability discovery and `organic.plugin.invoke` DXFunction. That generic function refuses a peer that is not linked and does not advertise the requested enabled/online capability; it is coordination-only, requires explicit confirmation, and does not automatically grant access.

Unreal only implements `GET /api/onewire/http-json/profile` probing (`WorldOneWireBridgeService`), **not** a registered linked 1-Wire peer, encrypted pairing/approval flow, or its own 1-Wire capability execution handler. Its settings explicitly state that discovery is not pairing. `WorldDirectorController` is a **separate** admin-only HTTP endpoint, protected by a loopback check and explicit administrative token, and must not be treated as the protected 1-Wire Invoke endpoint.

## Game features already reusable

| Domain | In Unreal 2.0.6 | In LocalGPT 6.2.x | Missing bridge |
| --- | --- | --- | --- |
| NPC companions | `IWorldNpcController` and authoritative `WorldNpcControlService` accept bounded observations and validate movement/combat/speech; offline `WorldMissionHelperController` exists | Council roles, model routing, generic 1-Wire work, human approval and game-specific *internal* director services | Async, correlated NPC observation → proposal adapter; active peer pairing; validated offline fallback |
| Lamps, switches and world interactions | `WorldSignalService.Emit` uses authored links, bounded fan-out, `Enable`/`Disable`/`Toggle`/`SetCondition` | Connected-peer `organic.plugin.invoke` can transport advertised typed capabilities with approval | World-session-aware light/signal proposal API with ownership and rate limits; no raw arbitrary object updates |
| World event / enemy director | `WorldDirectorService` accepts only `Rumor`, `DroneWarning`, `Patrol`; engine enforces cadence, fairness and session constraints | Council and generic 1-Wire coordination; `CouncilGameDirectorService` serves LocalGPT's *own* games | Advertise `world.director.propose` with typed schema, externally linked invoke, engine-side validation and fresh permission checks |
| Organic catalog | Worlds, NPC controller IDs and action types are already exposed through engine services | Live peer `Capabilities`, Council preferred capability lists and runtime discovery | Engine must advertise *actual registered* game operations and schemas; do not hardcode game-specific keys in LocalGPT |
| Standalone/offline | `LocalGPT.Enabled=false`, `Director.Enabled=false`, no automatic external network work; local AI-free NPC behavior | No LocalGPT dependency required by Unreal core | Preserve this default at every launch and export |

## Suggested integration contracts (proposal, not shipped functionality)

These are candidate **engine-advertised** capability keys, to be registered from configurable engine module metadata rather than embedded in LocalGPT application code:

- `unreal.world.snapshot.inspect`: bounded, redacted session- and actor-scoped observation with authoritatively assigned IDs and expiry.
- `unreal.npc.intent.propose`: a `WorldNpcDecision` proposal for one valid NPC controller, with original observation revision and correlation ID.
- `unreal.world.signal.propose`: typed, low-rate suggested `Enable`/`Disable`/`Toggle`/`SetCondition` on an allowlisted authored signal target; never a raw process, filesystem, JavaScript or damage endpoint.
- `unreal.director.event.propose`: a `WorldDirectorProposal` subject to the engine's existing `WorldDirectorService.Propose` pacing and safety checks.
- `unreal.world.capabilities.inspect`: registered controller IDs, world event types and module schemas, without leaking player data or privileged state.

**AI decisions must not execute inside the simulation's synchronous `IWorldNpcController.Decide` frame.** Batch or sample observations at a bounded interval and send asynchronously from a supervised service. Cache only schema-validated, time- and revision-limited proposals. If LocalGPT disconnects, stalls or returns an invalid proposal, the built-in NPC controller and deterministic world simulation continue uninterrupted. Do not allow a stale proposal to run after the owner, actor, world or session changes.

## Consent and boundaries

1. Default gameplay remains fully standalone, whether or not LocalGPT is installed. No automatic pairing.
2. An explicit in-game opt-in initiates discovery and secure 1-Wire pairing; LocalGPT must confirm the new peer through its normal trust/MFA flow.
3. Capability discovery reports only currently available actions and typed schemas. A game operator configures which actions LocalGPT may *propose*; user-facing approvals remain intact for consequential effects.
4. Each result binds to peer identity, world/session, actor or signal ID, observation revision, correlation/work-order ID and expiration, with replay protection.
5. The **authoritative engine** runs legality, collision, ownership, cooldown, fairness and safety checks; an LLM cannot set position, inventory, health, damage, permissions or world state directly.
6. LocalGPT team/model affinity remains per-user, visible and editable; offline AI hosts are not silently selected.
7. Do not transmit every frame or every lamp: use events and rate-limited observation summaries. Limit concurrent NPC inferences to protect gameplay latency and local models.

## Caveats

- LocalGPT's internal `CouncilGameDirectorService` and its Corridor / CreatureTournament seed material are not automatically callable against Unreal's simulation. The latter is a different authoritative world engine.
- The generic LocalGPT 1-Wire catalog and DXFunction plumbing are largely sufficient as a **transport/orchestration foundation**, but Unreal does not yet implement the necessary executable 1-Wire peer and no end-to-end game AI test has been run.
- The exported supplied engine remains **unchanged**. This document is a read-only architecture audit, not an Unreal 2.0.7 release or a promise of completed pairing.
