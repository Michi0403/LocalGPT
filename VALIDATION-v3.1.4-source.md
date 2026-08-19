# LocalGPT 3.1.4 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, EF migration command, or DevExpress compilation was executed while preparing this archive.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.4;
- 1-Wire protocol remains 2.1.1;
- the 3.1.3 Council member/provider recovery, expected-cancellation handling and live-user-row stability changes remain present;
- 3.1.1 durable benchmark evidence and 3.1.2 machine-derived benchmark coverage truth remain present;
- no EF migration source or database migration compatibility source changed;
- BenchmarkEvidence JSON schema remains version 1;
- the XML documentation audit now includes `.razor.cs` source instead of excluding it;
- every maintained Razor component has an XML-documented partial class declaration;
- direct Razor `@code` declarations are enriched and validated for contextual `<summary>` text and applicable parameter/return/value/type-parameter explanations;
- enum values are included in the C# documentation audit;
- empty required XML contract tags fail validation;
- 9,865 maintained direct C# declarations across 631 files pass XML documentation coverage/quality validation;
- 45 Razor component types and 752 direct Razor `@code` declarations pass Razor XML documentation coverage/quality validation;
- common 3.1.3 source files showed no non-documentation content differences before the deliberate 3.1.4 version bump; 40 new Razor code-behind companions are documentation-only empty partial class declarations;
- async continuation, service resilience and application architecture static audits pass in this source-only environment.

## Known baseline validation issue

`build/audit_documentation_onewire_contracts.py` also reports a version mismatch in the untouched 3.1.3 source baseline: the checked-in Pages archive identifies as documentation version 3.0.9 while that legacy audit still requires 2.3.7. 3.1.4 does not regenerate or relabel that generated documentation artifact without the .NET/DocFX toolchain, and this pre-existing mismatch is therefore recorded rather than hidden as a successful check.
