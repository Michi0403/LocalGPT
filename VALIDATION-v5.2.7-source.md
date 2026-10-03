# LocalGPT 5.2.7 source validation

## Scope

This release changes release-build storage ownership only. Runtime application behavior and the 5.2.5 transient UI-state repair are intentionally unchanged.

## Confirmed source checks

- `build/audit_repository_build_storage.py` passed. The full release and standalone documentation entrypoints initialize repository-owned build storage before heavy work; dotnet/NuGet/npm/XDG/documentation/temp state is redirected under the checkout by default; and `NodeRuntime.Common.ps1` prefers repository fallback storage before per-user application data.
- `build/audit_powershell_portable_paths.py` passed for 74 maintained PowerShell scripts.
- Application architecture, async continuation policy, async-only component/service architecture, Razor maintenance architecture, service resilience, and InteractiveServer transient UI-state ownership audits passed.
- The compatibility guard now carries source-level checks for the storage helper, required cache/temp variables, entrypoint ordering, documentation initialization, and fallback ordering.
- LocalGPT active version/cache-buster identity is aligned at **5.2.7**, preserving the single-digit minor/patch rule.

## Storage contract reviewed

`Build-Release.ps1` initializes `artifacts/.build-storage` unless `FUTURE2_BUILD_STORAGE_ROOT` is explicitly supplied. It redirects `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `NUGET_PLUGINS_CACHE_PATH`, `NUGET_SCRATCH`, `NPM_CONFIG_CACHE`, `XDG_CACHE_HOME`, `TMPDIR`, `TMP`, `TEMP`, and documentation cache ownership before restore/build/documentation/native packaging work. Standalone `Build-Documentation.ps1` initializes the same contract.

## Environment limitation

`pwsh`/Windows PowerShell and the .NET SDK are not available in this preparation environment. Therefore no PowerShell runtime execution, `dotnet build`, restore, publish, or native package build was claimed. The Mac/Linux host remains the authoritative runtime verification for the redirected storage environment.
