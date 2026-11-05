# LocalGPT 5.3.8 — compiler warning cleanup

LocalGPT 5.3.8 follows the successful 5.3.7 build and removes the compiler warnings reported from that build without suppressing diagnostics or changing runtime behavior.

## Fixed

- Removed the unread `IWebHostEnvironment environment` constructor dependency from `InitialDataCatalog`; the DataCatalog/DataFeed ownership introduced in 5.3.6 no longer requires that collaborator.
- Added the missing XML `<param name="runtimePolicy">` documentation to all 29 constructor/type declarations reported by the compiler after the database-backed runtime-policy expansion.
- No `NoWarn`, warning suppression, architecture exemption, or runtime-policy rollback was introduced.
- The DataCatalog/DataFeed unification, database-backed service parameters, prompt/regex ownership and ZIP/workspace learning fixes remain unchanged.

## Validation boundary

Source-only validation was performed. `dotnet`, MSBuild, NuGet restore, publish and GitHub were not invoked. The user-provided 5.3.7 build already reached `LocalGPT.dll` and completed DocFX with zero warnings/errors; this release addresses the C# warning set reported immediately before that successful output.
