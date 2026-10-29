# LocalGPT 6.6.3 — preserve seeded team assignment modes and compact model picker

- Correct `CouncilTeamConfigurationService.NormalizeSeedDefaults`: do not silently rewrite explicitly supplied `AllSelected` roles to `RandomRange` or apply the generic random-participant minimum/maximum. This was the root cause of the initial hardware calibration seed displaying/using a 2–3 sample instead of all benchmark subjects.
- Advance the database Council seed revision so untouched supplied teams are reconciled to the correct roles; existing user-owned copies, edited roles, disabled configurations and tombstones remain protected by the existing reconciliation policy.
- Display random min/max controls only for roles that actually use random assignment. For `AllSelected` show an explicit all-participant explanation instead of stale misleading 2–3 controls.
- Replace the large per-model checkbox grid with a DevExpress `DxListBox`: integrated search, virtual list rendering, 260px height and multi-selection per provider endpoint. Selection keys remain the exact provider-qualified identities, and changes on one endpoint do not erase selected models on other endpoints or saved offline bindings.
- Preserve the existing generic roles, model discovery, role-selection modes, team authoring, workflow execution and hardware calibration measurement implementation. No hardcoded model names or runtime sampling are introduced.

Verification status: Source/contract inspection only. The environment has neither `dotnet` nor Windows PowerShell/pwsh; native build, UI rendering and PowerShell guards still need to be executed on a supported machine. No Python or Node tests were added.
