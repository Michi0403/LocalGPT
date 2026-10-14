# LocalGPT 5.2.1 source validation

## Validation boundary

This handoff was validated at source level only. No `dotnet`, MSBuild, NuGet restore/publish, installer execution, GitHub, or online repository access was used. The maintainer's target Windows/.NET 10 run remains the compile/runtime authority.

## Supplied 5.2.0 runtime/HTML evidence

The supplied 5.2.0 trace shows a successful LocalGPT host start, MCP listener start, database initialization, DXFunction catalog synchronization, and primary listener startup. The supplied HTML shows the shell and DevExpress controls rendering. No `Error`/`Critical` log entry or exception appears in the supplied 5.2.0 log; the warnings are Kestrel address-source override, circuit connection-down diagnostics, and one configured Ollama-session fallback.

The language selector was still implemented directly inside the static `MainLayout`. In contrast, existing menu/theme behavior already used reviewed Interactive Server islands. 5.2.1 moves only the language selector into its own interactive island instead of converting the complete layout/router into one interactive boundary.

## Source checks executed

- `python build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python build/audit_razor_maintenance_contract.py --root . --product localgpt` — passed for 55 maintained Razor components.
- `python build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 333 source files, 4,052 await tokens.
- `python build/audit_service_resilience.py --root . --product localgpt` — passed for 2,854 service methods; 29 iterator methods and 3 direct Program/Startup methods remain intentional exclusions.
- `python build/audit_cross_platform_boundaries.py` — passed with 22 checks.
- `python build/audit_devexpress_blazor_controls.py` — passed; only the two circuit-independent reconnect controls remain native.
- `python build/audit_configuration_root_qualification.py` — passed.
- `python build/audit_codegen_dxfunction_wiring.py` — passed.

The repository-wide XML-documentation validator was also inspected. It still reports the repository's pre-existing documentation backlog (2,602 findings); none of those findings targets the new `CouncilToolResultContinuationMode` or the new 5.2.1 continuation service.

## Targeted regression checks

- Version is `5.2.1`, with browser asset cache-busters advanced to `5.2.1`.
- All 20 explicit render-mode directives from 5.2.0 remain present with identical component/directive text. `LanguageSwitcher.razor` is the sole new explicit render boundary, bringing the count to 21.
- `MainLayout` no longer owns the language `DxComboBox` event handler; `LanguageSwitcher` owns the selector inside an Interactive Server boundary while the layout retains its existing static routing architecture.
- The existing 5.2.0 MCP source is not modified by this pass.
- `CouncilWorkflowStepDefinition` persists the function-result continuation mode and bounded round count through the existing workflow-step JSON model; no database column/schema migration was introduced.
- Built-in/non-user-modified Council team seeds move to revision 39; user-modified team rows retain their ownership boundary.
- Configured Council primary and recovery results include same-member tool-continuation child phases in the workflow's stage result selection.
- The continuation prompt explicitly treats function output as intermediate evidence and `HumanApprovalPending` as queued-but-not-executed.
- Test Lab route/UI actions expose running state before long awaits, append result history, and merge artifact links.
- Documentation and Test Lab source popups use explicit viewport heights supported elsewhere in the application.
- Drawer and theme-switcher styling is based on LocalGPT theme/navigation variables and therefore remains compatible with light/dark themes rather than hard-coding one palette.
- PublisherStudio was not modified.

## Packaging hygiene

The final source ZIP is checked for archive integrity and for absence of `bin`, `obj`, `.vs`, and `__pycache__` work artifacts before handoff.
