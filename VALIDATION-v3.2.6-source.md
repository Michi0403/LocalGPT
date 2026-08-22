# LocalGPT 3.2.6 source validation

Validation was intentionally source-only. No `dotnet` build, publish, test or runtime launch was performed.

## Passed repository checks

- `audit_application_architecture.py --mode all`
- `audit_async_continuations.py`
- `audit_service_resilience.py`
- `audit_codegen_dxfunction_wiring.py`
- `audit_provider_qualified_council.py --root .` — 282 checks
- `Assert-XmlDocumentationCoverage.py`
- `audit_release_3_2_6.py` — 33 release-contract checks

## Release-contract evidence

- LocalGPT, InstallerConsole and WebView wrapper resolve to 3.2.6; minor/patch slots remain single digit.
- DevExpress stays at 25.2.9.
- `/remote-control` retains `@rendermode InteractiveServer`.
- The page is full-width, uses a 14–18rem desktop workbench rail, collapses responsively and has no old 1600px page ceiling.
- Allowed hosts and HTTP headers are maintained through guided row editors and presets.
- The guided editor continues to use existing JSON-text serialization and the existing stored connector contract.
- Six localization catalogs are in exact 2,012-key parity.

## Provided-archive limitation

The optional historical configurable-behavior and X-round audit scripts reference documentation files absent from the supplied repository ZIP. Their missing-document failures are baseline archive-content limitations, not failures in the 3.2.6 Remote Control changes.

## Toolchain limitation

Compilation status is deliberately not claimed because `dotnet` is unavailable in this execution environment.
