# LocalGPT 6.1.8 source-only validation

- Starting archive: `LocalGPT-6.1.7-source.zip`.
- Checked 27/27 maintained browser JS assets against the source diagnostics manifest using the same CRLF/CR-to-LF normalized SHA-256 algorithm as the PowerShell guard; exact source/inventory equality.
- `GoalExecutionService`, all partial implementation files and `GoalExecutionLedgerService`: no new application-static runtime helper declarations.
- `node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js`: passed.
- Version 6.1.8 and single-digit component convention: passed.
- The PowerShell build/architecture guard implementations are unchanged from the starting archive.
- **Unavailable:** Windows PowerShell 5.1, PowerShell 7 and .NET; no PowerShell gates or .NET builds were executed. Do not treat these checks as proof of a passing Visual Studio build.

After extraction, a Windows source check is the existing `build/Assert-ApplicationStaticPolicy.ps1` and `build/Assert-JavaScriptDiagnostics.ps1` via `Directory.Build.targets`, followed by an ordinary Debug build. The previously unreviewed `localgpt-game-console.js` path now has an explicit reviewed compatibility source in this archive.
