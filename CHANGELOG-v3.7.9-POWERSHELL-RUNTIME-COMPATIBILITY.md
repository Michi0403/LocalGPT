# LocalGPT 3.7.9 - PowerShell runtime compatibility

## Fixed

- Replaced the PDF cache completeness check's `String.Contains(value, StringComparison)` call with `String.IndexOf(value, comparison)`, which works on Windows PowerShell 5.1/.NET Framework and modern pwsh.
- Removed direct `System.IO.Path.GetRelativePath` calls from maintained PowerShell scripts. Portable helpers use the modern method through reflection when available and a URI-based fallback otherwise.
- Removed direct `ProcessStartInfo.ArgumentList` usage from browser PDF launching. Modern runtimes still use `ArgumentList` through reflection; Windows PowerShell 5.1 receives a correctly quoted `Arguments` string fallback.
- Removed direct `Process.Kill(true)` usage. The browser timeout path uses the process-tree overload through reflection when available and falls back to `Kill()` on Windows PowerShell 5.1.
- Extended `Assert-PowerShellCompatibility.ps1` so these runtime API regressions fail during the initial preflight instead of after documentation/build work.

## Preserved

- 3.7.8 release orchestration resilience, resumable Edge chunk printing, PDF cache validation, compression paths, notarization recovery/resume, signing, packaging, and InteractiveServer behavior are unchanged.
- No .NET build output or generated release artifacts are required by this source-only repair.

## Version

- LocalGPT advanced from 3.7.8 to 3.7.9. Minor and patch slots remain single digit.
