# LocalGPT 4.0.5 source validation

This is a source/static validation. No `dotnet build`, `dotnet test`, `dotnet publish`, native Windows installer execution, or native macOS release build was run in this environment.

Validated source contracts include: version 4.0.5 on maintained version surfaces; durable application/bootstrap logging; separate durable setup transcript; macOS application-log routing under the LocalGPT user-data root; preserved runtime/source identity and alternate-host `server.json` rendezvous logic; macOS distribution-PKG readback guards; Ollama install/update and console progress behavior; provider-model resolution; Council/provider policies; render-mode ownership; architecture/cross-platform boundaries; async continuation policy; service resilience; and XML/Razor documentation coverage.
