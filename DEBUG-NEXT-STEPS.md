# LocalGPT v0.1.4 workflow-safety debug steps

This is a source/debug candidate. Extract it into a new folder instead of overlaying an older build tree, so stale `bin`, `obj`, `.vs`, or generated Razor files cannot survive.

## Preferred validation

From the repository root in PowerShell:

```powershell
./build/Invoke-RepositoryValidation.ps1 -Configuration Debug
```

That command verifies protected instructions, source/security rules, Roslyn syntax, component safety, workflow contracts, restore, and the complete Debug solution build.

For the final release gate, run both configurations:

```powershell
./build/Invoke-RepositoryValidation.ps1
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```

## Visual Studio debugging

1. Open `LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln`.
2. Clean the solution.
3. Rebuild the `LocalGPT` project first.
4. Only investigate `LocalGPTWebviewWrapper` metadata errors after `LocalGPT.dll` is produced.
5. Then rebuild the whole solution and start the wrapper.

The wrapper's `CS0006`/`WMC1006` messages are normally cascade errors when the root web project did not produce `LocalGPT.dll`.

## Runtime safety smoke checks

After startup:

- Open `/`, `/chat`, `/model-council`, `/projects`, `/database`, `/test-lab`, and `/minecraft-mod-builder`.
- Trigger one successful and one deliberately failing safe/read-only action.
- Confirm a sanitized toast appears and technical details are present in the local application log.
- Open `/__diag/component-activity?take=40` and confirm the activity list contains route, component, operation, and status summaries only.
- Confirm the diagnostic output contains no prompt bodies, AI answers, uploaded content, generated source, credentials, secrets, or full exception messages.
- Navigate away after a deliberately failed page and confirm the next page loads; routed error boundaries now recover on navigation.

## What to send back after a failure

Send the first compiler errors from the `LocalGPT` project, plus the surrounding source lines. Do not start with wrapper `CS0006` or `WMC1006` unless `LocalGPT` itself built successfully.
