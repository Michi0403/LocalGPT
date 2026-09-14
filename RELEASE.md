# LocalGPT 4.3.0

LocalGPT 4.3.0 is a focused compile-repair release for the 4.2.9 ASCII combat/Council changes. The ASCII DOOM system seed now assigns its configured workflow through the actual `OrganicCouncilTeamDefinition.WorkflowSteps` member. The invalid `Workflow` initializer that produced compiler error `CS0117` is removed.

All 4.2.9 game behavior remains in place: deterministic enemies and combat, map-aware AI Hunter navigation, Human-mode ownership enforcement, optional active-session game reads, repaired Council game/display functions, the one-pass Doom Council workflow, and the browser-measured ASCII game viewport.

The Council seed version remains 28 because this release does not change the seeded workflow semantics; it corrects the C# member name required to compile that existing definition.

No `dotnet` build, restore, publish, release build, signing/notarization, GitHub access or GitHub API operation was performed. See `CHANGELOG-v4.3.0-COUNCIL-WORKFLOW-COMPILE-REPAIR.md` and `VALIDATION-v4.3.0-source.md`.
