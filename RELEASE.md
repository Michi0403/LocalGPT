# LocalGPT 3.4.1

LocalGPT 3.4.1 is the **Ollama Path Comparer Compile Repair** release.

It fixes the `CS0115` failure introduced in 3.4.0 by restoring the base `ExecutablePathComparer` member that the Windows Ollama platform implementation overrides. The default comparer is case-sensitive for Unix/macOS and the Windows implementation remains case-insensitive.

All 3.4.0 cross-platform backend boundaries and the 3.3.x documentation/Pages fixes remain intact. No wire-protocol, InteractiveServer render-mode, GitHub Pages workflow, or documentation accessibility policy is changed by this patch.

This handoff is source-only and was not built with .NET or executed with PowerShell in the packaging environment. See `CHANGELOG-v3.4.1-OLLAMA-PATH-COMPARER-COMPILE-REPAIR.md` and `VALIDATION-v3.4.1-source.md`.
