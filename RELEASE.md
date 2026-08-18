# LocalGPT 3.1.0

LocalGPT 3.1.0 is the source-only toolchain-integration successor to 3.0.9. It starts from the user's upgraded source tree and records the .NET 10 / DevExpress 25.2-line upgrade without changing the benchmark, Council, database schema, or 1-Wire behavior.

## Toolchain state

- .NET SDK: `10.0.400`
- Target framework: `net10.0`
- Microsoft ASP.NET Core / EF Core patch dependencies: `10.0.11` where present in the supplied source
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Database boundary

No migration or schema change is introduced by this release. The supplied migration sources and migration compatibility service are preserved byte-for-byte.

See `CHANGELOG-v3.1.0-DOTNET-DEVEXPRESS-UPGRADE.md` and `VALIDATION-v3.1.0-source.md`.
