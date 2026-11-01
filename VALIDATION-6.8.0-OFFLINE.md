# LocalGPT 6.8.0 — Offline source validation and required runtime acceptance

## Executed (offline; no GitHub or .NET build)

- Inspected the two user-supplied archives locally. Patches apply only to LocalGPT 6.7.9 sources.
- Verified that the exact Council source contracts demanded by the updated reconnect guard exist, including ordered-overlap preservation, canonical cache and control reload.
- Evaluated source-token assertions in `build/Assert-AiHelpEscalator.ps1` against the changed files; verified all current asserted tokens are present.
- Parsed both `ParameterSchemaJson` definitions as JSON; verified every required property is declared, new curator context fields remain optional, and `negativePrompt` is not added to escalation required fields.
- Parsed `LocalGPT.csproj` and `Directory.Build.targets` as XML; verified `Version=6.8.0` and the major/minor/patch rollover constraints.
- Checked that the patch changes no files in PublisherStudio and does not alter application-managed user data, dependency lockfiles or product binaries.

These checks are **source/static validation only**, not evidence that PowerShell scripts ran, that the C# compiler accepted every code path, or that any UI/LLM workflow passed end-to-end.

## Required host acceptance tests

1. On the user's licensed Windows .NET 10 environment, run Debug and Release builds. Confirm `Assert-CouncilXRoundWiring.ps1`, `Assert-AiHelpEscalator.ps1`, the XML documentation/DI checks, and all other mandatory architectural PowerShell gates actually pass, then compile both configurations. Any new build diagnostics must be handled as defects, not dismissed as guesses.
2. Restore a saved chat and rejoin its ongoing Council run: validate the visible DevExpress messages include the saved prefix plus Council messages exactly once, including reconnect/reload and additional user contributions.
3. Request AI-help after a failed operation: verify the bounded alternative, one curator attempt, and a single existing Human Collaboration request. Submit attempt A, then B, then replay A: failure/curator counters must remain stable.
4. Answer a guidance request through the normal Human Collaboration inbox. Confirm the persisted request status is `Answered`, `ai.help.status` with `checkHumanResponse=true` returns one `try_alternative`, and a new operation still requires its normal approval.
5. Exhaust Council member recovery under a configured non-strict team. Confirm failed evidence remains in the run, central help has a stable Council task, and re-reporting does not multiply curator sessions. Check explicit strict-list and disabled-tool scenarios remain restricted.
6. Exhaust goal replanning on a failed Council step. Confirm the last goal checkpoint has already been saved as `Failed` or `NeedsApproval` *before* model assistance runs. Restart before answering the Human Collaboration item and verify no potentially consequential step is replayed.
7. Provide an exact registered `functionName`, including an optional-`negativePrompt` image tool. Confirm the curator receives the registry's actual required/optional parameter names, not the AI's guesses. Where possible use another qualified model; when no independent curator exists, obtain the bounded human handoff.
8. Regression check normal chat, Council, Goal Execution, image generation with omitted/null `negativePrompt`, persistence/seed reconciliation for existing users, runtime DX function discovery, and request approval semantics.

## Verification limitations

No .NET SDK, `dotnet build`, Windows PowerShell or `pwsh` was used. The real Windows build error and running UI cannot be marked as tested here, even though the source-level cause was identified and patched. No GitHub access was used.
