# LocalGPT 2.3.9 — service resilience diagnostics

This source repair keeps the 2.3.8 code-generation, function-recovery, path-explorer, Kawaii documentation, provider, Council, project, knowledge, and deployment work intact.

## Restored diagnostics boundaries

- Every parsed method under `src/LocalGPT/Services` now owns a `try/catch` and a diagnostic write unless the method contains `yield` or is one of the small boot methods called directly from `Program.cs`.
- Existing `ILogger` instances are used where the service already owns one. A fully-qualified `System.Diagnostics.Trace.TraceError` fallback is used only for service types that deliberately have no injected logger, avoiding constructor/DI changes.
- `OperationCanceledException` is logged at debug level when an `ILogger` is available; other failures are logged as errors.
- Exceptions are rethrown after diagnostics. This prevents silent partial state changes; controller/component/worker recovery boundaries remain responsible for deciding whether an operation can continue.
- `build/audit_service_resilience.py` makes this broad policy reviewable and is called from `Assert-MethodDiagnostics.ps1` when Python is available.

## Browser diagnostics

- The LocalGPT documentation viewer now uses the same guarded JavaScript diagnostics shape as PublisherStudio.
- Reconnect helpers and Kawaii documentation functions own logged JavaScript error boundaries; existing global callback/error guards remain in place.

## Versioning

The application, WebView wrapper, and installer source versions are 2.3.9. The checked-in generated 2.3.7 DocFX tree/PDF is not relabeled: the owner-side documentation build receives the real version and regenerates the site.
