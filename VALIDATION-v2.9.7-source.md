# LocalGPT 2.9.7 source validation

This release was **not compiled** in the release-preparation environment. The user's Windows `Build-LocalDevelopment.ps1` build remains authoritative.

Source-only validation performed:

- 2.9.7 compile-contract regression audit for the four reported C# errors.
- Existing LocalGPT release/regression audits, including benchmark, role authority, provider-qualified Council, X-Round/heartbeat, async, architecture and service-resilience checks.
- XML parsing for maintained project/property files.
- JavaScript syntax checks where Node-compatible maintained JavaScript files are present.
- Exact `@rendermode` comparison against 2.9.6 source.
- Wire Protocol and `Directory.Build.props` byte comparison against 2.9.6 source.
- Source-only ZIP integrity and forbidden-artifact scan.

No `dotnet`, MSBuild, Visual Studio build, GitHub, or web repository source was used for release validation.
