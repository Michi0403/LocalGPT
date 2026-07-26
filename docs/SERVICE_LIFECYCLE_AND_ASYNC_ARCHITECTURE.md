# Service lifecycle and asynchronous execution architecture

## Runtime services are DI instances

LocalGPT runtime behavior belongs to singleton, scoped, transient, or hosted services selected by ownership and concurrency requirements. A class ending in `Service`, `Client`, `Registry`, or `Runner` must not be a static class.

Static code is limited to:

- extension methods;
- pure helpers in an explicitly named `Helpers` boundary;
- immutable constants and generated regex accessors;
- framework entry points;
- security invariants that own no runtime state.

A static class is not automatically stateless. Process-wide caches, mutable collections, active session state, formatter state, database state, provider state, UI state, or cancellation state are forbidden in static code. Shared extension/name catalogs must be immutable (`FrozenSet<T>`, immutable arrays, or equivalent) or owned by an injected service instance.

## Lifetimes

- **Singleton**: only when the implementation is thread-safe and owns process-wide coordination or immutable catalogs.
- **Scoped**: circuit, request, chat session, EF consumer, theme selection, council workflow, and user-facing mutable state.
- **Transient**: small stateless adapters when independent construction is useful.
- **Hosted service**: startup or background lifecycle coordinated by the host.

Services must be resolved through DI. Components, controllers, and services must not manually construct `ThemeService`, database bootstrap services, application-activity services, or supervised task runners.

## Logging and bounded short-term awareness

High-level service boundaries use constructor-injected `ILogger<T>`. They should either:

1. log and record bounded service activity in a local `try/catch` that rethrows; or
2. execute through `IServiceActivityService.RunAsync`.

`IServiceActivityService` records only the service name, operation, status, and a short sanitized summary. It never stores prompts, responses, uploaded content, generated source, SQL values, approval parameters, secrets, or full exception text. Exceptions remain in the configured technical logger and are rethrown to the boundary that owns recovery and user notification.

Low-level EF materialization, pure mapping, formatting, and hot-path helpers may propagate exceptions without per-method logging when their high-level caller already owns the operation boundary. Duplicate logging at every stack frame is not a quality improvement.

## Await every operation unless concurrency is intentional

A returned `Task` or `ValueTask` must be awaited, returned, or passed to `ISupervisedTaskRunner`. Explicit task discards such as `_ = SomeAsync()` are forbidden.

`ISupervisedTaskRunner` is reserved for work that must continue without blocking the current UI event, such as:

- route-boundary recovery after navigation events;
- collaboration refresh after a service event;
- bounded autosave loops.

The runner tracks each task, observes exceptions, records cancellation/failure, and accepts an owner-lifetime cancellation token. It is not permission to start autonomous work. Event subscribers must unsubscribe before disposal and guard the small race where a final event reads a cancellation source while the circuit is closing.

## Theme operations

Every `IThemeChangeService.SetTheme` call is asynchronous and must be awaited. A selected-theme failure attempts one awaited rollback to the previous DevExpress `ITheme`. Rollback failure is logged separately and the UI asks the user to reload rather than pretending restoration succeeded.

## Database initialization separation

`DatabaseInitializationService` coordinates health checks, compatibility reconciliation, EF migration, and seeding. `DatabaseMigrationCompatibilityService` owns schema inspection, compatibility backup, verified migration-history adoption, and stale-lock handling. This separation prevents one oversized static-heavy bootstrap service and makes the compatibility boundary independently testable.

## Validation

`build/Assert-ServiceArchitecture.ps1` rejects:

- static runtime service/client/registry/runner classes;
- static classes under `Services` that are not approved pure helpers;
- manually constructed core services;
- explicit discarded asynchronous calls;
- unawaited DevExpress theme changes;
- the migration-signature `Count` method-group regression;
- missing supervised-task wiring for the maintained event-driven components.
