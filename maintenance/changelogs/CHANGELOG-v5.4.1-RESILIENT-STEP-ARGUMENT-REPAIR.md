# LocalGPT 5.4.1 — Resilient Council step argument repair

- Fixed CS1503 at `OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs`: the resilience wrapper forwarded an `IReadOnlyList<string>` as `Step` argument 14, which is now the `includePriorTranscript` Boolean after the 5.4.0 addition of two workflow gate arguments.
- Bound all optional arguments to `Step` by name (`canUseOrganicFunctions`, `producesFinalAnswer`, `requiresHumanCheckpoint`, `enableRolePeerReview`, `summarizeRoleResults`, `includePriorTranscript`, `allowedAutomaticFunctions`). This preserves the existing compiler/release preset behavior and leaves the newly introduced gates at their defined defaults.
- Extended `build/audit_provider_qualified_council.py` with regression checks for these argument bindings.
- Updated active version and JS resource cache-busters to 5.4.1; retained 5.4.0 database migration and seed data unchanged.
- Source checks only; local .NET compilation and Council smoke tests remain necessary.
