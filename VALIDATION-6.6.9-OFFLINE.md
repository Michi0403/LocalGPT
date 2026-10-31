# LocalGPT 6.6.9 source validation

- Compared every original path in the 6.6.8 archive against the replacement source: none deleted.
- Updated both DevExpress selector callbacks to explicitly typed `async`/`await` expressions.
- Kept the service-owned saving path and original user confirmation.
- Extended the same build-wired PowerShell contract; no version-specific test scripts.
- XML parsing of `LocalGPT.csproj` and `Directory.Build.targets` passes.
- JavaScript SHA-256 manifest and RID counts are checked during ZIP assembly.
- **Not executed:** .NET compilation, PowerShell scripts, Razor generator, DevExpress runtime tests, migration or guest VM execution.
