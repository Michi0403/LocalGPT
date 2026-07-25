# LocalGPT v0.1.4 workflow-safety compile candidate

This source package fixes the compiler and workflow diagnostics reported after the earlier v0.1.4 candidates. It preserves the existing feature set while strengthening contracts, component safety, user notification, technical logging, and bounded short-term operational awareness.

It is intentionally called a **candidate** because this isolated environment does not have the owner's licensed DevExpress feed, Windows SDK/workloads, or a usable .NET SDK installation. It has passed the included lexical, source-contract, configuration, protected-file, and package-hygiene checks, but the owner-side command below remains authoritative:

```powershell
./build/Invoke-RepositoryValidation.ps1
```

Do not rename or publish this candidate as a verified release until that command completes Roslyn parsing and both Debug and Release builds for the exact source fingerprint. After success, create the normal package with:

```powershell
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```

## This revision

The workflow-safety revision also fixes the navigation constant/type collision, restores the shared `DxaichatFunctionInfo` contract, aligns the reported nullable workflow signatures, adds component-wide top-directive logger/notifier/activity dependencies, provides recoverable routed error boundaries, and exposes sanitized bounded activity at `/__diag/component-activity`.

See `DEBUG-NEXT-STEPS.md` for the owner-side build and runtime smoke sequence.

