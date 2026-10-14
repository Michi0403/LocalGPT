# LocalGPT 3.3.2 source validation

This handoff is source-only. The preparation environment does not provide PowerShell (`pwsh`) or the .NET SDK, so no restore, compile, publish, PowerShell execution, application launch or runtime test is claimed. No GitHub repository access was used.

## Source assertions completed before packaging

A source-side validation pass completed **42/42 assertions** successfully. It checks that:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are all version **3.3.2**;
- the DevExpress initialization and registration helpers no longer assign to a variable named `isWindows`/`IsWindows`;
- both helpers use the repository-owned `runningOnWindows` variable;
- the PowerShell compatibility validator rejects assignments to `IsWindows`, `IsLinux`, `IsMacOS` and `IsCoreCLR` regardless of casing;
- `Build-Release.ps1` executes the PowerShell compatibility guard before the DevExpress license preflight;
- all 3.3.1 cross-platform build/install service wiring remains present;
- the Windows wrapper retains `EnableWindowsTargeting=true`;
- normal NuGet configuration does not require a repository-local `./packages` source;
- all localization JSON files parse;
- project/NuGet XML parses;
- the Razor `@rendermode` ownership map remains identical to the supplied LocalGPT 3.3.0 source;
- no DevExpress license file or obvious personal license payload is included in the source archive;
- no merge-conflict markers are present in maintained source text.

## Developer-machine verification still required

Run `pwsh ./Build-Release.ps1` or `pwsh ./Build-LocalDevelopment.ps1` on the target developer machine. The reported PowerShell 7 automatic-variable collision should now be removed; any later build error should be treated as the next independent build/runtime issue.
