# LocalGPT 5.7.1 — source-only validation and Windows acceptance

## Checked without executing dotnet / PowerShell

- Authoritative user-provided ZIP integrity verified before extraction.
- `Directory.Build.targets` parses as XML after adding fail-fast, Python-free build and callback gates.
- All referenced `build/*.ps1` files exist; no executable Python-based maintenance/test invocation remains in Build/Debug/Publish PowerShell and Pages workflow entrypoints.
- Three source-selector callbacks are typed, awaited lambdas bound to `currentSourceContext` with `ConfigureAwait(true)`, and the reviewed-file callback is a Task-returning `InvokeAsync` implementation.
- Runtime regex-literal and prohibited native Razor section/dialog source scans yielded zero findings on the extracted source.
- Existing `NuGet`, DevExpress and media runtime sources were left intact.
- ZIP CRC/content comparison and the changed/added/missing file manifest checked after packaging.

**Not executed here:** Windows PowerShell 5.1 parser/runtime, real .NET SDK restore/build, Razor compiler, full DocFX render, GitHub Pages publication, tagged-PDF accessibility preflight, Windows and WSL release installers. The recreated PowerShell guards are source-inspected; they are not claimed to have passed in PowerShell without a Windows run.

## Windows Debug/release acceptance

1. Run Debug from the uploaded code baseline after replacing it with 5.7.1. Confirm `AssertLocalGptPowerShellOnlyMaintenance`, `AssertLocalGptRuntimePluginSourceCallbacks` and `AssertLocalGptNativeArchitectureMaintenance` succeed.
2. Confirm no Python installation or `python3` binary is necessary to complete the Debug build and generate documentation.
3. Test switching project, revision, file, and plugin draft while the async source queries are in flight; stale results must never mutate the new draft.
4. Verify DocFX HTML/link preflight under **Windows PowerShell 5.1** and the existing large PDF rendering pipeline; ensure valid tagged PDFs are accepted.
5. Complete Release and GitHub Pages snapshot staging and verify the Pages ZIP contains `github-pages-deployment.json`, `.nojekyll`, a matching version, and *no bundled release PDF*.
6. Verify the GitHub Actions workflow on its Ubuntu PowerShell runner.
7. If the PDF provider stores `/StructTreeRoot` solely inside compressed object streams, extend/test that compatibility path before relying on this validator for release publishing. Do **not** disable the PDF or HTML-accessibility check to force a pass.

## Preserved exceptions

The optional Python application interpreter/embedded Python.NET GIL worker and isolated CPython job support remain runtime features. Optional developer-oriented Python tools not referenced by Build/Debug/Publish are not a reason to install Python for normal compilation.
