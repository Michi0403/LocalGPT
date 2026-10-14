# LocalGPT 2.9.1 source validation

Source-only validation is performed without `dotnet`, MSBuild, Visual Studio or GitHub access.

Checks include the strict async-continuation policy, existing Council role/rejoin/trace regressions, application architecture/service resilience including Razor static-declaration enforcement, the 2.9.1 live-transcript-status regression, XML project parsing, render-mode comparison and archive integrity.

The Windows .NET build remains authoritative for compilation and runtime behavior.
