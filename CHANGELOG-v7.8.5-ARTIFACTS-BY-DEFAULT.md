# LocalGPT 7.8.5 – Unassuming Uakari

## Artifacts on by default
- Chat › "Council downloadable artifacts" is on by default and stays as set across runs (opt-out). Previously it was consumed after every run and reset to off.
- Diagnostic chat URLs no longer force it off. Presets still restore their own saved value.

## Coordinated source route for development teams
- New runtime value `CouncilFileWorkAdditionalTeamsJson` lists development teams (C#, PowerShell, Java) and maps their own steps onto the coordinated route: architecture → plan, implementation → files, curation → review, build → verification and function permission.
- A listed team takes the route only when artifacts are on, the project is new (no attachments or selected source), no route role is human-only/required and its build step allows the functions. Otherwise it runs its own workflow unchanged.
- The Program Compiler team is unchanged. New optional field `functionPermissionStepKey` in the file-work policy (empty = previous behavior).
- Remove a team from the list in Database › SystemVariables to keep it on its own workflow.

Checks performed: brace/parenthesis balance of edited C# files, seed JSON parses. Not compiled yet; build and UI test pending.
