# LocalGPT 4.5.8 source validation

## Scope

This release is prepared without GitHub/online repository access and without invoking `dotnet`, NuGet restore, MSBuild, compilation or publishing. Validation is source/static only. The user's Windows environment remains the authority for the final .NET/Razor compile and runtime behavior.

## Windows build findings repaired

- The maintained text-service ownership rule remains enabled and unchanged. `ChatGameConsole.razor` no longer composes the new browser-layout signature with direct `string.Join`; `AsciiChatTextService.BuildConsoleLayoutSignature` owns that projection instead.
- The Chat provider selector now supplies an explicit typed `string` lambda to DevExpress `ValueChanged`, matching the typed-value pattern already used by maintained selectors and avoiding the reported untyped `EventCallback` Razor inference.
- No text-service baseline entry, DevExpress exception, architecture exception, or other guard weakening was added for either repair.

## Preserved 4.5.7 behavior

- Inline Chat/ASCII review and function-request controls remain intact.
- Typed provider/list values and adaptive dropdown portal repair remain intact.
- Compact numeric editors continue to use `DxSpinEdit`; `DxRangeSelector` is not reintroduced.
- InteractiveServer cancellation/reconnect handling and reduced browser/render churn remain intact.
- Tournament ASCII actors, multi-frame combat presentation, duplicate-name handling and eligible-role-pool failover remain intact.
- Program Compiler/Repository Curator and Project Maintenance/Cross-platform Publisher seed teams remain intact.
- Council seed remains 35; runtime-class seed remains 7.
- No EF migration and no wire-protocol change.
- InteractiveServer topology remains 15 direct declarations plus 5 explicit non-prerender declarations.

## Automated source checks

The final source tree passed the maintained 4.5.8 release audit, DevExpress control audit, Kernel Creature Tournament audit (49 checks), provider-stream repetition audit, Chat ASCII audit (24 checks), ASCII color/game-authoring audit (58 checks), ASCII DOOM audit (32 checks), provider-qualified Council audit (282 checks), Council role-context isolation, code-generation/DXAIFunction wiring, application architecture, configurable Council behavior policy, X-Round/heartbeat wiring, Council SQL seed, cross-platform boundaries, PowerShell variable interpolation, configuration-root qualification, async-continuation and service-resilience audits.

The async-continuation audit covered 272 source files with 3,368 await tokens, 2,943 `ConfigureAwait(false)` continuations, 202 renderer-affine `ConfigureAwait(true)` continuations, 218 explicitly configured async disposals and 5 configured async streams. Service-resilience auditing covered 2,516 service methods.

The Windows `Assert-TextServiceOwnership.ps1` matching algorithm was reproduced against the unchanged maintained `text-service-ownership-baseline.json`; it reports no new component/controller string or regex ownership violation. The exact reported `var layoutSignature = string.Join(` pattern is absent from `ChatGameConsole.razor`, and the exact reported provider method-group callback is replaced by the explicitly typed lambda.

All 25 browser JavaScript source files under `wwwroot/js` passed `node --check`; the release audit separately verifies the 24 files tracked by the JavaScript diagnostics hash manifest. JSON parsing passed for 38 maintained JSON files and project/MSBuild XML parsing passed for 8 maintained project/property/targets files in this source tree.

The repository-wide XML documentation checker still reports the same 559 normalized historical findings already present in the 4.5.7 package. The new `BuildConsoleLayoutSignature` service method is fully documented and introduces no new finding. These checks do not substitute for a .NET compile.

## Packaging

The source ZIP is CRC-tested, checked for absolute/traversal paths, cleanly extracted, and compared byte-for-byte against the packaged source tree after excluding transient interpreter output. This validates packaging integrity, not .NET compilation or runtime behavior.
