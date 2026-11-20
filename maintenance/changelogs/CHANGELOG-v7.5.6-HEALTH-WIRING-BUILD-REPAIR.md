# LocalGPT 7.5.6 — Participant-health wiring build repair

- Repair the stale `Assert-CouncilXRoundWiring.ps1` assertion left by the 7.5.5 participant-health policy change. The maintained guard now checks actual model error collection, distinct-round failure counting, the configured threshold with a two-round minimum and useful-success protection, while retaining the infrastructure-failure filter.
- No runtime behavior, approval state, DevExpress UI, database, or user configuration changes. The existing mandatory build guard remains enabled; no exemptions or bypasses introduced.
- Source candidate: ZIP integrity and changed assertion review only. PowerShell execution, LocalGPT compilation and runtime acceptance remain pending on the owner's system.
