# LocalGPT 5.5.2 — source checks and Windows acceptance

## Static source checks

Execute from the unpacked repository root; the preparation environment performed no .NET build or Windows PowerShell execution.

```text
python3 build/audit_interactive_server_registry_5_5_2.py
python3 build/audit_install_hardware_scores_5_5_2.py
python3 build/audit_data_management_5_5_1.py
python3 build/audit_learning_checklist_5_4_9.py
python3 build/audit_documentation_pdf_merge_5_5_0.py
python3 build/audit_devexpress_blazor_controls.py
python3 build/audit_razor_maintenance_contract.py --root . --product localgpt
python3 build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt
python3 build/audit_async_continuations.py --source-root src/LocalGPT
python3 build/audit_service_resilience.py --root . --product localgpt
python3 build/audit_application_architecture.py --root . --product localgpt --mode all
python3 build/audit_runtime_text_ownership.py --root .
python3 build/audit_powershell_portable_paths.py
```

## Windows acceptance

- [ ] Run the original Visual Studio .NET 10 build with all PowerShell/maintenance targets enabled. Confirm `Assert-InteractiveServerRenderModes.ps1` now reports **21 explicit islands/pages** and **3 inherited theme children** (20 retained entries plus Data Management), with no `MSB3073` at the render-mode validation step.
- [ ] Visit `/data-management`. Confirm only the intended InteractiveServer component instance performs the initial service reads; no duplicate prerender/interactive UI requests. Verify live table groups, parent-child navigation, DevExpress editors and the trusted model-health release action.
- [ ] Visit `/install` → **AI providers**, enter **24.00 GiB** into the configured host's dedicated VRAM DevExpress editor, save and reload, and verify the value remains 24 GiB. Repeat the 24 GiB entry in **Setup Guide** → **Local host hardware**. Verify the invariant numeric decimal representation and preserved fractional increments such as 24.25 GiB.
- [ ] In `/install` → **Setup Guide**, select the correct detected/reviewed hardware row and explicitly opt in to CanIRun.ai. Run **Find hardware-fit models**, check the returned model names, grades and exact source scores in both the result cards and the paged/searchable score grid under **Provider models and hardware-fit discoveries**. Compare scores against the attributed source; zero scores must not be silently fabricated as positive values.
- [ ] While results are visible, change the left vertical workbench tab (e.g. **AI providers**) and return to **Setup Guide**. The latest successful lookup rows and model scores must still appear without requiring a second external request. Model actions must still function under the existing confirmation and provider gates. Reloading `/install` begins a new view session and may require a new opted-in lookup.
- [ ] Confirm existing routed pages, Council/Chat, DevExpress menu, Human Collaboration approval inbox, ASCII presentation, per-file learning checklist and documentation build behave as in 5.5.1.
- [ ] Run the remaining build targets and report any subsequent exact compiler/guard diagnostics; source-only preparation cannot establish successful Razor/C# compilation or provider/database behavior.

For feature-level checks, continue with `VALIDATION-v5.5.1-source.md`. The fix does not change EF schema, storage locations, Council approval policy, DevExpress component structure or PDF requirements.
