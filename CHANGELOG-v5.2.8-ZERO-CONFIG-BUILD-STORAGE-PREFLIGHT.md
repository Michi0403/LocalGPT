# LocalGPT 5.2.8 — zero-configuration build-storage preflight repair

## Fixed

- Fixed the copied `Assert-PowerShellCompatibility.ps1` StrictMode defect that could attempt to resolve `$documentationToolCacheRoot` while merely searching `Build-Documentation.ps1` source text.
- A normal clone has an explicitly guarded zero-configuration storage contract: absent an override, heavy build state defaults to `<repository>/artifacts/.build-storage`. `FUTURE2_BUILD_STORAGE_ROOT` and `-DocumentationCacheRoot` remain optional overrides only.
- `.gitignore` explicitly documents `artifacts/.build-storage/` alongside the existing `artifacts/` rule.

## Maintenance protection

- `Assert-PowerShellCompatibility.ps1` validates the default and Git-ignore contract.
- `build/audit_repository_build_storage.py` rejects the exact StrictMode source-search regression and loss of the zero-configuration default/ignore rule.

## Runtime scope

No LocalGPT application runtime behavior changed.
