# LocalGPT 6.7.6 — normal-build PowerShell diagnostics and automatic Council rejoin

## Corrected

- `Assert-PowerShellCompatibility.ps1` no longer interprets *quoted C# source-policy patterns* in `Assert-SessionSandboxIsolation.ps1` as executable PowerShell `.ArgumentList` usage. A PowerShell AST scan still rejects actual member access on older Windows PowerShell / .NET Framework.
- The full compatibility audit now runs as an unconditional LocalGPT `BeforeBuild` MSBuild target (except design-time IntelliSense) for **Debug, Release, Visual Studio Build/Rebuild, and command-line builds**. Release-only preflight remains in place.
- For failures, a machine-readable `file(line,column): error LGPS5101:` diagnostic is written so MSBuild and IDE error lists can point to the offending script, rather than only a late `Build-Release.ps1` exception. The existing fatal preflight is preserved.
- Browser-managed Council reconnect now automatically uses the existing **Reload & rejoin** URL if Blazor rejects the circuit or cannot reconnect, preserving the explicitly selected server-owned run identity. A bounded automatic reload budget (two reloads in two minutes), offline/hidden-tab checks, and retained manual actions prevent uncontrolled reload loops.
- Version advanced from 6.7.5 to 6.7.6; reviewed frontend SHA-256 manifest updated.

## Deliberate limits

- This restores the UI on a lost circuit. It **does not fix the framework exception** `CircuitHost.TryClaimPendingStream`: its originating .NET→browser stream must still be identified in live diagnostics. It also cannot reconstruct an unsaved, transient browser-only draft.
- No native `pwsh` or `dotnet` SDK was present in the packaging environment. A Windows/macOS/Linux build and a real live Council reconnect remain required acceptance tests.
- No architecture gates, runtime support, databases, migrations, Council execution semantics, Hyperlight providers, or team definitions were removed or weakened.
