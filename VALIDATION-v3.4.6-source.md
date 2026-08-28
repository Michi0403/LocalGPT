# LocalGPT 3.4.6 source validation

This handoff was validated statically because the maintenance environment does not contain .NET or PowerShell. No GitHub access was used.

Validated in the supplied 3.4.5 source baseline after the narrow repair:

- `build/audit_service_resilience.py`: passed; 2,188 non-iterator service methods own try/catch + diagnostics, with 29 existing yield methods skipped for the separate iterator policy and three boot methods excluded by policy.
- `build/audit_cross_platform_boundaries.py`: passed; 22 checks, no platform leaks.
- `build/audit_application_architecture.py --mode all`: passed.
- `build/audit_async_continuations.py`: passed for 259 source files.
- `Directory.Build.targets` parses as XML; active guards no longer carry `Windows_NT` execution gates and select `powershell`/`pwsh` only as the host command.
- LocalGPT Python-backed guard launchers probe `python3` between `python` and the Windows `py` launcher, so the same syntax-aware architecture/async/service checks remain available on macOS/Linux.
- The newly changed Ollama platform service contains no `yield` statement, so it no longer creates an unreviewed iterator-policy entry.
- Debug documentation still runs but defaults `RequireLocalGptDocumentationPdf=false`; Release defaults it to true, and `Build-Release.ps1` retains its explicit one-time `-RequirePdf` documentation build.
- Documentation browser discovery has an initialized LocalApplicationData value and keeps Windows, macOS, Linux/PATH browser probes with a 1,500-page fast-print ceiling.
- Shared Node resolution returns an existing Node.js runtime meeting the minimum before provisioning is considered.
- The DocFX console renderer suppresses redraw-only file counters and bar-only records and de-duplicates PDF percentages while retaining captured raw diagnostics.

A native .NET/PowerShell build was not claimed or performed in this environment. The next authoritative validation is the user build on Windows/macOS/Linux.
