# LocalGPT 3.1.8 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, DevExpress compile, build, publish, or EF migration command was available/executed while preparing this archive.

The real published 3.1.7 capture showed the quick selector dock rendered while the DevExpress input area was materially degraded. Inspection of the source isolated the regression to quick-feature CSS that changed the composer and textarea dimensions. The `DxAIChat` Razor subtree itself and the detailed Chat Configuration markup were already unchanged from the known-good 3.1.5 source.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.8;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0`; existing DevExpress 25.2 package lane remains unchanged;
- the Chat Configuration markup hash remains `cc8adbf3a5e57a225a4754043eb779610baecd1608b0114e9645cb52cb5dcc54`, identical to 3.1.5;
- the full `DxAIChat` subtree hash remains `ae8a6d9cec66907f073f94c4b83a939e44b209ccaadfa5b0527c2a0386c26b54`, identical to 3.1.5;
- the entire `Chat.razor.css` content before the 3.1.8 quick-dock section is restored to the 3.1.5 hash `3bc9693f026e410de1cd03c24544ab5695f58a13d238bc9710498eab6e090ad1`;
- the 3.1.8 quick CSS does not target `.localgpt-chat-composer`, `.localgpt-chat-textarea`, `.dxbl-chatui-submitarea`, `.dxbl-chatui-input`, `min-height`, or `padding-bottom`;
- all three quick `DxComboBox` selectors remain siblings after `DxAIChat` and retain the explicit typed callbacks that fixed the 3.1.6 compile failure;
- service-backed Chat Configuration refresh remains enabled when the configuration ribbon opens;
- refreshed teams, model presets, performance presets, prompt starters, memory and project data are fetched off-dispatcher and committed to component state through renderer-affine `InvokeAsync` calls;
- provider-stream repetition watchdog and Council round recovery/failover remain present;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- XML documentation completeness remains enforced for maintained C# and Razor source.

The packaged ZIP is extracted into a fresh directory and the 3.1.8 release audit, quick-configuration isolation audit, XML documentation audit, async continuation audit, service resilience audit, application architecture audit, configurable behavior audit and provider stream repetition policy audit are rerun from packaged source before handoff.
