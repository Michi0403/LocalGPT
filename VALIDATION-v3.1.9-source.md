# LocalGPT 3.1.9 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, DevExpress compile, build, publish, or EF migration command was available/executed while preparing this archive.

The real published 3.1.8 capture confirmed that Chat itself was working again and that Council-team/model-preset/performance-preset data wiring selected the expected values, but the quick selectors were still visually misplaced as a vertical/overlay surface in the middle of the Chat viewport. 3.1.9 changes only their structural placement plus the minimum existing grid-row count necessary to give the new normal-flow sibling its own row.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.9;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0`; the existing DevExpress 25.2 package lane remains unchanged;
- the detailed Chat Configuration markup hash remains `cc8adbf3a5e57a225a4754043eb779610baecd1608b0114e9645cb52cb5dcc54`;
- the complete `DxAIChat` subtree hash remains `ae8a6d9cec66907f073f94c4b83a939e44b209ccaadfa5b0527c2a0386c26b54`;
- the quick surface appears after the closing Chat host and before the Running session tools ribbon;
- the quick surface contains exactly one explicit `<div>` and one `DxFormLayout` with exactly three `DxFormLayoutItem` children;
- Team, Models and Performance each use `ColSpanMd="4"`, allowing DevExpress FormLayout to provide the normal three-column desktop layout and responsive smaller-width layout;
- the three typed `DxComboBox` callbacks from 3.1.7 remain intact;
- no `.chat-quick-configuration-bar` or `.chat-quick-configuration-item` CSS exists in `Chat.razor.css`;
- no selector-specific absolute positioning, fixed widths, custom overflow scrollbar or composer geometry is introduced;
- only the existing main-grid row count and optional ASCII-game grid row count/session row index are changed to host the new normal-flow sibling;
- when those permitted grid-row changes are normalized back, `Chat.razor.css` hashes to the known-good pre-feature value `3bc9693f026e410de1cd03c24544ab5695f58a13d238bc9710498eab6e090ad1`;
- service-backed Chat Configuration refresh remains enabled and renderer-affine state commits remain unchanged;
- provider-stream repetition watchdog and Council recovery/failover remain present;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- XML documentation completeness remains enforced for maintained C# and Razor source.

The packaged ZIP is extracted into a fresh directory and the 3.1.9 quick-row audit, release audit, XML documentation audit, async continuation audit, service resilience audit, application architecture audit, configurable behavior audit and provider-stream repetition policy audit are rerun from the packaged source before handoff.
