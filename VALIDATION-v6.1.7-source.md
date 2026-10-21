# LocalGPT 6.1.7 source validation

- **Source:** supplied LocalGPT 6.1.6 ZIP; new version 6.1.7.
- Inspected `Build-Release.ps1` PowerShell variable-colon parser diagnostic and corrected `${Rid}` interpolation without changing the payload/version verification logic.
- Inspected static-application-state rule and replaced the new goal journal's `private static readonly JsonSerializerOptions` with an instance field.
- `GoalExecutionService.TrySemanticPlanAsync` XML documentation now includes `recoveryContext`.
- Refreshed newline-normalized SHA256 diagnostics manifest for every `.js` file under `src/LocalGPT/wwwroot/js` and component `.razor.js` script; the verifier remains unchanged.
- The reported `localgpt-game-console.js` is an extra file in the user's pre-existing checkout, not in the supplied source ZIP; the current `ChatGameConsole.razor.js` is included in the manifest. Clean extraction avoids the stale-file policy failure. Unknown local files remain subject to the strict validator.
- No .NET build, PowerShell parser, repository gates or runtime tests have been executed. These checks remain for the developer's native platform.
