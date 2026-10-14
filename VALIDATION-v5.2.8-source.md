# LocalGPT 5.2.8 source validation

## Scope

Build-storage bootstrap/preflight repair only. Runtime application behavior is unchanged from 5.2.7.

## Confirmed source checks

- Repository build storage defaults to `<repository>/artifacts/.build-storage` without requiring environment configuration.
- `.gitignore` explicitly ignores `artifacts/.build-storage/` and the parent `artifacts/` output tree.
- The PowerShell compatibility guard searches `$documentationToolCacheRoot` as literal source text and therefore does not resolve an unset maintenance-script variable under StrictMode.
- `build/audit_repository_build_storage.py` checks these properties and passes.
- Active LocalGPT version/cache-buster identity is aligned at **5.2.8**.

## Environment limitation

`pwsh` and the .NET SDK are not used in this preparation environment. No PowerShell runtime build, dotnet restore/build/publish, documentation render, notarization, or native package build is claimed.
