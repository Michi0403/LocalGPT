# LocalGPT 4.7.8

## Compile repair and console-identity structure

- Repairs the remaining owner-build compiler failure in `FileLoggerSink.Enqueue`: `ObjectDisposedException` derives from `InvalidOperationException`, so the specific disposal catch now precedes the broader invalid-operation catch.
- Replaces the root-level shared `ConsoleProductIdentity` static helper with an immutable shared business object plus stateless extension layers. Startup and installer consumers now materialize identity from their own assembly metadata instead of reading global static identity properties.
- Adds shared string extensions for informational-version build-metadata removal and repository-URL-to-slug conversion, keeping those transformations explicit and reusable rather than buried inside the console helper.
- Keeps console writing as a formatting extension over the resolved identity business object. No mutable global runtime state or invented repository fallback is introduced.
- Links the shared business object/extensions into both LocalGPT and LocalGPTInstallerConsole so the two executables continue to use one identity contract without coupling the installer to the LocalGPT application assembly.

## Preservation

- Local AI runtime, Python.NET serialized execution, Hugging Face model management, image/video/speech DXFunctions, Council integration, benchmark/parser repairs, durable logging, ASCII/Pixel and tournament behavior remain intact.
- Existing `@rendermode InteractiveServer` coverage is preserved.
- Version advances from 4.7.7 to 4.7.8; all version slots remain single-digit.
- PublisherStudio is unchanged.
- No GitHub access, `dotnet`, MSBuild, NuGet restore, build, or publish is used for this source repair. The owner-side Visual Studio build remains the authoritative compiler/runtime check.
