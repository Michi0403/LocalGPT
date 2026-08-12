# LocalGPT 2.7.5 source validation

This package was repaired directly from the supplied/generated LocalGPT 2.7.4 source ZIP. No GitHub/network repository access was used. No `dotnet`, MSBuild, restore, build, test, publish or DocFX command was invoked; the owner's Windows/.NET build remains authoritative.

## User-reported build blockers addressed

- `CodeGenerationController.cs` now imports `LocalGPT.Services`, so the `LocalGptCatalogService catalog` constructor dependency resolves from its actual namespace.
- The exact source rule enforced by `Assert-TextServiceOwnership.ps1` was reproduced against `build/text-service-ownership-baseline.json`; it reports **0 new direct component/controller string/regex operations** after the repair.
- The offending live attachment presentation is now delegated from `Chat.razor` to `CouncilTextService.BuildAttachmentPresentation(...)`.
- The architecture guard and its MSBuild target remain enabled; no baseline exception was added.

## Completed source/static validation

- Provider-qualified Council audit: **210 checks** pass.
- Architecture policy audit passes.
- Async continuation audit passes for **154 source files**, **2,267 await tokens**, **2,061 ConfigureAwait(false)**, **30 reviewed ConfigureAwait(true)**, **2 preconfigured awaitables**, **171 reviewed await-using disposals**, and **3 configured async streams**.
- Service resilience audit passes for **1,809 service methods**; 30 iterator/yield methods and 3 direct Program/Startup methods are intentionally excluded.
- Code-generation/DXFunction audit passes, including the new `CodeGenerationController` service-namespace/catalog-dependency regression checks.
- Council X-Round/heartbeat/live-result audit passes.
- Documentation/1-Wire contract audit passes.
- Kawaii documentation-layout audit passes.
- Chat ASCII-console audit passes **17 checks**.
- XML documentation coverage passes for **7,232 maintained C# type/method/public API declarations**.
- LocalGPT `en-US` and `de-DE` localization catalogs contain **1,856 keys each** with exact key-set equality.
- All **4** LocalGPT project files parse as XML.
- Application, wrapper and installer projects are versioned **2.7.5**; wire protocol remains **2.1.1**.

## Compiler boundary

The reported owner build already demonstrated that disabling the text-service guard and adding the missing namespace import allowed compilation to continue. This source package fixes both issues without disabling the guard. Because this packaging environment intentionally did not invoke the .NET compiler, the next Windows build is still the final compile/runtime authority.
