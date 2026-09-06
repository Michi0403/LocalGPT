# LocalGPT 3.8.5 source validation

This source release repairs the common ASP.NET Core startup lifecycle that could leave the process alive after middleware configuration without ever publishing a listening LocalGPT HTTP endpoint.

## Release-specific invariants

- Exactly one LocalGPT application `AddHostedService` registration remains: `LocalGptPostListenHostedServiceCoordinator`.
- Database initialization, Remote Control polling, runtime-capability synchronization, DX AI-function catalog synchronization, and all four 1-Wire workers are registered as concrete singleton workers, not direct host-startup services.
- The coordinator waits for `IHostApplicationLifetime.ApplicationStarted` before resolving any of those worker instances.
- The coordinator starts the maintained workers only after the web listener is online and stops the workers it started during shutdown.
- Runtime `server.json` publication remains attached to `ApplicationStarted`; no launcher-visible endpoint is intentionally published before Kestrel startup.
- Port resolution, dynamic macOS `--port 0`, Windows/default port 5000, per-user storage paths, provider onboarding, notarization state, and InteractiveServer routing are otherwise preserved.

## Environment limitation

The source can be statically audited in this environment, but this environment does not provide the .NET SDK/runtime, Windows WebView2, or macOS runtime needed to execute the packaged application. Runtime acceptance still requires the built application to show `Now listening` / `LocalGPT listening on ...` and return `/health` on the selected loopback port.
