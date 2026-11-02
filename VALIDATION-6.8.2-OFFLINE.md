# LocalGPT 6.8.2 — Offline source and release-preflight validation

## Input and relevant evidence

Input: the uploaded `LocalGPT_6.8.1_source.zip`, not a network/GitHub checkout. The C# sandbox consumers in that ZIP already delegate native executable selection through `IPlatformRuntimeService`. The third reported release log finds the `6.8.1` audit while also finding pre-6.8.1 direct OS branches in both consumers; these states cannot coexist in the two corresponding files extracted unmodified from that ZIP.

## Static checks performed

1. Reviewed `build/Assert-CrossPlatformBoundaries.ps1` to confirm the OS-branch policy and the explicit `GetNativeExecutableName("hluk")` source contracts were kept. The audit now reports the exact offending lines and a mixed-source diagnostic rather than silencing findings.
2. Scanned **893** non-generated LocalGPT C# source files with the cross-platform audit's OS, Windows-detail, and System.Drawing patterns. **Zero** violations outside the reviewed platform boundaries.
3. Verified that both sandbox consumers call `platformRuntime.GetNativeExecutableName("hluk")` and have no direct `OperatingSystem.IsWindows()` or equivalent branch.
4. Verified Windows/Unix executable naming (`.exe` on Windows, no `.exe` on Unix) is scoped to the corresponding `IPlatformRuntimeService` implementation, and both compile-only and full-release entrypoints invoke the platform and sandbox guards.
5. Verified `src/LocalGPT/LocalGPT.csproj` now declares **6.8.2** and that release-version numeric minor/patch constraints are respected.
6. Tested in an offline source-check harness that the exact pre-6.8.1 OS branch expressions trigger the audit's detection pattern, while the 6.8.2 sandbox consumers do not; changing/removing either platform class's required executable-name contract fails its class-scoped assertion.
7. Verified each archived entry's CRC during ZIP packaging and confirmed the package preserves the full source inventory.

## Consumer SHA-256: use to detect a stale extraction

The following are the **full-file** SHA-256 hashes of the unchanged, correct sandbox C# implementations shipped in both 6.8.1 and 6.8.2:

| File (under `src/LocalGPT/Services/`) | SHA-256 |
|---|---|
| `HyperlightUnikraftSessionSandboxProvider.cs` | `648056f0513518a30598719cefac1967d9c2d4e0268024c543def6d3cee0b170` |
| `SessionSandboxExecutionService.cs` | `b8f85b08bb34e0f0dbc161ce08045f0857ad2b65bc6a5065d581e4d544715f32` |

In PowerShell on the macOS extraction:

```powershell
(Get-FileHash ./src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs -Algorithm SHA256).Hash
(Get-FileHash ./src/LocalGPT/Services/SessionSandboxExecutionService.cs -Algorithm SHA256).Hash
Select-String -Path ./src/LocalGPT/Services/*Sandbox*.cs -Pattern 'OperatingSystem.IsWindows\(' -CaseSensitive
```

The hashes should match the listed values; the `Select-String` search should find no match in the two sandbox consumers. If they do not, the build directory is not an unchanged extraction of the supplied 6.8.2 ZIP. Do not overwrite an existing working directory to troubleshoot it; use a fresh, separate directory.

## Not executed

`pwsh` and Windows PowerShell 5.1 are unavailable in this environment, so no PowerShell parser/guard execution was performed. No .NET compilation, release build, DevExpress checks, Node documentation build, signing, or Hyperlight guest execution was performed. Static guard parity is not equivalent to executing those tools on macOS or Windows.
