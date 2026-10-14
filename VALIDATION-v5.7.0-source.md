# LocalGPT 5.7.0 — source validation

- **PASS**: Three DevExpress project/revision/file selectors have an explicitly typed, awaited Guid callback with captured render-context and current `ValueExpression`.
- **PASS**: The three handlers return `Task` and the file selector writes through renderer-affine `InvokeAsync`.
- **PASS**: Nine malformed callback variants (missing `async`, missing `await`, wrong bound edit context) were rejected by the tested selector-expression pattern.
- **PASS**: `audit_python_project_orchestration_5_6_4.py`: Python.NET GIL worker, bounded isolated subprocesses, project curation, persisted policy, DevExpress editor and Council popup.
- **PASS**: `audit_release_packaging_pkcs_5_6_9.py`: PDFsharp/PKCS 10.0.13 pin, helper tool 1.0.5, security audit active.
- **PASS**: `audit_public_service_controllers_5_6_5.py`: public service controller/permission/discovery architecture.
- **PASS**: `audit_dx_function_workbench_5_6_3.py`: discovery, schema preview, human approval, UI/API/Council integration.
- **PASS**: project version is 5.7.0 and application JS cache tokens are consistent.
- **PASS**: full ZIP CRC verified and all archive members compared byte-for-byte to the authored source tree.
- **NOT RUN**: Windows PowerShell 5.1 syntax and execution of `build/Assert-RuntimePluginSourceCallbacks.ps1`; neither PowerShell nor pwsh is installed here.
- **NOT RUN**: .NET restore, C#/Razor compilation, DevExpress rendering, Windows runtime tests, DocFX, or GitHub actions.

On Windows, verify in this order:

```powershell
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File build/Assert-RuntimePluginSourceCallbacks.ps1
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File build/Assert-PythonProjectOrchestration.ps1
# Then run the normal Visual Studio solution build.
```

Watch for any `CS1503`/`CS1662` on `Install.razor` and confirm the old version-pinned architecture-gate failure is absent.
