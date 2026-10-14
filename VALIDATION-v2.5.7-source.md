# LocalGPT 2.5.7 source validation

This validation is intentionally source/static only. No `dotnet`, MSBuild, restore, build, publish or runtime compilation was executed.

## Repository audits executed

- `python build/audit_provider_qualified_council.py --root .`
  - Passed **118** provider-qualified Council / multi-host checks.
  - The gate now explicitly protects endpoint-qualified Install upserts, local Ollama fallback discovery, primary promotion without destructive replacement, Council current-catalog preflight, offline-host vs missing-model distinction, no same-name fallback, Chat stale-selection reconciliation, and the `OllamaCores` configuration surface.
- `python build/audit_application_architecture.py --root . --product localgpt --mode all`
  - Passed.
- `python build/audit_service_resilience.py --root . --product localgpt`
  - Passed for **1,724** service methods owning the required try/catch + diagnostic boundary; 30 yield methods and 3 direct Program/Startup methods were intentionally skipped by the maintained audit.
- `python build/audit_async_continuations.py --source-root src/LocalGPT`
  - Passed for **152** source files.
  - **2,240** await tokens reviewed.
  - **2,035** `.ConfigureAwait(false)` continuations.
  - **30** explicitly renderer-affine `.ConfigureAwait(true)` continuations.
  - 2 preconfigured awaitables, 171 reviewed await-using disposals and 2 configured async streams.
- `python build/audit_chat_ascii_console.py --root .`
  - Passed all **17** checks.
- `python build/audit_documentation_onewire_contracts.py --root .`
  - Passed.
- `python -m py_compile build/audit_provider_qualified_council.py`
  - Passed.

## Additional source checks executed

- Parsed `src/LocalGPT/appsettings.json` as JSON after adding the explicit empty `OllamaCores` registry.
- Parsed LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper project files and verified version `2.5.7`.
- Verified LocalGPT.WireProtocolVersion remains `2.1.1`.
- Parsed `Localization/en-US.json` and `Localization/de-DE.json`; both contain 1,496 keys and their key sets are identical.
- Verified all 13 maintained interactive pages still contain `@rendermode InteractiveServer`.
- Compared the 2.5.7 source tree against the delivered 2.5.6 tree; changes are limited to provider/Council runtime wiring, Install/Chat provider UI logic, the adaptive Ollama benchmark loopback resolver, provider configuration model/default JSON, provider-qualified Council audit, documentation, versions and this release metadata.
- Verified the Install discovery path no longer contains direct `Model.OllamaCore.Uri = host.Endpoint` or `Model.ChatGPTLocalCore.Endpoint = host.Endpoint` replacement.
- Verified exact Council preflight uses the current catalog and refuses same-name substitution across endpoints.
- Verified a configured but unreachable provider is distinguished from a reachable provider that no longer exposes the requested model.
- Verified a refresh that removes stale provider-qualified selections cannot silently auto-select a replacement model in the same refresh.

## Not executed

- .NET restore
- .NET compile/build
- runtime launch
- browser automation against a compiled build
- publish/package generation through the repository PowerShell build pipeline

Those checks require the .NET environment that the delivery request explicitly excludes.
