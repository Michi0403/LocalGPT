# LocalGPT 4.0.4 source validation

This is a source/static validation release. No GitHub access and no `dotnet build`, `dotnet test`, `dotnet publish`, native macOS PKG execution, signing or notarization are performed in this environment.

Validation covers version surfaces, the PowerShell parser repair, source/runtime identity stamps, macOS lifecycle ownership, distribution-PKG construction, `pkgutil --expand-full` readback, Installer `-showChoicesXML` readability preflight, alternate `server.json` rendezvous preservation, Ollama update/download behavior, architecture policy, InteractiveServer ownership, async policy, service resilience, localization and XML/Razor documentation coverage.
