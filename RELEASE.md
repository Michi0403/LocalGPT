# LocalGPT 3.8.6

LocalGPT 3.8.6 is a narrow build/startup parity repair on top of 3.8.5. It preserves the post-listen application-worker lifecycle while updating the mandatory operational-diagnostics build guard to validate that lifecycle instead of requiring the old direct database hosted-service registration.

Kestrel/Blazor remains the startup authority. `LocalGptPostListenHostedServiceCoordinator` is the only direct application hosted service; database initialization, Remote Control polling, runtime-capability synchronization, DX AI-function catalog synchronization, and the four 1-Wire workers remain present and are resolved/started only after `ApplicationStarted`.

The 3.8.3 provider-onboarding repairs and 3.8.4 per-user storage/path contract remain included. Windows continues to default to `%LOCALAPPDATA%\LocalGPT`, macOS to the current user's Application Support location, and Linux to `XDG_DATA_HOME/LocalGPT` or `~/.local/share/LocalGPT`. Provider/tool discovery remains separate from application-owned mutable data.

See `CHANGELOG-v3.8.6-STARTUP-GUARD-PARITY-REPAIR.md` and `VALIDATION-v3.8.6-source.md`.
