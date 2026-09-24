# LocalGPT 4.7.9

## Global import naming cleanup

- Renames the LocalGPT-wide C# namespace import file from `GlobalUsings.OneWire.cs` to `GlobalImports.cs` so the project terminology is easier to understand beside Blazor's `Components/_Imports.razor`.
- Keeps the actual C# language construct unchanged: `global using LocalGPT.WireProtocol;` remains the single project-wide import currently owned by this file.
- Updates the maintained 1-Wire architecture check to read `GlobalImports.cs`, so the protocol namespace requirement continues to be audited after the rename.
- Intentionally does not globalize feature-specific namespaces. The rename improves discoverability without hiding additional service/domain dependencies or increasing namespace-collision risk.

## Preservation

- Local AI runtime, Python.NET serialized execution, Hugging Face model management, image/video/speech DXFunctions, Council integration, console identity restructuring, benchmark/parser repairs, durable logging, ASCII/Pixel and tournament behavior remain intact.
- Existing Razor `Components/_Imports.razor` remains unchanged and continues to own Razor-specific imports/injections.
- Version advances from 4.7.8 to 4.7.9; all version slots remain single-digit.
- PublisherStudio is unchanged.
- No GitHub access, `dotnet`, MSBuild, NuGet restore, build, or publish is used for this source change. The owner-side Visual Studio build remains the authoritative compiler/runtime check.
