# LocalGPT 3.1.4 — XML documentation completeness

LocalGPT 3.1.4 is a forward-only documentation-quality release built on 3.1.3. It does not remove or redesign Council recovery, benchmark evidence, coverage truth, cancellation handling, UI stability, persistence, provider behavior, or the 1-Wire protocol. The purpose of this release is to make maintained C# and Razor source documentation complete enough to be mechanically audited instead of relying on public-member compiler warnings alone.

## Repository-wide XML documentation completion

The documentation enrichment/validation pipeline now covers maintained source under `src`, including:

- classes, interfaces, structs, records, enums, enum members and delegates;
- constructors, methods, extension methods, properties, fields and events;
- controllers, services, repositories, stores, registries, providers and other application-layer types through the same declaration rules;
- `.razor.cs` partial classes and their private/protected/internal/public members;
- Razor component types themselves;
- direct declarations inside Razor `@code` blocks, including component parameters, callbacks, state fields, lifecycle methods and helper methods;
- required XML contract tags such as `<typeparam>`, `<param>`, `<returns>` and `<value>` when applicable.

The pass added missing contextual summaries and contract explanations rather than only inserting empty `<summary>` shells. Existing meaningful documentation is retained; empty or mechanically generic documentation is enriched when the audit identifies it.

## Razor component documentation

Razor source was the largest uncovered area in 3.1.3. The previous C# XML scanner explicitly skipped `.razor.cs`, while inline `@code` declarations were outside the C# file scanner entirely.

3.1.4 removes that gap:

- `.razor.cs` files participate in the normal XML documentation audit;
- all 45 maintained Razor component types now have a documented partial class declaration;
- 40 lightweight `.razor.cs` documentation companions were added for components that previously existed only as `.razor` files;
- 752 direct Razor `@code` declarations are now audited and documented;
- 747 previously undocumented Razor `@code` declarations received XML documentation in this release.

The documentation-only companion partial classes contain no runtime state or behavior. Component logic remains in its existing Razor file unless it already had code-behind.

## C# documentation completion

The expanded C# pass validates 9,865 direct maintained declarations across 631 maintained C# files. This includes 385 enum members, which are now covered explicitly instead of documenting only the containing enum type.

Compared with 3.1.3 source, the release contains 936 additional C# `<summary>` blocks and 747 additional Razor `<summary>` blocks. The audit also checks that required parameter/return/value/type-parameter tags contain explanatory text rather than merely existing as empty XML elements.

## Future regression prevention

`build/Assert-XmlDocumentationCoverage.py` now validates both ordinary C# and Razor source. `build/Add-XmlDocumentation.py` enriches both source kinds through the same entry point. The existing PowerShell repository validation continues to call `Assert-XmlDocumentationCoverage.ps1`, so the broader coverage becomes part of the established repository validation path rather than a one-time cleanup.

New helper `build/razor_xml_documentation.py` performs line-safe Razor `@code` analysis and verifies that every maintained Razor component has an XML-documented partial component declaration.

## Behavior and compatibility

This is intentionally a documentation-focused release:

- no existing 3.1.3 Council recovery behavior was removed;
- no benchmark evidence or coverage-truth behavior was removed;
- no EF Core migration or SQLite compatibility source was changed;
- BenchmarkEvidence schema remains version 1;
- no provider/model runtime policy was changed by the documentation pass;
- no Razor component logic was moved or rewritten;
- no 1-Wire protocol change is introduced; protocol version remains 2.1.1;
- existing source files differ from 3.1.3 only by XML documentation except for the explicit 3.1.4 version identifiers and documentation-audit tooling; the new Razor companion files are empty partial declarations carrying component documentation only.

## Build status

This archive is source-only. The preparation environment does not contain the .NET/DevExpress build toolchain, so no claim of successful compilation or runtime execution is made. Repository static audits are recorded in `VALIDATION-v3.1.4-source.md` and the packaged validation log.
