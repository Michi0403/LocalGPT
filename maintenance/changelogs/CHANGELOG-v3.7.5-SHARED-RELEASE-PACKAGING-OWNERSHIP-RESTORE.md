# LocalGPT 3.7.5 — shared release-packaging ownership restore

## Fixed

- Reaffirmed `src/LocalGPT.ReleasePackaging` as LocalGPT-owned source only. PublisherStudio consumes its NuGet/.NET-tool package instead of carrying a second source project.
- Fixed a latent `Complete-ReleaseBundle` parameter mismatch: the call already supplied `-ReleasePackagingPackagePath`, but the function did not declare the parameter. The authoritative `LocalGPT.ReleasePackaging.1.0.2.nupkg` is now explicitly included in the upload-ready LocalGPT release bundle.
- Existing release-bundle resume validation now requires both authoritative LocalGPT packages plus the current documentation PDF before a version directory can be considered complete.
- Added the missing XML `<param>`/`<returns>` documentation to the new adaptive PDF helper methods without changing their behavior.

## Preserved

- `LocalGPT.ReleasePackaging` 1.0.2 behavior: PDFsharp merge, optional qpdf/Ghostscript optimization, native package helpers, and no commercial PDF dependency.
- Adaptive browser PDF chunks, compressed PDF embedding in `wwwroot/help-docs`, Full/self-contained packaging, notarization resume/reuse, Homebrew RPM support, and all application architecture boundaries.

## Validation

Static release, architecture, cross-platform, async-continuation, service-resilience, XML-documentation, and 15 reviewed `InteractiveServer` boundary checks are retained. The release audit now guards the authoritative package ownership/bundle contract.
