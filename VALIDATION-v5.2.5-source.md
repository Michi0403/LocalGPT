# LocalGPT 5.2.5 source validation

## Scope

Source-only audit and repair. No `dotnet`, MSBuild, NuGet restore, build, publish, GitHub access, or online repository access was used.

The audit targeted the InteractiveServer feedback-loop class confirmed in PublisherStudio 4.0.0: browser/vendor-owned transient interaction state (caret, selection, live document buffers, drag handles, scroll/focus state) must not be continuously written into server component state and immediately supplied back through a Blazor rerender.

## Repository-wide finding and repair

The repository scan found one analogous high-risk LocalGPT surface: the runtime-extension editor in `Install.razor` used delayed two-way `DxHtmlEditor @bind-Markup`. Its live document notifications could therefore schedule a render of the same complex editor while DevExpress still owned the active caret/document state.

5.2.5 changes that surface to one-way `Markup` initialization plus an explicit `MarkupChanged` observer. The observer captures the current source markup while suppressing the callback's automatic component render. Intentional source replacement advances an editor-generation key so stale vendor state is discarded only when LocalGPT deliberately supplies a new editor generation.

The broader scan found no `DxRichEdit` transient two-way binding, no server-round-tripped `DxRangeSelector` handle-move state, and no native range `@oninput` in maintained LocalGPT Razor components. Existing scalar form bindings (`Text`, `Value`, `Checked`) remain ordinary form state and are not classified as complex-editor transient state.

## Permanent maintenance rule

The following enforcement was added and wired into the normal build-maintenance chain:

- `build/audit_transient_ui_state_ownership.py`;
- `build/Assert-TransientUiStateOwnership.ps1`;
- `Directory.Build.targets` target `AssertLocalGptTransientUiStateOwnership`;
- retention checks in `build/Assert-DevExpressComponentRetention.ps1`; and
- the architecture/remediation contract in `AGENTS.md`.

The diagnostic itself explains the approved repair: initialize complex editors one-way, let browser/vendor code own live interaction state, observe changes without allowing the event callback to rerender that same live control, reset transient state at intentional editor-generation boundaries, and commit durable model state at Apply/Save/change/pointer-up/handle-release. Continuous preview must stay browser/vendor-owned and use coalesced/committed server updates rather than a feedback loop.

Synthetic negative fixtures confirmed that the new audit rejects delayed/live `DxHtmlEditor @bind-Markup`, `DxRichEdit @bind-Selection`, and `DxRangeSelector` `OnHandleMove`. A synthetic positive fixture using one-way markup plus a non-rendering observer passed.

## Maintained source audits executed

The following source-only checks passed after the repair:

- `build/audit_release_5_2_5.py`;
- `build/audit_transient_ui_state_ownership.py --root . --product localgpt` — 56 Razor components passed;
- `build/audit_application_architecture.py --root . --product localgpt --mode all`;
- `build/audit_async_only_architecture.py --source-root src/LocalGPT --product LocalGPT` — 613 maintained source files passed;
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 333 source files / 4,053 await tokens / 3,573 `ConfigureAwait(false)` / 205 renderer-affine `ConfigureAwait(true)` / 269 configured async disposals / 6 configured async streams;
- `build/audit_razor_maintenance_contract.py --root . --product localgpt` — 55 maintained Razor components passed; and
- `build/audit_service_resilience.py --root . --product localgpt` — 2,854 service methods passed, with 29 yield methods and 3 direct Program/Startup methods skipped by that maintained policy.

Python syntax validation passed for the new generic and release audits. `Directory.Build.targets` and `LocalGPT.csproj` parse as XML.

`build/Assert-XmlDocumentationCoverage.py .` still reports the same pre-existing documentation baseline as 5.2.4: 2,608 direct C# documentation findings and 21 Razor documentation findings. The untouched 5.2.4 ZIP was re-audited with the same script and produced the same counts, so 5.2.5 introduces no documentation-coverage regression. This historical baseline is not changed as part of the transient-state repair.

PowerShell is unavailable in this preparation environment, so the new `.ps1` wrapper could not be executed through `pwsh`; its build wiring and retention tokens were source-checked, and the Python audit it invokes was executed directly.

## Release identity

Active LocalGPT identity/cache references are aligned at **5.2.5**, satisfying the repository's single-digit minor/patch rule. The source package excludes generated build/cache output.
