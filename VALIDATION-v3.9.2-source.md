# LocalGPT 3.9.2 source validation

This patch is prepared under the existing source-only constraint: no `dotnet` build/publish and no PowerShell release build are run in this environment.

## Runtime failure addressed

The supplied installed-app log shows the Setup Assistant failing before its form can initialize. `HardwareInventoryService.RunProbeAsync()` reaches `Process.Start`, which calls `Interop.Sys.GetCwd()` and throws `FileNotFoundException`. That failure propagates through `ProbeNvidiaAsync`, `GetHardwareAsync`, `InitialSetupAssistantService.BuildHardwareListAsync`, `GetSnapshotAsync`, and finally `InitialSetupAssistantPanel.RefreshAsync`.

The same log shows `FileLogger` independently calling the invalid current directory while DevExpress creates a component logger. That unhandled exception kills the Blazor circuit. It also shows attempts to write `LocalGPT.log` below `/Applications/LocalGPT.app/Contents/Resources/app`, which is not an appropriate mutable log location for a signed installed app.

The saved Setup HTML confirms the visible result: the Setup guide contains the `AI-guided local runtime setup` component, but it displays `Initial setup refresh failed. Review LocalGPT logs.` followed by `Loading local setup state…` instead of the interactive form.

## 3.9.2 repair checks

The source repair ensures:

- the macOS launcher changes directory to the per-user LocalGPT runtime directory before launching the absolute packaged executable;
- startup repairs an already-invalid inherited current directory while leaving valid development/source working directories unchanged;
- `FileLogger` never calls `Directory.GetCurrentDirectory()` and defaults to the per-user LocalGPT logs directory;
- explicit file-log paths inside the application bundle are redirected away from the read-only bundle;
- hardware probe child processes always receive an explicit durable working directory;
- missing/failed optional NVIDIA and platform GPU probes return an empty optional result instead of aborting the Setup snapshot;
- `InitialSetupAssistantService.GetSnapshotAsync()` keeps provider/Ollama/model/recommendation controls alive if optional hardware discovery still fails;
- the bounded console uses the durable LocalGPT runtime directory when a request leaves its working directory blank, covering provider and model install commands;
- Ollama process start/restart no longer falls back to `Environment.CurrentDirectory`;
- the unified Setup component still contains Start/Stop/Restart Ollama, endpoint registration, CanIRun.ai recommendation fetch, direct model-ID install, and per-recommendation Install model actions;
- the Setup Razor component no longer applies its own 24/32-item display truncation, so every recommendation/model choice returned by the maintained bounded service policy remains visible and individually actionable.
- the CanIRun parser no longer multiplies an `Int32.MaxValue` recommendation limit and overflow-stops after the first parsed object; its existing 512-object JSON traversal bound remains the safety ceiling;
- provider model-choice mapping no longer adds a separate 96-item truncation after CanIRun results are returned.

## Static regression boundary

The final tree is checked with the release-specific Python audit plus the repository's non-version-specific architecture, cross-platform, async, DXFunction, provider/Council, SQL-seed, service-resilience, and XML-documentation audits. The maintained PowerShell guard predicates are also reproduced by the release audit so the 3.9.1 ownership/system-variable build repairs remain enforced without weakening their baselines.

Passing those checks is source/static validation, not a claim that the package was built in this environment. The next authoritative package result is the normal macOS `pwsh Build-Release.ps1` run.

## Static checks completed

The final source tree passed these maintained checks in this environment:

- `build/audit_release_3_9_2.py`: passed.
- `build/audit_application_architecture.py --root . --product localgpt --mode all`: passed.
- `build/audit_cross_platform_boundaries.py`: 22 checks passed.
- `build/audit_async_continuations.py --source-root src/LocalGPT`: 259 source files; 3,019 await tokens; continuation policy passed.
- `build/audit_chat_ascii_console.py --root .`: 17 checks passed.
- `build/audit_codegen_dxfunction_wiring.py`: passed.
- `build/audit_configurable_behavior_policy.py`: passed.
- `build/audit_council_sql_seed.py`: 60 executable `INSERT OR IGNORE` rows; deterministic hashes and second-run idempotency passed.
- `build/audit_provider_qualified_council.py --root .`: 282 checks passed.
- `build/audit_provider_stream_repetition_policy.py`: passed.
- `build/audit_service_resilience.py --root . --product localgpt`: 2,209 service methods passed the maintained resilience policy.
- `build/audit_xround_wiring.py`: passed.
- `build/Assert-XmlDocumentationCoverage.py .`: 10,333 direct C# declarations and 784 Razor members passed documentation coverage/quality.
- Extracted generated macOS launcher: `/bin/sh -n` passed.
- `@rendermode InteractiveServer`: 15 boundaries retained.
- `AddHostedService<T>`: eight registrations retained.

No .NET build/publish result is claimed by this file.
