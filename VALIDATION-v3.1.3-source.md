# LocalGPT 3.1.3 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.3;
- 3.1.1 durable benchmark evidence and 3.1.2 machine-derived coverage truth remain intact;
- configured workflow steps persist explicit member/provider failure recovery mode and bounded recovery-turn count;
- recovery members are selected only from the existing Social Team role/provider pool and respect AssignedModelSingle exact identity plus distinct role assignment groups;
- recovery turns are preserved as new Council evidence rather than replacing failed steps;
- per-participant infrastructure faults are isolated so one host queue member cannot abort unrelated queued members;
- unexpected phase infrastructure failure is no longer silently swallowed;
- explicit user stop follows the caller-cancellation path instead of the generic Council failure path;
- expected component/transport cancellation logging no longer emits full exception stacks;
- live direct-user messages are rendered through authoritative Blazor/DevExpress session state instead of JavaScript shadow rows that were recreated during heartbeat renders;
- no EF migration or database compatibility source was changed;
- BenchmarkEvidence JSON schema remains unchanged;
- .NET 10 / DevExpress 25.2-line package state is carried forward;
- 1-Wire protocol remains 2.1.1.
