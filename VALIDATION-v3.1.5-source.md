# LocalGPT 3.1.5 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.5;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0` and the existing DevExpress 25.2 package lane is retained;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- the repetition watchdog has a bounded rolling text window and conservative multi-sample periodic-token criteria;
- watchdog exception messages omit repeated provider payload text;
- benchmark watchdog cancellation is request-local and retries the same provider-qualified subject instead of substituting another model;
- `SystemBenchmarkCalibration` derives repetition retries from the existing Social Team member-failure recovery mode/count;
- a disabled configured member-failure recovery mode produces zero benchmark repetition retries;
- exhausted benchmark repetition recovery is retained as failed measurement evidence and does not abort the enclosing host queue;
- normal Council participant streaming, role-compliance recovery and final-answer recovery are all watchdog-protected;
- normal Council watchdog failures continue through the existing same-member and configured round-member recovery path rather than adding a parallel scheduler;
- caller cancellation and round-skip cancellation remain separate from watchdog failure;
- the 3.1.4 XML documentation completeness tooling remains active;
- 9,895 direct maintained C# declarations across 632 files pass XML documentation coverage/quality validation;
- 45 Razor component types and 752 direct Razor `@code` declarations pass Razor XML documentation coverage/quality validation;
- async continuation, service resilience and application architecture static audits pass in this source-only environment.

The packaged ZIP is also extracted into a fresh directory and the 3.1.5 release audit plus XML documentation audit are rerun from the packaged source before handoff.
