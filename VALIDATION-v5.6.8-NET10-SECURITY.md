# LocalGPT 5.6.8 source validation – 2026-10-14

## Performed (PASS)

- Baseline source ZIP CRC compared against extracted original: passed (performed before copy).
- 112 targeted structural and package-version assertions: passed.
- Package `.csproj` XML parse and maintained package pin verification: passed.
- `global.json` and applicable `dotnet-tools.json` JSON parse: passed.
- DevExpress package references identical to 5.6.7 baseline; pinned DevExpressVersion is 25.2.10.
- No original source files removed; exactly 6 original files intentionally modified.
- Full ZIP compression, CRC/testzip and document integrity verification: checked after packaging.

## Not performed

- Dotnet restore/build, Windows/macOS runtime validation and independent transitive NuGet resolution (SDK/DevExpress feeds unavailable in this execution environment).
- Package-vulnerability audit against an actual restored lock graph. Installed SDK must be 10.0.402 or a compatible newer 10.0 feature band; runtime servicing patch must be 10.0.13.

## Expected owner-side validation

1. `dotnet --version` → SDK 10.0.402 (or newer compatible SDK).
2. `dotnet restore` on both solutions, with the licensed DevExpress feed available.
3. `dotnet list package --vulnerable --include-transitive` for the LocalGPT and PublisherStudio affected projects.
4. Rebuild both applications and the Windows WebView wrapper; ensure no NU1903/NU1904 security warnings.
5. Run application tests, launch checks and regular release pipeline.

Note: historical EF migration generator metadata `ProductVersion=10.0.12` describes the original migration and has not been altered. Historical, version-specific audits may naturally assert the original release versions and are not the servicing acceptance tests.
