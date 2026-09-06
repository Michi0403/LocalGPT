# LocalGPT 3.8.6 — startup guard parity repair

## Repair

- Keeps the 3.8.5 post-listen startup architecture: Kestrel/Blazor reaches `ApplicationStarted` before LocalGPT resolves or starts its non-HTTP application workers.
- Repairs the mandatory operational-diagnostics build guard that still asserted the pre-3.8.5 direct `AddHostedService<DatabaseInitializationHostedService>` registration.
- The guard now requires `LocalGptPostListenHostedServiceCoordinator`, all eight preserved worker registrations as concrete singletons, the `ApplicationStarted` lifecycle boundary, and coordinator resolution/start of each worker.
- The guard explicitly rejects direct `AddHostedService<T>` registration for the eight application workers so they cannot silently return to the HTTP-host startup critical path.
- Adds a fail-fast startup smoke test immediately after the RID-neutral LocalGPT build and before documentation generation. It launches the freshly built assembly on a temporary loopback port with a temporary database and requires `/health` to answer within 45 seconds.
- No provider, Council, 1-Wire, persistence, path, UI, signing, notarization, or documentation-rendering feature is removed by this repair.

## Why

3.8.5 could not pass the authoritative LocalGPT build on macOS because `Assert-OperationalDiagnostics.ps1` still required the old database hosted-service registration. The source-specific 3.8.5 audit and the repository build guard had contradictory architecture expectations. 3.8.6 makes those checks agree instead of bypassing either one.
