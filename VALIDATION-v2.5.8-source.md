# LocalGPT 2.5.8 source validation

This validation is intentionally source/static only. No `dotnet`, MSBuild, restore, build, publish or runtime compilation was executed.

## Repository audits executed

- `python build/audit_provider_qualified_council.py --root .`
  - Passed **118** provider-qualified Council / multi-host checks.
- `python build/audit_application_architecture.py --root . --product localgpt --mode all`
  - Passed.
- `python build/audit_service_resilience.py --root . --product localgpt`
  - Passed for **1,725** service methods owning the required try/catch + diagnostic boundary; 30 yield methods and 3 direct Program/Startup methods were intentionally skipped by the maintained audit.
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

## Text-service ownership regression check

- Reproduced the matching and baseline logic from `build/Assert-TextServiceOwnership.ps1` against `src/LocalGPT/Components`, `Controllers` and `Controller`.
- Passed with **0 new direct string/regex ownership violations**.
- Verified the rejected `var preview = string.Join("; ", unavailable.Take(3));` statement no longer exists in `Chat.razor`.
- Verified the formatting now runs through the already injected `CouncilTextService`.
- `build/text-service-ownership-baseline.json` was intentionally not changed; the architecture guard remains authoritative.

## Additional source checks

- Parsed LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper project files and verified version `2.5.8`.
- Verified LocalGPT.WireProtocolVersion remains `2.1.1`.
- Confirmed the 2.5.7 multi-Ollama provider/Council audit still passes unchanged.
- Confirmed the new `CouncilTextService.ProviderUnavailableSelectionNotice(...)` owns try/catch + diagnostic logging and therefore passes the maintained service-resilience audit.
- Confirmed no new `ConfigureAwait(true)` site was added.

## Not executed

- .NET restore
- .NET compile/build
- runtime launch
- browser automation against a compiled build
- publish/package generation through the repository PowerShell build pipeline

Those checks require the .NET environment that the delivery request explicitly excludes.
