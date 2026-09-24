# LocalGPT 4.7.9 source validation

LocalGPT 4.7.9 is a focused source cleanup following the owner-side 4.7.8 compiler pass. It changes the name and audit ownership of the C# project-wide import file without broadening the set of global namespaces.

## Global import structure

- `src/LocalGPT/GlobalImports.cs` exists and contains the maintained `global using LocalGPT.WireProtocol;` import.
- Legacy `src/LocalGPT/GlobalUsings.OneWire.cs` is removed.
- `build/Assert-OneWireArchitecture.ps1` now reads `src/LocalGPT/GlobalImports.cs` and still checks that the 1-Wire protocol namespace is imported application-wide.
- `src/LocalGPT/Components/_Imports.razor` remains separate and unchanged because Razor import inheritance and C# `global using` compilation are different surfaces.

## Static checks

- XML parsing for all versioned project files.
- JSON parsing for maintained application/documentation JSON.
- Python async-continuation audit.
- Python service-resilience audit.
- Python application-architecture audit.
- LocalGPT 4.7.9 release audit, including the new GlobalImports filename contract and absence of the legacy filename.
- JavaScript syntax checks for maintained LocalGPT browser scripts.
- Source-package CRC, path-traversal, Python-cache exclusion, and clean-extraction byte comparison.

## Runtime boundary

No `dotnet`, MSBuild, NuGet restore, build, publish, installer execution, or GitHub access is used here. The first actual .NET compiler pass for 4.7.9 remains the owner's Visual Studio build.
