# LocalGPT 7.1.3 — live Council model staffing and approved repair

## Defect evidence

A paired PublisherStudio Council request could reach LocalGPT over 1-Wire but fail in `GoalExecutionService` with `CouncilParticipants` and zero usable models. Ollama discovery reports newly pulled local models as `IsInstalled=true, IsConfigured=false`; the old goal planner required both flags. The interactive Council runtime already accepted installed models.

## Changes

- `GoalExecutionService.PlanAsync` accepts locally installed model candidates even if they are not saved as preferred models. Configured nonlocal routes remain eligible. Planning probes provider models for each request and does not rewrite model preferences.
- Goal planning reads exact model assignments only from AI roles that explicitly use assigned-model modes, checks current provider identities, and surfaces `AssignedModelUnavailable` instead of substituting an unrelated model or host. A separate explicit selected model remains authoritative.
- `CouncilTeamStaffingHealthCheck` checks current provider discovery and persistent Council team staffing, reporting absent models, insufficient distinct staffing, and stale provider-qualified assignments. Its model count is consistent with the goal planner.
- For one stale binding in a **user-owned** team, the Configuration Steward can offer concrete installed-model replacement choices through its existing Human Collaboration approval workflow. The repair uses a compare-and-set transaction, changes the one approved role binding, and refreshes the role/model index. Protected system-seed teams, ambiguous mappings, and multi-stale bindings remain report-only.
- An unsuccessful 1-Wire Council `WorkResult` now includes a targeted message directing the operator to team/model diagnosis rather than disguising missing participants as successful output.

## Preserved contracts

No changes to Windows/macOS/Linux installers or package scripts; no TLS, MCP, 1-Wire pairing/MFA/transport format, database migration, provider credentials, automatic model installation, or permission policies. The existing run-local Goal Recovery Helper is unchanged: it must never authorize a new provider or mutate saved assignments. Approved team repair is owned by the Configuration Steward.

## Release identity

Application version 7.1.3. Installer version remains 5.0.3 because installer code is unchanged.
