# LocalGPT 4.3.0 — Council workflow compile repair

## Compile repair

- Corrects the ASCII DOOM Council seed initializer to assign its configured workflow through `OrganicCouncilTeamDefinition.WorkflowSteps`.
- Removes the invalid `Workflow` member assignment that caused compiler error `CS0117` in `OrganicCouncilBlueprintSeedDataService.cs`.
- Preserves the 4.2.9 deterministic combat, Human/AI ownership, enemy, pathfinding, Council-tool and ASCII viewport behavior unchanged.
- Keeps Council system seed version 28 unchanged because the seeded workflow content itself is unchanged; this release fixes the source member name used to construct that same definition.

## Release identity

- Advances LocalGPT, installer console and webview wrapper from 4.2.9 to 4.3.0 to keep every semantic-version component single-digit.
- Refreshes documentation and browser cache-busting release identity to 4.3.0.
- Preserves the historical 4.2.9 changelog and validation records.

The user's Windows Debug build reached the C# compiler and reported `CS0117` for the invalid `Workflow` property. No .NET build, restore, publish, signing/notarization or GitHub operation was performed by this source-preparation environment.
