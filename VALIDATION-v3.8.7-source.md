# LocalGPT 3.8.7 source validation

This source package is a bounded startup repair based directly on LocalGPT 3.8.4.

Validated statically in the packaging environment:

- all eight pre-existing LocalGPT `AddHostedService<T>` registrations are present;
- no `LocalGptPostListenHostedServiceCoordinator` exists;
- `DatabaseInitializationService` has no `IServiceActivityService` dependency;
- `DatabaseMigrationCompatibilityService` has no `IServiceActivityService` dependency;
- the runtime-policy constructor initializes from the authoritative built-in seed and does not call `Reload()`;
- persisted runtime policy is reloaded by `DatabaseInitializationHostedService` only after database initialization completes;
- LocalGPT 3.8.3 provider onboarding/package-knowledge markers remain present;
- LocalGPT 3.8.4 cross-platform per-user path/storage markers remain present;
- exact `@rendermode InteractiveServer` routed/island architecture remains preserved;
- broad repository architecture, cross-platform, async, service-resilience, provider/Council and configurable-behavior audits are rerun before packaging;
- final ZIP contains no repository-local `bin`, `obj`, `__pycache__` or `.pyc` build state and is integrity-tested.

This environment does not provide `dotnet` or `pwsh`, so a compiled/runtime launch cannot be claimed here. The source repair specifically removes the statically proven boot dependency cycle instead of changing hosted-service ownership.
