# LocalGPT 3.1.7 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

The user-provided .NET build of 3.1.6 identified three `CS1503` errors in `Chat.razor` where untyped quick-selector method groups could not be converted to the DevExpress `EventCallback` parameter. 3.1.7 changes those three bindings to explicit typed lambdas while preserving the existing handlers and service-backed behavior.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.7;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0` and the existing DevExpress 25.2 package lane is retained;
- all three `/chat` quick `DxComboBox` selectors use explicit typed `ValueChanged` lambdas;
- the three compile-failing untyped method-group forms are absent;
- quick selector handlers still delegate to the same Council-team, model-preset and performance-preset application paths;
- Chat Configuration live service refresh behavior from 3.1.6 remains intact;
- provider-stream repetition watchdog and Council failover/recovery remain intact;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- 9,905 direct maintained C# declarations across 632 files pass XML documentation coverage/quality validation;
- 45 Razor component types and 752 direct Razor `@code` declarations pass Razor XML documentation coverage/quality validation;
- async continuation, service resilience, application architecture and configurable Council behavior static audits remain applicable.

The packaged ZIP is extracted into a fresh directory and the 3.1.7 release audit, typed quick-selector callback audit, XML documentation audit and applicable architecture/resilience audits are rerun from the packaged source before handoff.
