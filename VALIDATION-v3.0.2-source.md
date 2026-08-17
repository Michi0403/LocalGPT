# LocalGPT 3.0.2 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. The user's Windows build remains authoritative.

## User-reported Windows build failures addressed

1. Operational diagnostics guard: now scans logical Chat and Program partial sets rather than only `Chat.razor` / `Program.cs`.
2. InteractiveServer guard: now scans all `Program*.cs` for `AddInteractiveServerComponents`, `AddInteractiveServerRenderMode`, and circuit diagnostics registration.
3. Iterator exception guard: responsibility-named partial files map to the original maintained baseline owner; the six reported historical iterator findings remain baseline-controlled without weakening new-violation detection.
4. System-variable initialization guard: the five reported historical literal findings map to the original maintained baseline owner; new literals remain blocked.
5. `StructuredTextController`: missing `LocalGPT.Services` import repaired, resolving the reported `CouncilTextService` `CS0246` source error.
6. WebView wrapper `WMC1006/CS0006`: treated as downstream of the failed LocalGPT build because `LocalGPT.dll` was not produced; no speculative wrapper reference change was made.

The Visual Studio message stating that NuGet project details for `LocalGPTWebviewWrapper` could not be loaded did not identify a package or restore conflict. No package-reference change was made without evidence.

## Service/controller/DXFunction wiring audit

- `MinecraftProjectService`: DI-registered; used by `MinecraftDiagnosticController`; AI-facing read-only resolver exposed as `minecraft.dependency.version.resolve`.
- `MinecraftDatapackService`: DI-registered; used by `MinecraftDiagnosticController`; AI-facing read-only resolver exposed as `minecraft.datapack.version.resolve`.
- Both Minecraft DX handlers implement `IDxAiFunctionHandler`; Program assembly discovery registers every concrete handler and `DxAiFunctionCatalogService` discovers registry descriptors and persists them with `IsSystemSeed = true`.
- `CouncilKnowledgeContentService`: subordinate to `ICouncilKnowledgeService`; knowledge controller/diagnostic and existing knowledge DXFunction surfaces remain the external boundary.
- `RegexCompilationService`: subordinate compiler used by Regex persistence/runtime policy services; regex controller and seeded regex DXFunctions remain the external boundary.
- `ProviderModelReviewerPolicyService`: shared benchmark policy used by benchmark service/UI; provider benchmark controller/diagnostic and DXFunction remain the external boundary.
- `JsonTextService`: infrastructure service used behind configuration/security/Minecraft services; no direct controller or AI tool is intentionally created because doing so would bypass owning services.

## Passed source validation

- `build/audit_release_3_0_2.py`: 59 Windows-build/wiring checks.
- All historical `build/audit_release_2_8_5.py` through `build/audit_release_3_0_1.py` pass.
- Application architecture audit passes.
- Service resilience audit: 1,909 methods own logged try/catch boundaries; 30 iterator methods and 3 Program/Startup methods remain governed by dedicated policies.
- Async continuation audit: 220 source files, 2,437 await tokens, 2,222 `ConfigureAwait(false)`, exactly 29 renderer-affine `ConfigureAwait(true)`, 182 configured async disposals, 4 configured async streams.
- Provider-qualified Council audit: 282 checks.
- X-Round/heartbeat audit passes.
- Code-generation/DXFunction wiring audit passes.
- Configurable Council behavior-policy audit passes.
- Benchmark/rejoin regression audit passes.
- Chat ASCII console audit: 17 checks.
- Human-visible entity formatting audit passes.
- Documentation/1-Wire contract audit passes.
- XML documentation coverage and quality: 7,572 declarations across 530 maintained source files.
- Python-equivalent execution of the system-variable initialization matching logic reports zero new violations after partial-owner normalization.

PowerShell is not installed in this inspection environment. The PowerShell guards were therefore source-reviewed and their matching/baseline behavior was reproduced with Python where applicable; the Windows build is the authoritative execution of those `.ps1` files.
