# LocalGPT 3.1.0 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or pack operation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole, and LocalGPTWebviewWrapper versions are 3.1.0;
- .NET SDK policy is 10.0.400 from the supplied upgraded source;
- SignalR, EF Core, System.CodeDom, and WebView cryptography dependencies retain the supplied 10.0.11 versions;
- DevExpress remains on LocalGPT's configurable `25.2.*` package lane rather than being downgraded or newly hard-pinned;
- the complete migration directory and database migration compatibility service are byte-identical to the user-supplied upgrade archive;
- 1-Wire protocol remains 2.1.1;
- the authored `docs/` DocFX/Kawaii source is restored because the supplied upgrade archive omitted it;
- the retained 3.0.9 benchmark/live-lane source audit, architecture audit, service resilience audit, and async continuation audit still pass after the version roll-forward;
- the tracked GitHub Pages snapshot was not manually rewritten without a .NET/DocFX build; the normal documentation target can regenerate the version-matched output from the restored authored source.
