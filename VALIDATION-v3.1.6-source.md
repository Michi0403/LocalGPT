# LocalGPT 3.1.6 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.6;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0` and the existing DevExpress 25.2 package lane is retained;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- `/chat` renders exactly three quick service-backed DevExpress selectors for Council team, Council model preset and hardware performance preset;
- quick selectors delegate to the existing detailed Chat configuration selection/application paths instead of duplicating preset semantics;
- quick-selector markup remains Blazor-owned and is not transplanted into DevExpress internal DOM by JavaScript;
- opening Chat configuration independently refreshes Council teams, model presets, performance presets, persistent prompt starters, projects and chat-memory lists from their services;
- provider discovery retains its existing refresh path and no longer gates the other configuration refreshes;
- service-backed refresh preserves current manual configuration and selected IDs/keys when those rows still exist;
- an interlocked gate prevents concurrent duplicate configuration refresh passes;
- 9,431 direct maintained C# declarations across 624 files pass XML documentation coverage/quality validation;
- 45 Razor component types and 752 direct Razor `@code` declarations pass Razor XML documentation coverage/quality validation;
- async continuation, service resilience, application architecture and configurable Council behavior static audits pass in this source-only environment;
- the 3.1.5 provider-stream repetition watchdog policy audit remains applicable and passes.

The packaged ZIP is extracted into a fresh directory and the 3.1.6 release audit, quick-configuration audit and XML documentation audit are rerun from the packaged source before handoff.
