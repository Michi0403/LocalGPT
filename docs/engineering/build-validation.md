# Build and validation

## Build truth

A successful LocalGPT build is the primary compilation truth. Delimiter counts, regex scans, JSON/XML parsing, IDE inspection, or screenshots cannot replace it.

The repository pins its SDK through `global.json` and provides validation scripts for architecture and policy. When .NET is unavailable, those scripts can still provide partial evidence, but the result must be labeled uncompiled.

## Validation sequence

A normal maintenance pass should run, as applicable:

1. source formatting and encoding checks;
2. JSON/YAML/XML parsing;
3. C# syntax parsing through the pinned SDK/Roslyn route;
4. architecture assertions;
5. dependency and publish-configuration checks;
6. localization and JavaScript diagnostics checks;
7. database migration/snapshot checks;
8. application build/tests;
9. documentation build and artifact verification;
10. focused browser smoke tests.

## Generated C#

Generated source must preserve namespaces, nullable context, project references, service lifetimes, and existing framework patterns. It must not report compilation success until the target project has actually compiled.

Warnings are triaged by category. Suppression is not the default response to a warning introduced by generated code.

## Static and runtime ownership

Application-owned mutable catalogs, regex lists, runtime values, and policy state are not static. Framework bootstrap and pure immutable helpers remain valid static boundaries.

Runtime-value services own mutable definitions and current values. Persistence services own durable policy. Components consume services rather than constructing global state.

## Logging integrity

Logs contain technical context needed for diagnosis but avoid prompts, full model responses, uploads, generated file contents, credentials, secrets, and unbounded exception data.

The database logger exposes readiness so startup failures do not recurse. User notifications are sanitized; detailed traces remain in the configured logger.

## JavaScript diagnostics

Maintained JavaScript functions used through interop are registered and validated. Build checks detect missing function names and known unsafe patterns. Browser tests cover theme selection, documentation dropdown behavior, fullscreen/close flows, and critical navigation.

## Frontend checks

Important checks include:

- interactive render-mode ownership;
- no nested incompatible render boundaries;
- responsive dialogs and scroll ownership;
- keyboard focus and visible labels;
- clickable menus above decorative layers;
- reduced-motion behavior;
- light/dark contrast;
- no fake or dead controls.

## Documentation checks

The documentation build verifies conceptual pages, generated API YAML/HTML, versioned PDF, Kawaii CSS/JS markers, icon assets, status JSON, and nonzero source/API counts.

The Pages extractor accepts UTF-8 files with or without BOM, normalizes ZIP separators, rejects unsafe paths, and chooses the strongest complete release candidate.
