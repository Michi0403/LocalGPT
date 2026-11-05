# LocalGPT 6.2.8 — Council text ownership / optional game-engine integration audit

## Source repair (build blocking)

- Addressed both findings from the Windows `Assert-TextServiceOwnership.ps1` audit at `CouncilTeams.PublisherOrganicRouting.razor.cs` and `CouncilTeams.razor.cs`.
- Centralized filtering, deduplication, and sorting of the `publisher.` 1-Wire protocol capability namespace in the **already injected** `CouncilTextService` as `FilterPublisherOrganicCapabilityKeys`.
- Both the persisted team-affinity projection and live peer discovery use the same service method, without adding a hardcoded key allowlist or modifying team-save semantics.
- Preserved the production audit scripts, approval policy, runtime peer connection checks, and existing model bindings exactly. No architecture guard exemptions were introduced.
- Updated the two version-locked source tests and extended the team-affinity regression for text-service ownership.

## Separate source-only audit

- Reviewed the user-supplied Unreal 2.0.6 engine sources and checked compatibility against LocalGPT's existing 1-Wire protocol package and organic function architecture.
- See `UNREAL-v2.0.6-LOCALGPT-ONEWIRE-COMPATIBILITY-AUDIT.md` for the implemented/offline capabilities, missing secure pairing/invocation steps, and a safe design for NPC, lighting and director AI.
- No Unreal source file is changed in this LocalGPT release. The engine's own source/build guards remain untouched.

## Known limits

- The container has neither .NET SDK nor PowerShell, so full compilation and Windows architecture scripts cannot be executed. The report does not claim a Windows build pass.
- This release fixes the two known text-ownership findings, **not** every possible future compiler issue.
- The Unreal client currently performs optional 1-Wire **profile discovery only**. Pairing and authenticated game capability dispatch are not introduced by this release.
