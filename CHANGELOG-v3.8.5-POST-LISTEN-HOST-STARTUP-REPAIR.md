# LocalGPT 3.8.5 — post-listen host startup repair

## Why this release exists

LocalGPT could finish builder/service/middleware configuration on Windows and macOS but never reach a listening HTTP endpoint. The browser then opened or waited on the correct configured port while Kestrel itself remained offline. Because the same failure occurred on both operating systems, the defect was in the common LocalGPT host lifecycle rather than the platform launchers.

The original 3.6.3 intent was already correct: database initialization and catalog work must not hold Kestrel offline. The source later accumulated additional application workers under direct `AddHostedService` registration, so the host still had to construct/start those worker graphs inside `app.StartAsync()` before `ApplicationStarted` could be published.

## Repair

- All eight non-HTTP application workers remain implemented and registered, but they are now concrete singleton services rather than direct host-startup services:
  - database initialization,
  - Remote Control polling,
  - runtime-capability synchronization,
  - DX AI-function catalog synchronization,
  - 1-Wire TCP,
  - 1-Wire discovery,
  - 1-Wire approval processing,
  - 1-Wire work processing.
- `LocalGptPostListenHostedServiceCoordinator` is the only LocalGPT application `AddHostedService` registration.
- The coordinator has a deliberately light constructor and does not resolve any worker dependency graph before `IHostApplicationLifetime.ApplicationStarted` is signaled.
- After the listener is online, the coordinator resolves and starts the existing workers in their maintained order.
- On shutdown it stops only the workers it actually started, in reverse order, under a bounded stop budget.
- A worker startup failure is logged after the HTTP frontend is already available instead of preventing the local listener from appearing.
- The existing runtime endpoint file remains owned by the `ApplicationStarted` callback, so the macOS launcher and Windows wrapper only receive a runtime URL after ASP.NET Core reports the listener started.

## Preserved behavior

- LocalGPT 3.8.4 per-user storage/path contract remains unchanged.
- Windows default application state remains `%LOCALAPPDATA%\\LocalGPT`.
- macOS and Linux user-data behavior remains unchanged from 3.8.4.
- Ollama/LM Studio onboarding and model-seed repairs from 3.8.3 remain unchanged.
- Kestrel port selection remains unchanged: Windows/default launches can use port 5000 while `--port 0` selects a free loopback port for the macOS launcher.
- 1-Wire, database initialization, Remote Control, runtime capability synchronization, and DX-function synchronization were not removed; only their startup boundary changed.
- Blazor `InteractiveServer` routing remains unchanged.
