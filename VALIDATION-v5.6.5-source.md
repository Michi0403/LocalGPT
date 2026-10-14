# LocalGPT 5.6.5 — Static source validation

## Successful source checks
- `python build/audit_public_service_controllers_5_6_5.py` — passed (new controller/DI/approval and ConfigRoot contracts).
- `python build/audit_configuration_root_qualification.py` — passed.
- `python build/audit_python_project_orchestration_5_6_4.py` — passed against source version 5.6.5.
- `python build/audit_cross_platform_boundaries.py` — passed (26 checks).
- `python build/audit_devexpress_blazor_controls.py` — passed.
- Prior static passes during development: architecture policy, async continuation, async-only, service resilience, Razor maintenance, runtime text and regex ownership, DX function workbench, data management, game studio, windows no-auto-model setup, human collaboration and PowerShell portability.

## Inconclusive / not run
- A last combined audit runner was terminated by the execution time budget, not a source failure; it does not establish a new all-green result.
- The standalone XML documentation coverage tool also finds thousands of findings in *unchanged 5.6.4* and is not reported as passing.
- No .NET restore, compilation, Razor source generation, publish, Windows/UI runtime tests, device tests or GitHub access were performed.
- User log shows a separate `LocalGPTWebviewWrapper` NuGet restore project-detail load problem; not enough evidence to repair safely and independently of CS0104.

## Windows acceptance
1. Restore all project packages in Visual Studio and diagnose the wrapper project-load error separately if it persists.
2. Build LocalGPT normally; verify CS0104 is gone and mandatory public-controller audit fires.
3. GET `/api/dxai/public-service/groups`, find purpose-specific service group and method schema.
4. POST `/api/dxai/public-service/invoke`: verify pending approval (202), then approve exact action and retry it with unchanged arguments; verify action runs only through registered DI method.
5. Verify group `/exposure` remains independently approved and never silently activates AI/peer access when viewing service metadata.
6. Exercise runtime plugin, Council team and knowledge/regex controller actions including deletion approval.
7. In chat/Council, use `localgpt.public_service.discover`; verify discovery cannot itself execute a method.
