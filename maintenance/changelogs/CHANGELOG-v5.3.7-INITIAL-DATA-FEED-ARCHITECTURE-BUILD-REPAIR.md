# LocalGPT 5.3.7 — initial data feed architecture/build repair

LocalGPT 5.3.7 follows 5.3.6 and addresses the reported `ARCH0001` build failure without weakening the application-static guard.

## Fixed

- `InitialDataFeedService.IsSupportedFeedPath(...)` is now an instance-owned helper instead of an application `static` member.
- `InitialDataFeedService.ResolveRepositoryRoot(...)` is now an instance-owned helper instead of an application `static` member.
- The service continues to own repository/embedded initial-feed reading behind `IInitialDataFeedService`; `InitialDataCatalog` remains the catalog/projection boundary.
- Markdown, SQL and text feed support introduced in 5.3.6 remains unchanged.
- Runtime-policy/database ownership from 5.3.5 remains unchanged.

## WebView wrapper restore message

The reported Visual Studio restore message for `LocalGPTWebviewWrapper` was inspected separately. Its project file is well-formed XML and is byte-identical to the supplied 5.3.5 wrapper project, so this corrective release does not guess at a package/project change without a package-specific restore error. The concrete source-owned build failure in this release was the application-static policy violation above.

## Validation boundary

Source-only validation was performed. `dotnet`, MSBuild, NuGet restore, publish and GitHub were not invoked.
