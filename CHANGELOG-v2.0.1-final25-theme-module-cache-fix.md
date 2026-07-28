# LocalGPT 2.0.1 final25

## Fixed

- The theme dispatcher now resolves `theme-controller.js` through ASP.NET Core's `IFileVersionProvider` before dynamically importing it.
- The application shell uses the same fingerprinted static-asset URL for its module script.
- A browser can no longer retain the obsolete module export shape that caused `applyThemeState is not a function` after upgrading the source package.

## Safeguards

- The JavaScript diagnostics architecture check now requires the fingerprinted dynamic import and fingerprinted application-shell module URL.
- Existing direct-module export, JavaScript error reporting, security, 1-Wire, runtime-value ownership, and service-boundary rules remain unchanged.
