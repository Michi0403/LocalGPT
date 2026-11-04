# LocalGPT 6.9.1 — offline validation

Environment: Linux, PowerShell 7.5.4. No .NET SDK was reachable from this environment.

## Executed

All PowerShell guards that `Directory.Build.targets` wires into the normal build, run from the repository root exactly as MSBuild invokes them (`-NoProfile -NonInteractive -ExecutionPolicy Bypass -File build/<guard>.ps1`), once against unmodified 6.9.0 (baseline) and once against this tree.

| Result | 6.9.0 baseline | 6.9.1 |
|---|---|---|
| Build-wired guards passed | 36 / 36 | 36 / 36 |
| `Assert-CSharpSyntax.ps1` (not build-wired) | needs .NET SDK | needs .NET SDK |
| `Assert-XmlDocumentationCoverage.ps1` (not build-wired) | 2 findings | the same 2 findings |

The two XML-documentation findings exist unchanged in 6.9.0 (`Controller/CouncilRoleAssignmentsController.cs`, `Interfaces/ICouncilRoleAssignmentService.cs`) and are not touched by this release.

A first 6.9.1 candidate that wrapped the Council launch in `CouncilChatClient` in `Task.Run` failed `Assert-CouncilXRoundWiring` ("start the backend Council task before yielding the first browser update"). That change was reverted; the guard was not modified.

## Not executed

- C# compilation (Debug/Release), Roslyn syntax gate, analyzers.
- Any Windows, browser, Blazor circuit, Council start, Rejoin or installer runtime test.

Acceptance on Windows: start a Council from `/chat` with the same team/model preset as the failing 6.9.0 run. Expected: the Council introduction appears, no `TryClaimPendingStream` error in the browser console, and the server log shows `Finished enumerating build debug files.` **before** any circuit `connection went down`.
