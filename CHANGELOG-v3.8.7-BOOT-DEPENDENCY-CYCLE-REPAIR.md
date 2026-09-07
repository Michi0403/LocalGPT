# LocalGPT 3.8.7 - boot dependency cycle repair

## Problem

LocalGPT could finish building the `WebApplication` and configuring endpoints but remain alive without Kestrel ever becoming reachable on `/health` or the Blazor frontend. The failure reproduced on both Windows and macOS, which ruled out a platform-specific launcher defect.

The startup graph contained a hidden singleton cycle:

`DatabaseInitializationHostedService -> IDatabaseInitializationService -> IServiceActivityService -> ComponentActivityService -> ILocalGptRuntimePolicyDataService -> ILocalGptRuntimePolicyStoreService -> IDatabaseInitializationService`

`IServiceActivityService` is registered through a factory resolving `ComponentActivityService`, so the cycle is opaque to normal service-provider build validation. It can therefore surface only when hosted services are resolved during host startup, after `builder.Build()` has already succeeded.

## Repair

- Restored and preserved the ordinary ASP.NET Core hosted-service model. All eight application workers remain direct `AddHostedService<T>` registrations.
- Removed `IServiceActivityService` from `DatabaseInitializationService` and `DatabaseMigrationCompatibilityService`. Boot-critical database work now uses its own existing structured logger diagnostics instead of entering UI/service activity memory.
- Removed synchronous database loading from the `LocalGptRuntimePolicyDataService` constructor.
- Runtime policy now creates its initial immutable state from `ILocalGptRuntimePolicySeedDataService`, which is the same authoritative seed used for deterministic database initialization.
- `DatabaseInitializationHostedService` reloads persisted runtime-policy values after migration/seeding completes. If persisted reload fails, the valid built-in seed remains active and the web host stays available.
- No custom post-listen coordinator, manual hosted-service `StartAsync`, or release-time startup architecture is introduced.
- The 3.8.3 provider onboarding, Ollama/LM Studio discovery and packaged knowledge repairs remain intact.
- The 3.8.4 Windows/macOS/Linux per-user storage/path contract remains intact.
- macOS dynamic-port launcher behavior and Windows default port compatibility remain unchanged.

## Expected startup boundary

A normal launch should no longer enter the database/runtime-policy cycle while the host resolves its hosted services. Kestrel can complete normal host startup, after which background database initialization seeds/reconciles the database and reloads persisted runtime policy.
