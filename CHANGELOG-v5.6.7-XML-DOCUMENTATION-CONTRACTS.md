# LocalGPT 5.6.7 — XML documentation coverage and build enforcement

## Why this release exists

The Windows Visual Studio build of LocalGPT 5.6.6 succeeded and generated the LocalGPT assembly, but emitted XML documentation warnings: CS1572 (stale parameter tags) and CS1573 (missing parameters). The existing XML-documentation scanner was available as a standalone repository validation but was **not wired into the normal LocalGPT build**, so missing documentation in recently added services and Razor components remained undetected.

## Changes

1. Ran the existing contextual XML documentation enrichment against the **entire** maintained `src` tree, not only the latest services. The initial validator reported 3,796 findings. The enrichment added **1,744** missing C# documentation blocks and improved **1,050** existing blocks; it also updated **23** Razor `@code` documentation blocks and created the missing documented `LanguageSwitcher.razor.cs` partial component declaration.
2. Reviewed and rewrote the documentation contracts of the Python task coordinator to describe Python.NET GIL serialization, isolated interpreter processes, device-lane locks, bounded process admission and output, cancellation, status and state snapshots. Its executable implementation is unchanged.
3. Fixed `DxAiFunctionRegistry.BuildValidationIssue` documentation, removing obsolete `descriptor` and `parameters` tags and documenting the current `message` signature. Added the missing constructor and request parameter documentation reported by the compiler in the other affected controller/service classes.
4. Extended `build/xml_documentation.py` and `build/razor_xml_documentation.py` to reject **unknown and duplicate** `<param>` and `<typeparam>` tags, in addition to their existing missing, empty, generic, malformed, `<returns>` and `<value>` validations. XML diagnostics now use the Visual Studio/MSBuild `file(line,column): error LGXML001:` form.
5. Added unconditional normal-build `AssertLocalGptXmlDocumentationCoverage` to `Directory.Build.targets`, invoking the maintained PowerShell/Python validation wrapper before compilation and long DocFX/PDF production. In the LocalGPT project, CS1572, CS1573 and CS1591 are now errors rather than warnings. There is no baseline allow-list, skip switch, or build-time source rewrite.
6. Added `build/audit_xml_documentation_contract.py` with isolated passing/failing C# and Razor samples, signature drift, duplicates, property contracts, missing component partials and read-only validation tests.
7. Advanced application version and UI cache-busting asset versions to **5.6.7**; preserved all existing runtime, storage, DI, controller, renderer, DevExpress and Python behavior.

## Audit scope

The complete source-only XML validator now passes on **14,504 maintained C# declarations in 857 files** and **1,244 Razor `@code` declarations in 58 components** (component type partials also verified). Previous Windows C# XML warnings have been addressed at their declarations.

The broad annotation pass was necessary because the existing audit had uncovered older documentation debt, not only the newest Python service. Some legacy documentation remains mechanically descriptive; the gate prevents missing, structurally generic, stale, duplicate and malformed tags but it does **not** prove the scientific quality of every prose sentence. The actively reviewed Python coordinator has task-specific documentation.

## Verification boundary

All documented changes were checked with source-only audits, negative regression samples, XML parse checks, and a line-level executable-source comparison against 5.6.6. **No .NET compilation or DocFX/PDF generation was executed here.** A fresh Visual Studio build and a normal documentation run are still the acceptance tests on the user machine. PublisherStudio is unchanged.
