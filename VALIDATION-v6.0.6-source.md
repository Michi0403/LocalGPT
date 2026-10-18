# LocalGPT 6.0.6 — source validation report

## Baseline and intended scope

- Baseline: complete `LocalGPT-6.0.5-source.zip` (2,156 source files).
- All baseline paths and source bytes are preserved except the six deliberate edits:
  `.gitignore`, `Build-Release.ps1`, `build/Assert-StaticWebAssets.ps1`,
  `build/Assert-TextServiceOwnership.ps1`, `docs/engineering/build-validation.md`,
  and `src/LocalGPT/LocalGPT.csproj` (application version field only).
- Add this validation report and a 6.0.6 changelog; retain third-party code,
  copyright/license notices, vendor sources, all C#/Razor/CSS/JS application
  implementations, SQL and existing mandatory MSBuild validation targets.

## Failure evidence and hypothesis limits

- Windows 6.0.4 `Assert-TextServiceOwnership.ps1` previously failed because
  `$known` was not defined; the baseline 6.0.5 source initializes an empty
  `HashSet` and moves a Council list-splitting operation to the injected text
  service. The source-only check detects no remaining direct string/regex
  ownership violations in the 189 maintained UI/controller files.
- The macOS 6.0.5 release stopped in `Assert-StaticWebAssets.ps1` with MSB3073
  exit code 1; the log did not reveal the script's failing condition.
  All 357 project-required static assets and all four CSS contracts are present
  in the full source ZIP, but this does **not** establish that the Mac's active
  Git checkout contains the same files or that an actual Mac test passes.
- Git's real `check-ignore -v --no-index` reproduces the old repository-root
  `images/` pattern incorrectly ignoring nested web assets. After anchoring the
  scratch exclusion to `/images/`, Git no longer ignores the two representative
  authored image paths and continues to ignore root `images/scratch.png`.
  Whether the Mac checkout is missing files cannot be established without the
  Mac's filesystem/Git index; missing files are printed explicitly at runtime.

## Offline source checks performed

1. Parsed `LocalGPT.csproj` XML; enumerated the same 357 maintained source
   assets named by the guard; all exist at their exact case-sensitive paths.
2. Validated all four required Council evidence CSS selectors in `site.css`
   and in the unchanged detection semantics of the updated guard.
3. Replayed the text ownership guard's .NET regex semantics using compatible
   offline regex matching against 189 UI/controller C# and Razor sources;
   **zero reported violations**. Four direct-operation test probes match.
4. Confirmed early release guard order: JavaScript diagnostics ->
   text-service ownership -> static assets -> `bin/obj` cleanup.
5. Validated source asset visibility with actual Git ignore matching.
6. ZIP CRC, baseline path retention, unchanged-byte comparison and versioned
   project metadata are verified as part of final packaging.

## Not executed here

PowerShell 5.1/pwsh, .NET, Visual Studio, macOS native signing/notarization,
and LocalGPT browser/runtime tests are not available in this environment.
**The source repair must not be called a successful macOS release until tested.**
The new direct preflight provides complete missing-file/CSS diagnostics even
when the later MSBuild Exec only exposes a numeric failure code.
