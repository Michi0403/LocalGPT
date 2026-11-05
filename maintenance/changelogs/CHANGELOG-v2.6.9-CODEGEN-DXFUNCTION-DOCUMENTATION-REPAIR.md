# LocalGPT 2.6.9 — Code generation, DXFunction and documentation repair

## Ollama build repair

- Repaired the `OllamaThinkingChatClient` textual DXFunction fallback raw interpolated string that caused CS9006/CS1733 around the literal JSON example.
- Uses the reviewed three-dollar raw interpolated string form so `marker` and `functionDirectory` are interpolated with triple braces while the one-brace JSON object remains literal content.
- The fallback still exposes only exact request-specific registry names and retains normal LocalGPT DXFunction validation/approval behavior.

## Code-generation scale limits removed

- Removed the `MaxPayloadCharacters`, `MaxFileCount`, and `MaxReviewTake` constants from `CodeGenerationWorkflowService`.
- Removed the fixed four-million-character review payload rejection.
- Removed the fixed 512 explicit-file and 512 CodeDOM-type ceilings.
- Removed workflow truncation of title/goal/project-state/council/change/safety summaries, decision notes, file purposes, CodeDOM result/summary text, and output descriptions. The workflow now preserves supplied text and only substitutes a fallback when a value is blank.
- Removed the 5,000-path response-reporting cut-off after cloning an approved tracked project revision. All approved tracked files are still hash-verified/copied, and all written paths are now retained in the execution result.
- `codegen.review.list` now honors any positive caller-provided `take` value rather than enforcing a fixed 100-review maximum.
- Removed the hidden 100,000-file post-generation project-rescan ceiling and the hard-coded generated-workspace file-size override. Project scans now use the database-backed `MaxFiles` and `MaxSingleFileBytes` runtime policies by default, while explicit positive caller values can intentionally request smaller bounded scans.
- Removed duplicate `ProjectMaintenanceService` source constants for compiler-candidate count and captured process-output length; those values now resolve through the existing `ProjectMaintenanceMaximumCompilerCandidates` and `ProjectMaintenanceMaximumCapturedCharacters` runtime-policy keys.
- Raised the retained compatibility seed values for `CodeGenerationMaximumPayloadCharacters`, `CodeGenerationMaximumFileCount`, and `CodeGenerationMaximumReviewTake` to the Int32 range so fresh policy stores do not reintroduce the former small limits if those keys are consumed by extensions.
- Path normalization, isolated-workspace containment, review hashing, one-use approval, tracked-file content hashes, and separate build confirmation remain intact. These are safety/integrity boundaries rather than arbitrary repository-size limits.

## DXFunction code-generation wiring

- Audited the DI path from `IDxAiFunctionHandler` assembly discovery through `DxAiFunctionRegistry` to `ICodeGenerationWorkflowService`.
- Audited all five code-generation registry functions: `codegen.review.list`, `.get`, `.create`, `.execute`, and `.reject`.
- Fixed the `codegen.review.create` JSON schema to expose `projectRevisionId`, which already exists in `CreateCodeGenerationReviewRequest` and is required for exact approved-revision project maintenance.
- Audited all seven output kinds through the workflow: `SourceFiles`, `ClassLibrary`, `ConsoleApplication`, `Solution`, `LocalGptAddon`, `CSharpScript`, and `JavaScriptModule`.
- Added a source-only `build/audit_codegen_dxfunction_wiring.py` contract check plus PowerShell wrapper and wired it into repository validation before compilation.

## Code-generation documentation

- Added `docs/guide/code-generation-and-dxfunctions.md` and linked it from the user-guide TOC.
- Documented the exact user/AI data needed for exact source trees, C# libraries/apps/scripts, JavaScript modules, LocalGPT add-ons, scaffolded solutions, exact multi-project solutions, and tracked-project maintenance.
- Documented the review -> inspect hash -> explicit execute approval -> optional separately confirmed build flow.
- Documented the Ollama textual DXFunction fallback and its exact JSON call shape.
- Clarified that GitHub/network/compiler access is not required to create or review code; a toolchain is needed only for an explicitly requested build.

## XML documentation

- Added/extended deterministic XML documentation tooling so maintained C# types and methods are covered beyond the previous public-only surface; public/protected API members are included as well.
- Added missing XML summaries throughout LocalGPT, installer, WebView wrapper, and RID-neutral wire-protocol source while leaving generated Designer/g.cs sources untouched.
- Source audit now passes for **7,073 maintained C# type/method and public API declarations**.

## InteractiveServer render boundaries

- Verified every routed LocalGPT page except the intentionally static Error page already carries an explicit InteractiveServer render mode.
- Strengthened `Assert-InteractiveServerRenderModes.ps1` to cover the previously unaudited `Help.razor` and `Index.razor` directives.
- Preserved the reviewed architecture where child workbench/theme components inherit their parent InteractiveServer circuit instead of creating unsafe nested render boundaries around callback/fragment content.

## Version

- LocalGPT application: **2.6.9**
- Installer console: **2.6.9**
- WebView wrapper: **2.6.9**
- 1-Wire protocol package version remains **2.1.1** because this release does not change the wire contract.
