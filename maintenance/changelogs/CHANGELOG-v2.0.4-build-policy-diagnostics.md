# LocalGPT 2.0.4 — build-policy, diagnostics lifetime and seed reconciliation

- Corrected the architecture-audit PowerShell bridge so process output is displayed without being mistaken for the Python exit code.
- Restored the async-continuation, method-diagnostics, application-static and iterator policy targets in `Directory.Build.targets` after correcting their reported findings.
- Moved the newly introduced diagnostics batching, structured-text candidate and game-session state objects into `BusinessObjects`; service and diagnostics classes retain behavior only.
- Removed disposal ownership from `ServiceMethodLoggingDispatchProxy`; disposable services remain directly owned by dependency injection and are not proxied.
- Increased successful-operation batching bounds while retaining per-call Trace diagnostics, immediate cancellation/failure logging and aggregate timing data.
- Added logged exception boundaries to the new game, remote-import and DX parameter services and to their HTTP controllers.
- Moved Test Lab role/topic text normalization into `IRemoteKnowledgeImportService`.
- Replaced the remote HTML link iterator with a bounded list-producing method that logs and rethrows failures.
- Corrected renderer-affine and service-background `ConfigureAwait` policy findings and reviewed their baselines.
- Removed application statics from the structured-text translation path and routed dynamic regex compilation through `IRegexPatternService`.
- Added database seed concurrency reconciliation that reloads durable values, leaves concurrent user edits authoritative and saves unrelated additive records.
- Bumped the LocalGPT application version to 2.0.4. The 1-Wire protocol project is unchanged.
